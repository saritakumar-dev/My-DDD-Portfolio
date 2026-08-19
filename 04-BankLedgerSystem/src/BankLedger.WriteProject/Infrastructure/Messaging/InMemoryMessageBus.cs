using BankLedger.Core.Common.Events;
using BankLedger.Core.Common.MessageBus;
using BankLedger.ReadModel.Projection.Handlers;
using BankLedger.WriteProject.Application.Sagas;


namespace BankLedger.WriteProject.Infrastructure.Messaging
{
    public class InMemoryMessageBus : IMessageBus
    {
        private readonly AccountBalanceProjector _projector;
        private readonly MoneyTransferSaga _saga;

        // Explicit DI: Inject both consumers directly on the exact same thread scope
        public InMemoryMessageBus(AccountBalanceProjector projector, MoneyTransferSaga saga)
        {
            _projector = projector;
            _saga = saga;
        }
        public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
        {
            try
            {
                // Sequential Routing: Deliver each event type to BOTH the read projector and the saga process manager
                switch (@event)
                {
                    case AccountOpenedEvent openedEvent:
                        // 1. Update the Cosmos DB read cache
                        await _projector.HandleAsync(openedEvent, cancellationToken);
                        break;

                    case MoneyWithdrawnEvent withdrawnEvent:
                        // 1. Update the Cosmos DB read cache (Subtract balance)
                        await _projector.HandleAsync(withdrawnEvent, cancellationToken);
                        // 2. Notify the Saga to advance to the Deposit step
                        await _saga.HandleAsync(withdrawnEvent, cancellationToken);
                        break;

                    case MoneyDepositedEvent depositedEvent:
                        // 1. Update the Cosmos DB read cache (Add balance / Handle reversal)
                        await _projector.HandleAsync(depositedEvent, cancellationToken);
                        // 2. Notify the Saga to finalize the milestone check
                        await _saga.HandleAsync(depositedEvent, cancellationToken);
                        break;

                    case DepositMoneyFailedEvent failedEvent:
                        // 1. Notify the Saga to trigger the Compensating Transaction (Refund)
                        await _saga.HandleAsync(failedEvent, cancellationToken);
                        break;

                    case UserForgottenEvent userForgottenEvent:
                        // 1. Notify the Cosmos DB Container to delete the key
                        await _projector.HandleAsync(userForgottenEvent, cancellationToken);
                        break;

                    case JournalEntryPostedEvent journalEntryPostedEvent:
                        await _projector.HandleAsync(journalEntryPostedEvent, cancellationToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MESSAGE BUS ERROR] Failed to route event {@event.GetType().Name}: {ex.Message}");
            }

            await Task.CompletedTask;
        }
    }

}
