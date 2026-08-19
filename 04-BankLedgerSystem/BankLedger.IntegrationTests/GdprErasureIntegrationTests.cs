using BankLedger.Core.Common;
using BankLedger.Core.Common.Events;
using BankLedger.Core.Common.MessageBus;
using BankLedger.Domain;
using BankLedger.Domain.Common;
using BankLedger.WriteProject.Application;
using BankLedger.WriteProject.Application.Common;
using BankLedger.WriteProject.Application.Handlers;
using BankLedger.WriteProject.Infrastructure.Database;
using Microsoft.Extensions.Logging;
using Moq;
using MySql.Data.MySqlClient;


namespace BankLedger.IntegrationTests
{
    public class GdprErasureIntegrationTests
    {
        private const string ConnectionString = "Server=localhost;Port=3306;Database=bankledgersystem;Uid=root;Pwd=MyNewSecurePassword123!;AllowUserVariables=True;";
        private readonly ISnapshotStore _snapshotStore;
        private readonly ICryptoKeyVault _keyVault;
        private readonly IEventStore _eventStore;
        private readonly Mock<IMessageBus> _messageBusMock;
        private readonly ICommandHandler<OpenAccountCommand> _openAccountCommandHandler;
        private readonly ICommandHandler<ForgetUserCommand> _forgetUserCommandhandler;
        private readonly Mock<ILogger<MySqlEventStore>> _mockLogger;
        public GdprErasureIntegrationTests()
        {
            // Initialize infrastructure components
            _snapshotStore = new MySqlSnapshotStore(ConnectionString);
            _keyVault = new MySqlCryptoKeyVault(ConnectionString);
            _messageBusMock = new Mock<IMessageBus>();
            _mockLogger = new Mock<ILogger<MySqlEventStore>>();
            // Wire the EventStore using the registered CryptoKeyVault wrapper
            _eventStore = new MySqlEventStore(ConnectionString, _keyVault, _mockLogger.Object);

            // Initialize the handler under test
            _openAccountCommandHandler = new OpenAccountCommandHandler(_eventStore, _messageBusMock.Object);
            _forgetUserCommandhandler = new ForgetUserCommandHandler(_keyVault, _eventStore, _messageBusMock.Object, _snapshotStore);

            ClearDatabaseTables();
        }

        [Fact]
        public async Task ForgetUserCommand_ShouldShredKey_AndAnonymiseEventStorePayloads()
        {
            var accountId = Guid.NewGuid();
            var cancellationToken = CancellationToken.None;

            var initialEvents = new List<BankEvent>
            {
                new AccountOpenedEvent
                {
                    AggregateId = accountId,
                    CustomerName = "John Doe",
                    Currency = "EUR"
                }
            };

            try
            {
                AmbientContext.CurrentAggregateId = accountId;
                await _openAccountCommandHandler.HandleAsync(new OpenAccountCommand(accountId, "John Doe", "EUR"), cancellationToken);
            }
            finally
            {
                AmbientContext.CurrentAggregateId = Guid.Empty;
            }

            // Verify Baseline: Verify the raw text string is successfully Encrypted and scrambled on disk
            string rawPayloadFromDb = GetRawEventDataFromDatabase(accountId, version: 1);
            Assert.False(rawPayloadFromDb.Contains("John Doe")); // Cryptography validation

            var command = new ForgetUserCommand(accountId, ClosureReason.GdprRequest);
            await _forgetUserCommandhandler.HandleAsync(command, cancellationToken);

            const int expectedErasureVersion = 2;
            string erasureEventPayload = GetRawEventDataFromDatabase(accountId, expectedErasureVersion);
            Assert.True(erasureEventPayload.Contains("erasedAt", StringComparison.InvariantCultureIgnoreCase));

            List<BankEvent> replayedHistory;
            try
            {
                AmbientContext.CurrentAggregateId = accountId;
                replayedHistory = await _eventStore.GetEventsAsync(accountId, 0, cancellationToken);
            }
            finally
            {
                AmbientContext.CurrentAggregateId = Guid.Empty;
            }

            var replayedOpenedEvent = replayedHistory.OfType<AccountOpenedEvent>().First();

            // 🌟 THE ULTIMATE COMPLIANCE CHECK: after the key is shred, the customer's real name is lost forever and
            // the JSON converter safely outputs the data erasure token
            Assert.Equal("[DATA_ERASED_UNDER_GDPR]", replayedOpenedEvent.CustomerName);


            // Assert : Verify the message bus asynchronously broadcasted the transaction
            // 🌟 Verify that PublishAsync was called exactly once with our target event type
            _messageBusMock.Verify(bus => bus.PublishAsync(
                It.Is<UserForgottenEvent>(e => e.AggregateId == accountId && e.Version == expectedErasureVersion),
                cancellationToken),
                Times.Once);

        }

        #region Operational Database and Messaging Helpers

        private string GetRawEventDataFromDatabase(Guid aggregateId, int version)
        {
            const string query = "SELECT EventData FROM EventStore WHERE AggregateId = @AggregateId AND Version = @Version LIMIT 1;";
            using var connection = new MySqlConnection(ConnectionString);
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@AggregateId", aggregateId.ToString());
            command.Parameters.AddWithValue("@Version", version);

            connection.Open();
            var result = command.ExecuteScalar();
            return result?.ToString() ?? string.Empty;
        }

        private void ClearDatabaseTables()
        {
            using var connection = new MySqlConnection(ConnectionString);
            connection.Open();
            using var command = new MySqlCommand("TRUNCATE TABLE EventStore; TRUNCATE TABLE DomainEncryptionKeys; TRUNCATE TABLE BankAccountSnapshots;", connection);
            command.ExecuteNonQuery();
        }

        #endregion
    }
}
