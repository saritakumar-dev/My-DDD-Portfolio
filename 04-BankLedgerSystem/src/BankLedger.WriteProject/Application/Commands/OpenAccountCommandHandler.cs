using BankLedger.Core.Common;
using BankLedger.Core.Common.Commands;
using BankLedger.Core.Common.Events;
using BankLedger.Core.Common.MessageBus;
using BankLedger.Domain;
using BankLedger.WriteProject.Application.Common;
using BankLedger.WriteProject.Domain.Aggregates;

namespace BankLedger.WriteProject.Application.Commands
{
    public class OpenAccountCommandHandler : ICommandHandler<OpenAccountCommand>
    {
        private readonly IEventStore _eventStore;
        private readonly IMessageBus _messageBus;

        public OpenAccountCommandHandler(IEventStore eventStore, IMessageBus messageBus)
        {
            _eventStore = eventStore;
            _messageBus = messageBus;
        }
        public async Task HandleAsync(OpenAccountCommand command, CancellationToken cancellationToken)
        {
            var account = new BankAccount(command.AccountId, command.CustomerName, command.Currency);
            var eventsToPublish = account.UncommittedEvents.ToList();
            await _eventStore.AppendEventsAsync(command.AccountId, account.Version, account.UncommittedEvents, cancellationToken);
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
