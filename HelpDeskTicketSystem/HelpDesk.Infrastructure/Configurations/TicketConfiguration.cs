using HelpDesk.Domain;
using HelpDesk.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace HelpDesk.Infrastructure.Configurations
{
    public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            builder.ToTable("Tickets");

            builder.Property<int>("TicketDbId").ValueGeneratedOnAdd().HasColumnName("Id");

            builder.HasKey("TicketDbId");

            builder.Property(t => t.TicketId)
                .HasConversion(id => id.Value, value => new TicketId(value))
                .HasColumnName("TicketId");

            builder.Property(t => t.Priority)
                .HasConversion(p => p.Value, value => new Priority(value))
                .HasMaxLength(20);

            builder.Property(t => t.Status)
                .HasConversion(s => s.Value, value => new TicketStatus(value))
                .HasMaxLength(20);

            builder.Property(t => t.CustomerId)
                .HasConversion(id => id.Value, value => new UserId(value))
                .HasColumnName("CustomerId")
                .IsRequired();

            builder.Property(t => t.AssignedAgentId)
                .HasConversion(id => id != null ? id.Value : (Guid?)null,
                          value => value.HasValue ? new UserId(value.Value) : null)
                .HasColumnName("AssignedAgent")
                .IsRequired(false);

            builder.Property(t => t.ManagerId)
                .HasConversion(id => id != null ? id.Value : (Guid?)null,
                              value => value.HasValue ? new UserId(value.Value) : null)
                .HasColumnName("ManagerId")
                .IsRequired(false);

            builder.OwnsOne(t => t.CurrentSla, sla =>
            {
                sla.Property(p => p.Deadline).HasColumnName("SlaDeadline");
            });

            builder.Property(t => t.CreatedAt).IsRequired();
            builder.Property(t => t.LastActivityAt);
            builder.Property(t => t.EscalatedAt);

            builder.OwnsMany(t => t.Messages, msg =>
            {
            msg.ToTable("TicketMessages");

            msg.Property(m => m.Content).HasColumnName("MessageContent");
                msg.Property(m => m.AuthorId)
                   .HasConversion(id => id.Value, value => new UserId(value))
                   .HasColumnName("AuthorId")
                   .HasColumnType("Text")
                   .IsRequired();

                msg.WithOwner().HasForeignKey("TicketId");
                msg.Property<int>("Id").ValueGeneratedOnAdd();
                msg.HasKey("Id");
            });

            

            builder.Property<bool>("IsOpenedByAgent").HasColumnName("IsOpenedByAgent");

            builder.Property(t => t.TicketClosedBy)
                   .HasConversion(id => id != null ? id.Value : (Guid?)null,
                                  value => value.HasValue ? new UserId(value.Value) : null)
                    .HasColumnName("ClosedById")
                    .IsRequired(false);

            builder.Property(t => t.TicketClosedAt)
                .HasColumnName("ClosedAt")
                .IsRequired(false);

            builder.Property(t => t.LastReopenedAt)
                .HasColumnName("LastReopenedAt")
                .IsRequired(false);

            builder.Property(t=>t.LastReopenedBy)
                .HasConversion(id => id != null ? id.Value : (Guid?)null,
                                  value => value.HasValue ? new UserId(value.Value) : null)
                .HasColumnName("ReopenedById")
                .IsRequired(false);

            builder.Property(t => t.ReopenedCount)
                .HasColumnName("ReopenCount")
                .IsRequired(false);

        }
    }
}
