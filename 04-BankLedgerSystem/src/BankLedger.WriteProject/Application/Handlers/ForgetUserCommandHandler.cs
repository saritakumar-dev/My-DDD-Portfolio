using BankLedger.Core.Common;
using BankLedger.WriteProject.Application;
using BankLedger.Core.Common.Events;
using BankLedger.Core.Common.MessageBus;
using BankLedger.WriteProject.Application.Common;
using BankLedger.Domain.Aggregates;
using BankLedger.Domain.Common;

namespace BankLedger.WriteProject.Application.Handlers
{
    public class ForgetUserCommandHandler : ICommandHandler<ForgetUserCommand>
    {
        private readonly ICryptoKeyVault _keyVault;
        private readonly IEventStore _eventStore;
        private readonly IMessageBus _messageBus;
        private readonly ISnapshotStore _snapshotStore;

        public ForgetUserCommandHandler(ICryptoKeyVault keyVault, IEventStore eventStore, IMessageBus messageBus, ISnapshotStore snapshotStore)
        {
            _keyVault = keyVault;
            _eventStore = eventStore;
            _messageBus = messageBus;
            _snapshotStore=snapshotStore;
        }
        public async Task HandleAsync(ForgetUserCommand command, CancellationToken cancellationToken)
        {
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
                var history = await _eventStore.GetEventsAsync(command.AccountId, startingVersion, cancellationToken);

                if (!history.Any()) throw new KeyNotFoundException("Account not found.");

                account = BankAccount.LoadFromHistory(history);
                int version = account.Version + 1;
                var erasureEvent = new UserForgottenEvent(command.AccountId, version);

                var eventsToAppend = new List<BankEvent> { erasureEvent };
                var eventsToPublish = eventsToAppend;

                await _eventStore.AppendEventsAsync(command.AccountId, version, eventsToAppend, cancellationToken);
                await _keyVault.ShredKeyAsync(command.AccountId);

                foreach (var @event in eventsToPublish)
                {
                    if (@event is UserForgottenEvent userForgottenEvent)
                        await _messageBus.PublishAsync(userForgottenEvent, cancellationToken);
                }
            }
            finally
            {
                AmbientContext.CurrentAggregateId = Guid.Empty;
            }
        }
    }
}
