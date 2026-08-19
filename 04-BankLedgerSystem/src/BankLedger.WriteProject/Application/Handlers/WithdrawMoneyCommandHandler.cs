using BankLedger.Core.Common;
using BankLedger.Core.Common.Events;
using BankLedger.Core.Common.MessageBus;
using BankLedger.Domain.Aggregates;
using BankLedger.Domain.Common;
using BankLedger.WriteProject.Application.Common;
using BankLedger.WriteProject.Application.Common.Exceptions;

namespace BankLedger.WriteProject.Application.Handlers
{
    public class WithdrawMoneyCommandHandler : ICommandHandler<WithdrawMoneyCommand>
    {
        private readonly IEventStore _eventStore;
        private readonly IMessageBus _messageBus;
        private readonly ISnapshotStore _snapshotStore;
        private readonly int _snapshotInterval;
        private const string StreamCategory = "Bank Account";
        public WithdrawMoneyCommandHandler(IEventStore eventStore, IMessageBus messageBus, ISnapshotStore snapshotStore,
            int snapshotInterval = 100)
        {
            _eventStore = eventStore;
            _messageBus = messageBus;
            _snapshotStore = snapshotStore;
            _snapshotInterval = snapshotInterval;
        }
        public async Task HandleAsync(WithdrawMoneyCommand command, CancellationToken cancellationToken)
        {
            IList<BankEvent> history = new List<BankEvent>();
            BankAccount? account = null;
            try
            {
                var snapshot = await _snapshotStore.GetLatestAsync(command.AccountId);
                int startingVersion = 0;
                if (snapshot != null)
                {
                    account = BankAccount.FromSnapshot(snapshot);
                    startingVersion = snapshot.Version;
                }

                AmbientContext.CurrentAggregateId = command.AccountId;

                history = await _eventStore.GetEventsAsync(command.AccountId, startingVersion, cancellationToken);

                if (!history.Any()) throw new KeyNotFoundException("Account not found.");

                account = BankAccount.LoadFromHistory(history);

                account.Withdraw(command.Amount, command.Currency, command.Reference);

                var eventsToPublish = account.UncommittedEvents.ToList();
                        
                await _eventStore.AppendEventsAsync(command.AccountId, StreamCategory, account.Version, account.UncommittedEvents, cancellationToken);

                // Snapshot Evaluation and Creation Strategy

                if (account.Version % _snapshotInterval == 0)
                {
                    // Extract the state snapshot from the aggregate
                    var newSnapshot = account.CreateSnapshot();

                    // Save it to your MySQL snapshots table
                    await _snapshotStore.SaveAsync(newSnapshot);
                }

                // Broadcast to the generic bus.
                foreach (var @event in eventsToPublish)
                {
                    if (@event is MoneyWithdrawnEvent withdrawnEvent)
                        await _messageBus.PublishAsync(withdrawnEvent, cancellationToken);
                }
            }
            catch (InvalidOperationException ex)
            {
                // Catch domain business rules violations
                throw new ApplicationDomainException("Account Operation Rejected", ex.Message, 400);
            }
            catch (Exception)
            {
                throw new ApplicationDomainException(
                 "Internal System Error",
                 "An error occurred while communicating with backend infrastructure. Please contact support.",
                 500);
            }
            finally { account?.ClearUncommittedEvents(); }
        }
    }
}
