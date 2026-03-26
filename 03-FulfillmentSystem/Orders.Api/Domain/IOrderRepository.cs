using Orders.Api.Infrastructure;

namespace Orders.Api.Domain
{
    public interface IOrderRepository
    {
        Task<Order> GetOrderByIdAsync(int id);
        Task SaveOrderAsync(Order order, OutboxMessage outboxMessage);

    }
}
