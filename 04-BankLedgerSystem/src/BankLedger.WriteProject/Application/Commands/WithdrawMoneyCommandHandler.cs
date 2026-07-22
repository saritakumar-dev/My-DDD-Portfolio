using BankLedger.Core.Common;
using BankLedger.Core.Common.Commands;
using BankLedger.Core.Common.Events;
using BankLedger.Core.Common.MessageBus;
using BankLedger.WriteProject.Application.Common;
using BankLedger.WriteProject.Domain.Aggregates;

namespace BankLedger.WriteProject.Application.Commands
{


    public class WithdrawMoneyCommandHandler : ICommandHandler<WithdrawMoneyCommand>
    {
        private readonly IEventStore _eventStore;
        private readonly IMessageBus _messageBus;
        private readonly ISnapshotStore _snapshotStore;
        private readonly int _snapshotInterval;
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

                history = await _eventStore.GetEventsAsync(command.AccountId, startingVersion, cancellationToken);

                if (!history.Any()) throw new KeyNotFoundException("Account not found.");

                account = BankAccount.LoadFromHistory(history);

                account.Withdraw(command.Amount, command.Reference);

                var eventsToPublish = account.UncommittedEvents.ToList();

                await _eventStore.AppendEventsAsync(command.AccountId, account.Version, account.UncommittedEvents, cancellationToken);

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
            finally { account?.ClearUncommittedEvents(); }
        }
    }


}
