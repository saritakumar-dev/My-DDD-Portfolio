using BankLedger.Core.Common.MessageBus;
using BankLedger.WriteProject.Application;
using BankLedger.WriteProject.Application.Handlers;
using BankLedger.WriteProject.Infrastructure.Database;
using Microsoft.Extensions.Logging;
using Moq;
using MySql.Data.MySqlClient;

namespace BankLedger.IntegrationTests
{
    public class SnapshotPatternIntegrationTests
    {

        private const string ConnectionString = "Server=localhost;Port=3306;Database=bankledgersystem;Uid=root;Pwd=MyNewSecurePassword123!;AllowUserVariables=True;";
        private readonly MySqlEventStore _eventStore;
        private readonly Mock<IMessageBus> _messageBus;
        private readonly MySqlSnapshotStore _snapshotStore;
        private readonly ICryptoKeyVault _keyVault;
        private readonly Mock<ILogger<MySqlEventStore>> _mockLogger;
        private readonly OpenAccountCommandHandler _openAccountCommandHandler;
        private readonly DepositMoneyCommandHandler _depositMoneyCommandHandler;
        public SnapshotPatternIntegrationTests()
        {
            _snapshotStore = new MySqlSnapshotStore(ConnectionString);
            _keyVault = new MySqlCryptoKeyVault(ConnectionString);
            _mockLogger = new Mock<ILogger<MySqlEventStore>>();
            _eventStore = new MySqlEventStore(ConnectionString, _keyVault, _mockLogger.Object);
            _messageBus = new Mock<IMessageBus>();
            // Injecting the ADO.NET dependencies directly into your handler
            _openAccountCommandHandler= new OpenAccountCommandHandler(_eventStore, _messageBus.Object);
            _depositMoneyCommandHandler = new DepositMoneyCommandHandler(_eventStore, _messageBus.Object, _snapshotStore, 3);

        }

        [Fact]
        public async Task DepositFlow_ShouldGenerateSnapshotOnThreshold_AndLoadEfficientlyOnSubsequentRequests()
        {
            var accountId = Guid.NewGuid();
            var currency = "EUR";

            await _openAccountCommandHandler.HandleAsync(new OpenAccountCommand(accountId,"alice", currency), CancellationToken.None);
            await _depositMoneyCommandHandler.HandleAsync(new DepositMoneyCommand(accountId, 100, currency, "initial cash deposit"), CancellationToken.None);

            Assert.Equal(2, GetEventCount(accountId));
            Assert.Equal(0, GetSnapshotCount(accountId));

            await _depositMoneyCommandHandler.HandleAsync(new DepositMoneyCommand(accountId, 20, currency, "cheque dpeosit"), CancellationToken.None);
            Assert.Equal(3, GetEventCount(accountId));
            Assert.Equal(1, GetSnapshotCount(accountId));

            var latestSnapshot = await _snapshotStore.GetLatestAsync(accountId);
            Assert.NotNull(latestSnapshot);
            Assert.Equal(3, latestSnapshot.Version);
            Assert.Equal(120.00m, latestSnapshot.Balance.Amount); // 100 +  20
            Assert.Equal("EUR", latestSnapshot.Balance.Currency);
        }

        #region Database Helper Methods

        private int GetEventCount(Guid aggregateId)
        {
            const string query = "SELECT COUNT(*) FROM EventStore WHERE AggregateId = @AggregateId;";
            using var connection = new MySqlConnection(ConnectionString);
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@AggregateId", aggregateId.ToString());
            connection.Open();
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private int GetSnapshotCount(Guid aggregateId)
        {
            const string query = "SELECT COUNT(*) FROM bankaccountsnapshots WHERE AggregateId = @AggregateId;";
            using var connection = new MySqlConnection(ConnectionString);
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@AggregateId", aggregateId.ToString());
            connection.Open();
            return Convert.ToInt32(command.ExecuteScalar());
        }

        #endregion
    }
}