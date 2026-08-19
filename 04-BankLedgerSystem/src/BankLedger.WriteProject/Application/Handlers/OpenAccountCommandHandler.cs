using BankLedger.Core.Common;
using BankLedger.Core.Common.Events;
using BankLedger.Core.Common.MessageBus;
using BankLedger.Domain.Aggregates;
using BankLedger.WriteProject.Application.Common;

namespace BankLedger.WriteProject.Application.Handlers
{
    public class OpenAccountCommandHandler : ICommandHandler<OpenAccountCommand>
    {
        private readonly IEventStore _eventStore;
        private readonly IMessageBus _messageBus;
        private const string StreamCategory = "Bank Account";
        public OpenAccountCommandHandler(IEventStore eventStore, IMessageBus messageBus)
        {
            _eventStore = eventStore;
            _messageBus = messageBus;
        }
        public async Task HandleAsync(OpenAccountCommand command, CancellationToken cancellationToken)
        {
            var account = new BankAccount(command.AccountId, command.CustomerName, command.Currency);
            var eventsToPublish = account.UncommittedEvents.ToList();
            await _eventStore.AppendEventsAsync(command.AccountId, StreamCategory, account.Version, account.UncommittedEvents, cancellationToken);
            account.ClearUncommittedEvents();

            // Broadcast to the generic bus.
            foreach (var @event in eventsToPublish)
            {
                if (@event is AccountOpenedEvent accountOpenedEvent)
                    await _messageBus.PublishAsync(accountOpenedEvent, cancellationToken);
            }
        }
    }
}
