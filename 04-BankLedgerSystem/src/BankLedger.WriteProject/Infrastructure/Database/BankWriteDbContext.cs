using BankLedger.WriteProject.Application.Sagas;
using Microsoft.EntityFrameworkCore;

namespace BankLedger.WriteProject.Infrastructure.Database
{
    public class BankWriteDbContext : DbContext
    {

        public BankWriteDbContext(DbContextOptions<BankWriteDbContext> options) : base(options) { }
        public DbSet<MoneyTransferSagaState> Sagas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MoneyTransferSagaState>(entity =>
            {
                entity.ToTable("SagaStates");
                entity.HasKey(e => e.SagaId);

                // Configure properties to match standard banking sizes
                entity.Property(e => e.Amount)
                      .HasPrecision(18,4)
                      .IsRequired();

                // Store the State Enum as a readable string in MySQL instead of a number
                entity.Property(e => e.CurrentState)
                      .HasConversion<string>()
                      .HasMaxLength(50)
                      .HasColumnType("varchar(50)") // Explicitly maps to VARCHAR in MySQL
                      .IsRequired();
            });
        }
    }
}
