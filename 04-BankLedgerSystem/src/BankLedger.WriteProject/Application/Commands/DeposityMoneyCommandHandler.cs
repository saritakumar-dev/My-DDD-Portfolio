using BankLedger.Core.Common;
using BankLedger.Core.Common.Commands;
using BankLedger.Core.Common.Events;
using BankLedger.Core.Common.MessageBus;
using BankLedger.WriteProject.Application.Common;
using BankLedger.WriteProject.Domain.Aggregates;


namespace BankLedger.WriteProject.Application.Commands
{
    public class DepositMoneyCommandHandler : ICommandHandler<DepositMoneyCommand>
    {
        private readonly IEventStore _eventStore;
        private readonly IMessageBus _messageBus;

        public DepositMoneyCommandHandler(IEventStore eventStore, IMessageBus messageBus)
        {
            _eventStore = eventStore;
            _messageBus = messageBus;
        }

        public async Task HandleAsync(DepositMoneyCommand command, CancellationToken cancellationToken)
        {
            IList<BankEvent> history = new List<BankEvent>();
            try
            {
                history = await _eventStore.GetEventsAsync(command.AccountId, cancellationToken);

                if (!history.Any()) throw new KeyNotFoundException("Account not found.");

                var account = BankAccount.LoadFromHistory(history);

                account.Deposit(command.Amount, command.Reference);

                var eventsToPublish = account.UncommittedEvents.ToList();

                await _eventStore.AppendEventsAsync(command.AccountId, account.Version, account.UncommittedEvents, cancellationToken);

                account.ClearUncommittedEvents();

                // Broadcast to the generic bus.
                foreach (var @event in eventsToPublish)
                {
                    if (@event is MoneyDepositedEvent depositedEvent)
                        await _messageBus.PublishAsync(depositedEvent, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HANDLER ERROR] Deposit failed for account {command.AccountId}: {ex.Message}");

                var currentAccountVersion = history.Any() ? history.Max(e => e.Version) : 0;
                var failureEvent = new DepositMoneyFailedEvent
                (
                    command.AccountId,
                    command.Amount,
                    command.Reference,
                    ex.Message.ToString(),
                    currentAccountVersion
                );

                await _messageBus.PublishAsync(failureEvent, cancellationToken);
            }
        }
    }
}
