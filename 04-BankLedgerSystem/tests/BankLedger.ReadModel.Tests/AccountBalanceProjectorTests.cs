using BankLedger.Core.Common.Events;
using BankLedger.ReadModel.Projection.Common.Models;
using BankLedger.ReadModel.Projection.Handlers;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using PartitionKey = Microsoft.Azure.Cosmos.PartitionKey;

namespace BankLedger.ReadModel.Tests
{
    public class AccountBalanceProjectorTests
    {
        private readonly Mock<Container> _mockContainer;
        private readonly Mock<CosmosClient> _mockClient;
        private readonly AccountBalanceProjector _projector;
        private readonly Mock<ILogger<AccountBalanceProjector>> _mockLogger;
        public AccountBalanceProjectorTests()
        {
            _mockContainer = new Mock<Container>();
            _mockClient = new Mock<CosmosClient>();
            _mockLogger= new Mock<ILogger<AccountBalanceProjector>>();
            _mockClient.Setup(m => m.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                       .Returns(_mockContainer.Object);
            _projector = new AccountBalanceProjector(_mockClient.Object, _mockLogger.Object);
        }


        [Fact]
        public async Task HandleDeposit_StaleOrDuplicateVersion_ShouldSilentlyIgnoreAndNotUpsert()
        {
            var accountId = Guid.NewGuid();
            AccountBalanceDocument existingAccountSnapshot = new AccountBalanceDocument
            {
                Id = accountId.ToString(),
                PartitionKey = accountId.ToString(),
                Currency = "USD",
                CurrentBalance = 500.00m,
                CustomerName = "Alice",
                LastProcessedVersion = 5  // Snapshot is currently at Version 5
            };

            var mockResponse = new Mock<ItemResponse<AccountBalanceDocument>>();
            mockResponse.Setup(r => r.Resource).Returns(existingAccountSnapshot);

            _mockContainer.Setup(c => c.ReadItemAsync<AccountBalanceDocument>(
                                accountId.ToString(),
                                It.IsAny<PartitionKey>(),
                                null, default)).ReturnsAsync(mockResponse.Object);

            // 2. WHEN: A delayed, out-of-order event arrives claiming Version 4 
            var staleEvent = new MoneyDepositedEvent(accountId, 100.00m, "USD", "test cash deposit", 4);
            await _projector.HandleAsync(staleEvent, CancellationToken.None);

            // 3. THEN: The denormalizer must safely drop the event and NEVER call UpsertItemAsync
            _mockContainer.Verify(c => c.UpsertItemAsync(It.IsAny<AccountBalanceDocument>(),
                It.IsAny<PartitionKey>(), null, CancellationToken.None), Times.Never);
        }
    }
}