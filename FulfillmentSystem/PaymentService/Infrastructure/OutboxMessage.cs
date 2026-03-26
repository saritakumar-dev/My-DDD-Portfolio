
namespace PaymentService.Infrastructure
{
    public class OutboxMessage
    {
        public int Id { get; set; } 
        public string Type { get; set; } = string.Empty; // e.g., "PaymentCompleted"
        public string AggregateType { get; set; } = string.Empty; // e.g., "Payment"
        public int AggregateId { get; set; }  // "Payment Id"
        public string Content { get; set; } = string.Empty; // Serialized JSON
        public DateTime OccurredOnUtc { get; set; } = DateTime.UtcNow; 
        public DateTime? ProcessedAtUtc { get; set; } // Null until published to broker
        public string? Error { get; set; } // Log failures here
    }
}
