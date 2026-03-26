using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;


namespace PaymentService.Infrastructure
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<InboxMessage> InboxMessages { get; set; }

        public DbSet<OutboxMessage> OutboxMessages { get; set; }
    }
}
