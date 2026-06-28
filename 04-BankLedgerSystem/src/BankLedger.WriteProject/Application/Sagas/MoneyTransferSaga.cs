using BankLedger.Core.Common;
using BankLedger.Core.Common.Commands;
using BankLedger.Core.Common.Events;
using BankLedger.WriteProject.Application.Common;

namespace BankLedger.WriteProject.Application.Sagas
{
    public class MoneyTransferSaga : IDomainEventHandler<MoneyWithdrawnEvent>, IDomainEventHandler<MoneyDepositedEvent>
    {
        private readonly ICommandHandler<WithdrawMoneyCommand> _withdrawHandler;
        private readonly ICommandHandler<DepositMoneyCommand> _depositHandler;
        private readonly ISagaStateRepository _sagaStateRepository;

        //public MoneyTransferSaga() { }
        public MoneyTransferSaga(ICommandHandler<WithdrawMoneyCommand> withdrawHandler,
            ICommandHandler<DepositMoneyCommand> depositHandler,
            ISagaStateRepository sagaStateRepository)
        {

            _withdrawHandler = withdrawHandler;
            _depositHandler = depositHandler;
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

            await _withdrawHandler.HandleAsync(new WithdrawMoneyCommand(sourceAccountId, amount, $"Transfer out to {targetAccountId} | SagaId: {sagaState.SagaId}"), cancellationToken);
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

            await _depositHandler.HandleAsync(depositMoneyCommand, cancellationToken);
        }


        public async Task HandleAsync(MoneyDepositedEvent @event, CancellationToken cancellationToken)
        {
            Guid sagaId = ExtractSagaIdFromReference(@event.Reference);
            if (sagaId == Guid.Empty) return;

            var state = await _sagaStateRepository.GetStateBySagaIdAsync(sagaId, cancellationToken);

            if(state == null || state.CurrentState != TransferWorkflowState.WithdrawalCompleted) return;

            state.CurrentState = TransferWorkflowState.DepositCompleted;

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
