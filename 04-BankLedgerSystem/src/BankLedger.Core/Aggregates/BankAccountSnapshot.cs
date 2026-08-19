

using BankLedger.Domain.ValueObjects;

namespace BankLedger.WriteProject.Domain.Aggregates
{
    public class BankAccountSnapshot
    {
        public Guid AggregateId { get; set; }
        public int Version { get; set; }
        public bool IsClosed { get; set; }

        public Money Balance { get; set; } = Money.Zero();
        public DateTime SnapshottedAt { get; set; }
    }
}
