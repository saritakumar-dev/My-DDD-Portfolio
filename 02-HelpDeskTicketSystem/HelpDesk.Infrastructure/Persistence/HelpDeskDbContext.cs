using HelpDesk.Domain;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Persistence
{
    public class HelpDeskDbContext : DbContext
    {
        public HelpDeskDbContext(DbContextOptions<HelpDeskDbContext> options) : base(options) { }

        
        public DbSet<Ticket> Tickets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(HelpDeskDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
       
    }
}
