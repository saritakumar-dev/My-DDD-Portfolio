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

        public WithdrawMoneyCommandHandler(IEventStore eventStore, IMessageBus messageBus)
        {
            _eventStore = eventStore;
            _messageBus = messageBus;
        }
        public async Task HandleAsync(WithdrawMoneyCommand command, CancellationToken cancellationToken)
        {
            BankAccount? account = null;
            try
            {
                var history = await _eventStore.GetEventsAsync(command.AccountId, cancellationToken);

                if (!history.Any()) throw new KeyNotFoundException("Account not found.");

                account = BankAccount.LoadFromHistory(history);

                account.Withdraw(command.Amount, command.Reference);

                var eventsToPublish = account.UncommittedEvents.ToList();

                await _eventStore.AppendEventsAsync(command.AccountId, account.Version, account.UncommittedEvents, cancellationToken);



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
