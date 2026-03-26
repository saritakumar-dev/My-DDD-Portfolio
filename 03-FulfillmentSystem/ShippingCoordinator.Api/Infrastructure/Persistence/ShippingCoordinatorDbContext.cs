
using Microsoft.EntityFrameworkCore;
using ShippingCoordinator.Api.Domains;
using ShippingCoordinator.Api.Infrastructure.Model;

namespace ShippingCoordinator.Api.Infrastructure
{
    public class ShippingCoordinatorDBContext: DbContext
    {
        public ShippingCoordinatorDBContext(DbContextOptions<ShippingCoordinatorDBContext> options) : base(options) { }

        public DbSet<ShippingDetail> ShippingDetails { get; set; }

        public DbSet<InboxMessage> InboxMessages { get; set; }
    }
}
