using Microsoft.EntityFrameworkCore;
using Orders.Api.Domain;
using System.Text.Json;

namespace Orders.Api.Infrastructure
{
    public class OrderRepository : IOrderRepository
    {
        private readonly OrderDbContext _context;

        public OrderRepository(OrderDbContext context) { _context = context; }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await _context.Orders.FindAsync(id);
        }


        //Outbox pattern
        public async Task SaveOrderAsync(Order order, OutboxMessage outboxMessage)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                outboxMessage.AggregateId = order.Id;
                // to ensure the Content has orderId
                outboxMessage.Content = JsonSerializer.Serialize(new
                {
                    OrderId =order.Id,
                    order.CustomerId,
                    order.BillingAddress,
                    ShippingAddress = order.ShippingAddress,
                    order.TotalAmount,
                    Currency = "USD"
                });
                _context.OutboxMessages.Add(outboxMessage);
                await _context.SaveChangesAsync();

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
