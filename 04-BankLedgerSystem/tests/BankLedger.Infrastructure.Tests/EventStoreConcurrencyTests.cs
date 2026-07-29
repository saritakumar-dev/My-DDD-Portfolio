using BankLedger.Core.Common.Events;
using BankLedger.WriteProject.Infrastructure.Database;
using Moq;


namespace BankLedger.Infrastructure.Tests
{
    public class EventStoreConcurrencyTests
    {
        // Mock connection string pointing to your local testing schema
        private const string TestConnectionString = "Server=localhost;Port=3306;Database=bankledgersystem;Uid=root;Pwd=MyNewSecurePassword123!;AllowUserVariables=True;";
        private readonly Mock<ICryptoKeyVault> _cryptoKeyVaultMock;

        public EventStoreConcurrencyTests()
        {
            _cryptoKeyVaultMock = new Mock<ICryptoKeyVault>();
        }

        /* To unit test the MySQL Optimistic Concurrency Lock(the UQ_Aggregate_Version duplication error), we must write an 
           Integration Test that uses a mock Event Store to simulate two threads trying to write version 2 at the exact same millisecond. */

        [Fact]
        public async Task AppendEvents_SimultaneousDuplicateVersions_ShouldThrowConcurrencyException()
        {
            var eventStore = new MySqlEventStore(TestConnectionString, _cryptoKeyVaultMock.Object);
            var accountId = Guid.NewGuid();

            using var aes = System.Security.Cryptography.Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();
            string fakeBase64Key = Convert.ToBase64String(aes.Key);

            _cryptoKeyVaultMock.Setup(vault => vault.GetOrCreateKeyAsync(accountId)).ReturnsAsync(fakeBase64Key);

            var initialEvent = new AccountOpenedEvent(accountId, "Concurrency Tester", "USD", 1);
            await eventStore.AppendEventsAsync(accountId, 0, new[] { initialEvent },  CancellationToken.None);

            // 2.Create Thread A attempting to save a deposit at Version 2
            var eventA = new MoneyDepositedEvent(accountId, 100.00m, "EUR", "Thread A Win", 2);

            // 3. Create Thread B attempting to save a separate deposit ALSO at Version 2
            var eventB = new MoneyDepositedEvent(accountId, 50.00m, "EUR", "Thread B Conflict", 2);

            // Thread A saves successfully, occupying Version 2 slot in MySQL
            await eventStore.AppendEventsAsync(accountId, 1, new[] { eventA }, CancellationToken.None);

            // 4. Thread B attempts to save Version 2 again. 
            // The UNIQUE database constraint must trigger an InvalidOperationException.
            var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await eventStore.AppendEventsAsync(accountId, 1, new[] { eventB }, CancellationToken.None));

            Assert.Contains("Concurrency Exception", exception.Result.Message);
        }
    }
}