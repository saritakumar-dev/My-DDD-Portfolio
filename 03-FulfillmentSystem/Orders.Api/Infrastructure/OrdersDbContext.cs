using Microsoft.EntityFrameworkCore;
using Orders.Api.Domain;

namespace Orders.Api.Infrastructure
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions options) : base(options)
        {
        }

       public DbSet<Order> Orders { get; set; }

        public DbSet<OutboxMessage> OutboxMessages { get; set; }
    }
}
