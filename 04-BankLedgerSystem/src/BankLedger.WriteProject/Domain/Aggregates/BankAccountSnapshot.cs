

namespace BankLedger.WriteProject.Domain.Aggregates
{
    public class BankAccountSnapshot
    {
        public Guid AggregateId { get; set; }
        public int Version { get; set; }
        public decimal Balance { get; set; }
        public string Currency { get; set; }
        public DateTime SnapshottedAt { get; set; }
    }
}
