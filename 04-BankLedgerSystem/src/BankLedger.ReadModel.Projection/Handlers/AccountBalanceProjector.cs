
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
                var itemResponse = await _container.ReadItemAsync<AccountBalanceDocument>(id, partitionKey, null, cancellationToken);
                var accountBalanceDocument = itemResponse.Resource;
                accountBalanceDocument.Status = "Closed";
                accountBalanceDocument.CustomerName = "GDPR-REDONE-USER";
                await _container.UpsertItemAsync<AccountBalanceDocument>(accountBalanceDocument, partitionKey, null, cancellationToken);
                Console.WriteLine($"Account withKey {id} closed successfully");
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Key {id} was not found");
            }
        }

        public async Task HandleAsync(JournalEntryPostedEvent @event, CancellationToken cancellationToken)
        {
            try
            {
                foreach (var leg in @event.LedgerEntries)
                {
                    await _resiliencePolicy.ExecuteAsync(async () =>
                    {
                        var id = leg.AccountId.ToString();
                        var partitionKey = new PartitionKey(id);

                        var itemResponse = await _container.ReadItemAsync<AccountBalanceDocument>(id, partitionKey, cancellationToken: cancellationToken);

                        var accountBalanceDocument = itemResponse.Resource;

                        accountBalanceDocument.CurrentBalance += leg.Amount;

                        await _container.ReplaceItemAsync(accountBalanceDocument,
                            id,
                            partitionKey,
                            new ItemRequestOptions { IfMatchEtag = itemResponse.ETag },
                            cancellationToken: cancellationToken);
                    });
                }
            }
            catch (Exception ex)
            {
                // This block triggers ONLY if Polly exhausts all 3 retries and still fails
                _logger.LogCritical(ex, "CRITICAL: Event {EventId} failed to project after 3 retries. Admin notification triggered.", @event.JournalEntryId);

                // Invoke your administrative alert workflow here (e.g., writing to an AuditAlerts collection)
                await SaveToPoisonQueueAsync(@event, ex);

                // DO NOT rethrow the exception here! 
                // Swallowing it safely lets the event subscription advance to the next event.
            }
        }

        

        private async Task SaveToPoisonQueueAsync(JournalEntryPostedEvent @event, Exception ex)
        {
            // Simple internal persistence trick: store the un-projectable event payload 
            // into a separate CosmosDB container named "PoisonedEvents" for manual admin review.
            try
            {
                var poisonDoc = new PoisonEventDocument
                {
                    Id = Guid.NewGuid().ToString(),
                    StreamId = "journalentry-{@event.JournalEntryId}",
                    EventType = nameof(JournalEntryPostedEvent),
                    ErrorMessage = ex.Message,
                    StackTrace = ex.StackTrace ?? "No stack trace available",
                    RawEventPayload = @event,
                    LoggedAt = DateTime.UtcNow,
                };

                var itemResponse = await _poisonEventsContainer.CreateItemAsync<PoisonEventDocument>(poisonDoc, new PartitionKey(poisonDoc.StreamId), cancellationToken: default);

                var itemDocument = itemResponse.Resource;
            }
            catch (Exception poisonDBEx)
            {
                // Fail-Safe Net: If even the poison log database fails (e.g. Cosmos DB is completely down),
                // fallback to your system logger so the event is NEVER silently swallowed into total darkness.
                _logger.LogCritical(poisonDBEx, "FATAL ERROR: Failed to write event {@EventId} to the Cosmos DB Poison Container.", @event.JournalEntryId);

            }
        }
    }
}
