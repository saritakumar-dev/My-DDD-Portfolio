using BankLedger.Core.Common;
using BankLedger.Core.Common.Events;
using BankLedger.Domain.Aggregates;
using BankLedger.Domain.Common;
using BankLedger.WriteProject.Application.Common;
using BankLedger.WriteProject.Application.Common.Exceptions;
using BankLedger.WriteProject.Application.Common.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BankLedger.WriteProject.Application.Sagas
{
    public class MoneyTransferSaga : IDomainEventHandler<MoneyWithdrawnEvent>,
                                     IDomainEventHandler<MoneyDepositedEvent>,
                                     IDomainEventHandler<DepositMoneyFailedEvent>
    {
        private readonly ISagaStateRepository _sagaStateRepository;
        private readonly IAccountReadModelService _accountReadModelService;
        private readonly IServiceProvider _serviceProvider;

        public MoneyTransferSaga(IServiceProvider serviceProvider,
            ISagaStateRepository sagaStateRepository, IAccountReadModelService accountReadModelService)
        {
            _serviceProvider = serviceProvider;
            _sagaStateRepository = sagaStateRepository;
            _accountReadModelService = accountReadModelService;
        }

        public async Task StartAsync(List<MoneyTransferInstruction> instructions, CancellationToken cancellationToken)
        {
            var uniqueAccountIds = instructions.Select(x => x.AccountId).Distinct().ToList();

            var lookupTasks = (uniqueAccountIds.Select(accountId =>
                            _accountReadModelService.GetAccountStateResultAsync(accountId, cancellationToken)).ToList());

            await Task.WhenAll(lookupTasks);

            var accountStates = uniqueAccountIds
                                .Zip(lookupTasks, (id, task) => new { id, task.Result })
                                .ToDictionary(x => x.id, x => x.Result);

            // Group all instructions by AccountId and sum up their total net amount impact
            var accountNetImpacts = instructions
                .GroupBy(i => i.AccountId)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));


            foreach (var keyValuePair in accountNetImpacts)
            {
                var accountState = accountStates[keyValuePair.Key];
                if (accountState.IsClosed) throw new AccountDeactivatedException(accountState.AccountId);

                if (keyValuePair.Value < 0 && accountState.CurrentBalance < Math.Abs(keyValuePair.Value)) throw new InsufficientFundsException(accountState.AccountId, accountState.CurrentBalance, Math.Abs(keyValuePair.Value));
            }

            var transactionLegs = instructions.Select(i => new LedgerEntry(i.AccountId, i.Amount, i.Description)).ToList();
            var journalEntryCommandHandler = _serviceProvider.GetRequiredService<ICommandHandler<PostJournalEntryCommand>>();
            await journalEntryCommandHandler.HandleAsync(new PostJournalEntryCommand(Guid.NewGuid(), transactionLegs), cancellationToken);
        }
        public async Task HandleAsync(MoneyWithdrawnEvent @event, CancellationToken cancellationToken)
        {
            Guid sagaId = ExtractSagaIdFromReference(@event.Reference);
            if (sagaId == Guid.Empty) return;

            var state = await _sagaStateRepository.GetStateBySagaIdAsync(sagaId, cancellationToken);
            if (state == null || state.CurrentState != TransferWorkflowState.WithdrawalStarted) return;

            state.CurrentState = TransferWorkflowState.WithdrawalCompleted;
            await _sagaStateRepository.SaveAsync(state, cancellationToken);

            var depositMoneyCommand = new DepositMoneyCommand(state.TargetAccountId, state.Amount, @event.Currency, $"Transfer In | SagaId: {state.SagaId}");

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
                                                            state.Amount, @event.Currency,
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
