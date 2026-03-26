namespace Orders.Api.Infrastructure
{
    public class OutboxMessage
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // e.g., "OrderPlaced"

        public string AggregateType { get; set; } = string.Empty;

        public int AggregateId { get; set; }

        public string Content { get; set; } = string.Empty;  // Json Payload

        public DateTime OccurredOnUtc { get; set; }

        public DateTime? ProcessedOnUtc { get; set; } // null untill processed
    }
}
