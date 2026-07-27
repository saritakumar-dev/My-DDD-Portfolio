
using BankLedger.Core.Common;
using BankLedger.Core.Common.Events;
using BankLedger.ReadModel.Projection.Common.Models;
using Microsoft.Azure.Cosmos;

namespace BankLedger.ReadModel.Projection.Handlers
{
    public class AccountBalanceProjector : IDomainEventHandler<AccountOpenedEvent>,
                                           IDomainEventHandler<MoneyDepositedEvent>,
                                           IDomainEventHandler<MoneyWithdrawnEvent>,
                                           IDomainEventHandler<UserForgottenEvent>

    {
        private readonly Container _container;

        public AccountBalanceProjector(CosmosClient cosmosClient)
        {
            _container = cosmosClient.GetContainer("BankLedgerReadDb", "Balances");
        }
        public async Task HandleAsync(AccountOpenedEvent @event, CancellationToken cancellationToken = default)
        {
            try
            {
                var id = @event.AggregateId.ToString();
                var partitionKey = new PartitionKey(id);
                var document = new AccountBalanceDocument
                {
                    Id = id,
                    AggregateId = id,
                    CustomerName = @event.CustomerName,
                    Currency = @event.Currency,
                    CurrentBalance = 0,
                    LastProcessedVersion = @event.Version
                };

                await _container.CreateItemAsync(document, partitionKey, cancellationToken: cancellationToken);
                Console.WriteLine("[PROJECTOR] Document successfully written to Cosmos DB.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL PROJECTOR ERROR] Cosmos save failed: {ex.Message}");
            }
        }

        public async Task HandleAsync(MoneyDepositedEvent @event, CancellationToken cancellationToken)
        {
            try
            {
                var id = @event.AggregateId.ToString();
                var partitionKey = new PartitionKey(id);
                ItemResponse<AccountBalanceDocument> itemResponse = await _container.ReadItemAsync<AccountBalanceDocument>(id, partitionKey, cancellationToken: cancellationToken);
                var currentDocument = itemResponse.Resource;

                // CQRS Invariant Safeguard: Prevent duplicate or out-of-order event updates
                if (@event.Version <= currentDocument.LastProcessedVersion) return;

                if (@event.Reference.Contains("REVERSAL"))
                {
                    Console.WriteLine($"[PROJECTOR - REVERSAL] Processing refund of {@event.Amount} back to Account {id}.");
                }
                else
                {
                    Console.WriteLine($"[PROJECTOR - CREDIT] Processing standard deposit of {@event.Amount} to Account {id}.");
                }

                var updatedDocument = new AccountBalanceDocument
                {
                    Id = currentDocument.Id,
                    AggregateId = currentDocument.AggregateId,
                    CustomerName = currentDocument.CustomerName,
                    Currency = currentDocument.Currency,

                    // Apply the updated metrics safely here
                    CurrentBalance = currentDocument.CurrentBalance + @event.Amount,
                    LastProcessedVersion = @event.Version
                };

                await _container.UpsertItemAsync(updatedDocument, partitionKey, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL PROJECTOR ERROR] Cosmos save failed: {ex.Message}");
            }
        }

        public async Task HandleAsync(MoneyWithdrawnEvent @event, CancellationToken cancellationToken = default)
        {
            try
            {
                var id = @event.AggregateId.ToString();
                var partitionKey = new PartitionKey(id);
                ItemResponse<AccountBalanceDocument> itemResponse = await _container.ReadItemAsync<AccountBalanceDocument>(id, partitionKey, cancellationToken: cancellationToken);
                var document = itemResponse.Resource;

                if (@event.Version <= document.LastProcessedVersion) return;

                document.CurrentBalance -= @event.Amount;
                document.LastProcessedVersion = @event.Version;

                await _container.UpsertItemAsync(document, partitionKey, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL PROJECTOR ERROR] Cosmos save failed: {ex.Message}");
            }
        }

        public async Task HandleAsync(UserForgottenEvent @event, CancellationToken cancellationToken)
        {
            // Physically delete the mutable account balance document out of Cosmos DB cache
            string id = @event.AggregateId.ToString();
            var partitionKey = new PartitionKey(id);
            try
            {
                await _container.DeleteItemAsync<AccountBalanceDocument>(id, partitionKey, null, cancellationToken);
                Console.WriteLine($"Key {id} deleted successfully");
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Key {id} was not found");
            }
        }
    }
}
