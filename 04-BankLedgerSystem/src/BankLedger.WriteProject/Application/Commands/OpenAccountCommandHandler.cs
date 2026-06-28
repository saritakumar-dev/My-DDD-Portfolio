using BankLedger.Core.Common;
using BankLedger.Core.Common.Commands;
using BankLedger.WriteProject.Application.Common;
using BankLedger.WriteProject.Domain.Aggregates;

namespace BankLedger.WriteProject.Application.Commands
{
    public class OpenAccountCommandHandler : ICommandHandler<OpenAccountCommand>
    {
        private readonly IEventStore _eventStore;

        public OpenAccountCommandHandler(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }
        public async Task HandleAsync(OpenAccountCommand command, CancellationToken cancellationToken)
        {
            var account = new BankAccount(command.AccountId, command.CustomerName, command.Currency);
            await _eventStore.AppendEventsAsync(command.AccountId, account.Version, account.UncommittedEvents, cancellationToken);
            account.ClearUncommittedEvents();
        }
    }
}
