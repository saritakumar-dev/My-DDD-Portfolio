using BankLedger.Core.Common;
using BankLedger.Core.Common.Commands;
using BankLedger.Core.Common.Events;
using BankLedger.WriteProject.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace BankLedger.WriteProject.Application.Sagas
{
    public class MoneyTransferSaga : IDomainEventHandler<MoneyWithdrawnEvent>,
                                     IDomainEventHandler<MoneyDepositedEvent>,
                                     IDomainEventHandler<DepositMoneyFailedEvent>
    {
        private readonly ISagaStateRepository _sagaStateRepository;
        private readonly IServiceProvider _serviceProvider;
       
        public MoneyTransferSaga(IServiceProvider serviceProvider,
            ISagaStateRepository sagaStateRepository)
        {
            _serviceProvider = serviceProvider;
            _sagaStateRepository = sagaStateRepository;
        }
        public async Task StartAsync(Guid sourceAccountId, Guid targetAccountId, decimal amount, CancellationToken cancellationToken)
        {
            var sagaState = new MoneyTransferSagaState
            {
                SourceAccountId = sourceAccountId,
                TargetAccountId = targetAccountId,
                Amount = amount,
                CurrentState = TransferWorkflowState.WithdrawalStarted
            };
            await _sagaStateRepository.SaveAsync(sagaState, cancellationToken);
            var withdrawHandler = _serviceProvider.GetRequiredService<ICommandHandler<WithdrawMoneyCommand>>();
            await withdrawHandler.HandleAsync(new WithdrawMoneyCommand(sourceAccountId, amount, $"Transfer out to {targetAccountId} | SagaId: {sagaState.SagaId}"), cancellationToken);
        }

        public async Task HandleAsync(MoneyWithdrawnEvent @event, CancellationToken cancellationToken)
        {
            Guid sagaId = ExtractSagaIdFromReference(@event.Reference);
            if (sagaId == Guid.Empty) return;

            var state = await _sagaStateRepository.GetStateBySagaIdAsync(sagaId, cancellationToken);
            if (state == null || state.CurrentState != TransferWorkflowState.WithdrawalStarted) return;

            state.CurrentState = TransferWorkflowState.WithdrawalCompleted;
            await _sagaStateRepository.SaveAsync(state, cancellationToken);

            var depositMoneyCommand = new DepositMoneyCommand(state.TargetAccountId, state.Amount, $"Transfer In | SagaId: {state.SagaId}");

            var depositHandler = _serviceProvider.GetRequiredService<ICommandHandler<DepositMoneyCommand>>();
            await depositHandler.HandleAsync(depositMoneyCommand, cancellationToken);
        }

        public async Task HandleAsync(MoneyDepositedEvent @event, CancellationToken cancellationToken)
        {
            Guid sagaId = ExtractSagaIdFromReference(@event.Reference);
            if (sagaId == Guid.Empty) return;

            var state = await _sagaStateRepository.GetStateBySagaIdAsync(sagaId, cancellationToken);

            if (state == null || (state.CurrentState != TransferWorkflowState.WithdrawalCompleted &&
                state.CurrentState != TransferWorkflowState.CompensationStarted)) return;

            if (state.CurrentState == TransferWorkflowState.CompensationStarted)
            {
                state.CurrentState = TransferWorkflowState.FailedAndReversed;
            }
            else
            {
                state.CurrentState = TransferWorkflowState.DepositCompleted;
            }


            await _sagaStateRepository.SaveAsync(state, cancellationToken);
        }

        public async Task HandleAsync(DepositMoneyFailedEvent @event, CancellationToken cancellationToken)
        {
            Guid sagaId = ExtractSagaIdFromReference(@event.Reference);
            if (sagaId == Guid.Empty) return;

            var state = await _sagaStateRepository.GetStateBySagaIdAsync(sagaId, cancellationToken);

            if (state == null || state.CurrentState != TransferWorkflowState.WithdrawalCompleted) { return; }

            state.CurrentState = TransferWorkflowState.CompensationStarted;
            await _sagaStateRepository.SaveAsync(state, cancellationToken);

            var refundCommand = new DepositMoneyCommand(state.SourceAccountId,
                                                            state.Amount,
                     $"REVERSAL: Transfer to {state.TargetAccountId} failed. Reason: {@event.Reason} | SagaId: {state.SagaId}"
                );

            var depositHandler = _serviceProvider.GetRequiredService<ICommandHandler<DepositMoneyCommand>>();
            await depositHandler.HandleAsync(refundCommand, cancellationToken);

            state.CurrentState = TransferWorkflowState.FailedAndReversed;
            await _sagaStateRepository.SaveAsync(state, cancellationToken);
        }

        private Guid ExtractSagaIdFromReference(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference) || !reference.Contains("SagaId:")) return Guid.Empty;

            var parts = reference.Split("SagaId:");

            if (parts.Length < 2) return Guid.Empty;

            return Guid.TryParse(parts[1].Trim(), out var guid) ? guid : Guid.Empty;
        }
    }
}
