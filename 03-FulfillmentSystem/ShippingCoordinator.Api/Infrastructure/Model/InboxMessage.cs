namespace ShippingCoordinator.Api.Infrastructure.Model
{
    public class InboxMessage
    {
        public int Id { get; set; }
        public int EventId {  get; set; }

        public int AggregateId {  get; set; }
        public string EventType { get; set; } = string.Empty; // e.g., "PaymentCompleted"
        public DateTime RecivedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAtUtc {  get; set; }
    }
}
