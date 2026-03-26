using ShippingCoordinator.Api.Domains;
using ShippingCoordinator.Api.Infrastructure.Model;

namespace ShippingCoordinator.Api.Domain
{
    public interface IShippingRespository
    {
        Task<bool> IsMessageProcessedAsync(int eventId);
        Task SaveShippingDetailAsync(ShippingDetail shippingDetail, InboxMessage inboxMessage);
    }
}
