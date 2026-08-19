using BankLedger.Core.Common.Events;
using BankLedger.Domain.Aggregates;
using BankLedger.WriteProject.Application.Common.Exceptions;
using BankLedger.WriteProject.Infrastructure.Database;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MySql.Data.MySqlClient;


namespace BankLedger.IntegrationTests
{
    public class MySqlEventStoreTests
    {
        // Mock connection string pointing to your local testing schema
        private const string TestConnectionString = "Server=localhost;Port=3306;Database=bankledgersystem;Uid=root;Pwd=MyNewSecurePassword123!;AllowUserVariables=True;";
        private const string BankAccountCategory = "BankAccount";
        private const string JournalCategory = "JournalEntry";
        private readonly Mock<ICryptoKeyVault> _cryptoKeyVaultMock;
        private readonly Mock<ILogger<MySqlEventStore>> _mockLogger;
        private readonly MySqlEventStore _eventStore;
        private readonly List<string> _trackedStreamIds = new();
        public MySqlEventStoreTests()
        {
            _cryptoKeyVaultMock = new Mock<ICryptoKeyVault>();
            _mockLogger = new Mock<ILogger<MySqlEventStore>>();
            _eventStore = new MySqlEventStore(TestConnectionString, _cryptoKeyVaultMock.Object, _mockLogger.Object);
        }

        /* To unit test the MySQL Optimistic Concurrency Lock(the UQ_Aggregate_Version duplication error), we must write an 
           Integration Test that uses a mock Event Store to simulate two threads trying to write version 2 at the exact same millisecond. */

        [Fact]
        public async Task AppendEvents_SimultaneousDuplicateVersions_ShouldThrowConcurrencyException()
        {

            var accountId = Guid.NewGuid();

            using var aes = System.Security.Cryptography.Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();
            string fakeBase64Key = Convert.ToBase64String(aes.Key);

            _cryptoKeyVaultMock.Setup(vault => vault.GetOrCreateKeyAsync(accountId)).ReturnsAsync(fakeBase64Key);

            var initialEvent = new AccountOpenedEvent(accountId, "Concurrency Tester", "USD", 1);
            await _eventStore.AppendEventsAsync(accountId, BankAccountCategory, 0, new[] { initialEvent }, CancellationToken.None);

            // 2.Create Thread A attempting to save a deposit at Version 2
            var eventA = new MoneyDepositedEvent(accountId, 100.00m, "EUR", "Thread A Win", 2);

            // 3. Create Thread B attempting to save a separate deposit ALSO at Version 2
            var eventB = new MoneyDepositedEvent(accountId, 50.00m, "EUR", "Thread B Conflict", 2);

            // Thread A saves successfully, occupying Version 2 slot in MySQL
            await _eventStore.AppendEventsAsync(accountId, BankAccountCategory, 1, new[] { eventA }, CancellationToken.None);

            // 4. Thread B attempts to save Version 2 again. 
            // The UNIQUE database constraint must trigger an InvalidOperationException.
            var exception = Assert.ThrowsAsync<ApplicationDomainException>(async () =>
                await _eventStore.AppendEventsAsync(accountId, BankAccountCategory, 1, new[] { eventB }, CancellationToken.None));

            Assert.Contains("The financial aggregate resource", exception.Result.Message);
        }

        [Fact]
        public async Task AppendEventsAsync_ShouldPrependStreamPrefix_ToPreserveRawGuidTyping()
        {
            // Arrange: Use the journal entry aggregate identifier
            var journalEntryId = Guid.NewGuid();
            var expectedFullStreamId = $"{JournalCategory}-{journalEntryId}";
            _trackedStreamIds.Add(expectedFullStreamId);

            var uncommittedEvents = new[]
            {
            new JournalEntryPostedEvent(journalEntryId, new List<LedgerEntry>())
        };

            // Act: Append using the distinct journalentry category prefix
            await _eventStore.AppendEventsAsync(journalEntryId, JournalCategory, 1, uncommittedEvents, CancellationToken.None);

            // Assert: Open a raw ADO.NET connection to verify the row level state in MySQL
            using var connection = new MySqlConnection(TestConnectionString);
            await connection.OpenAsync();

            var row = await connection.QuerySingleOrDefaultAsync<dynamic>(
                "SELECT StreamCategory, Version FROM eventstore WHERE AggregateId = @AggregateId",
                new { AggregateId = journalEntryId }
            );

            ((object)row).Should().NotBeNull();
            ((string)row.StreamCategory).Should().Be(JournalCategory);
            ((int)row.Version).Should().Be(1);
        }
        public async Task InitializeAsync() => await Task.CompletedTask;
        public async Task DisposeAsync()
        {
            if (!_trackedStreamIds.Any()) return;

            using var connection = new MySqlConnection(TestConnectionString);
            await connection.OpenAsync();

            // Aligned to target your explicit AggregateId structure cleanly
            await connection.ExecuteAsync(
                "DELETE FROM eventstore WHERE AggregateId IN (@Ids)",
                new { Ids = _trackedStreamIds }
            );
        }
    }
}