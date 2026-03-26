

using Microsoft.EntityFrameworkCore;
using ShippingCoordinatorService.Domains;

namespace ShippingCoordinatorService.Infrastructure
{
    public class ShippingCoordinatorDBContext: DbContext
    {
        public ShippingCoordinatorDBContext(DbContextOptions<ShippingCoordinatorDBContext> options) : base(options) { }

        public DbSet<ShippingDetail> ShippingDetails { get; set; }

        public DbSet<InboxMessage> InboxMessages { get; set; }
    }
}
