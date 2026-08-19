using BankLedger.Domain.Common;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System.Net;

namespace BankLedger.ReadModel.Projection.Handlers
{
    public class CosmosAccountReadModelService : IAccountReadModelService
    {
        private readonly Container _container;

        public CosmosAccountReadModelService(CosmosClient cosmosClient, ILogger<AccountBalanceProjector> logger)
        {
            _container = cosmosClient.GetContainer("BankLedgerReadDb", "Balances");
        }
        public async Task<AccountStateResult> GetAccountStateResultAsync(Guid accountId, CancellationToken cancellationToken)
        {
            try
            {
                var id = Convert.ToString(accountId);
                var itemresponse = await _container.ReadItemAsync<AccountStateResult>(id, new PartitionKey(id), cancellationToken: cancellationToken);
                var document = itemresponse.Resource;

                return new AccountStateResult(document.AccountId, document.IsClosed, document.CurrentBalance);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Edge Case Protection: If an account doesn't exist on the read-side yet, 
                // treat it as closed/invalid so the Saga immediately blocks any money movement.
                return new AccountStateResult(Guid.Empty, IsClosed: true, CurrentBalance: 0.00m);
            }
        }
    }
}
