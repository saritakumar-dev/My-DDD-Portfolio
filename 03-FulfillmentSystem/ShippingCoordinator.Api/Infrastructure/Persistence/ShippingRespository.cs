using Microsoft.EntityFrameworkCore;
using ShippingCoordinator.Api.Domain;
using ShippingCoordinator.Api.Domains;
using ShippingCoordinator.Api.Infrastructure.Model;

namespace ShippingCoordinator.Api.Infrastructure.Persistence
{
    public class ShippingRespository(ShippingCoordinatorDBContext dbContext) : IShippingRespository
    {
        private readonly ShippingCoordinatorDBContext _dbContext = dbContext;

        public async Task<bool> IsMessageProcessedAsync(int eventId)
        {
            return await _dbContext.InboxMessages.AnyAsync(m => m.EventId == eventId);
        }

        public async Task SaveShippingDetailAsync(ShippingDetail shippingDetail, InboxMessage inboxMessage)
        {

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                _dbContext.ShippingDetails.Add(shippingDetail);
                await _dbContext.SaveChangesAsync();

                inboxMessage.AggregateId = shippingDetail.Id;
                _dbContext.InboxMessages.Add(inboxMessage);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
