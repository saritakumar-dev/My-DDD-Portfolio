

using PaymentService.Domain;

namespace PaymentService.Dtos
{
    public record OutboxDto(
    int Id,
    int OrderId,
    string Type,
    string Content, // This is the JSON string containing OrderId, Amount, etc.
    DateTime OccurredOn
);

    public record PaymentRequestDto
    (
        int OrderId,
        int PaymentId,
        decimal Amount,
        string Currency
    );

    public record PaymentResultDto
    (
       Guid TransactionId,
       PaymentStatus PaymentStatus
    );

    public record ShippingRequestDto
    {
        public int OrderId { get; set; }
        public int PaymentId { get; set; }
        public required string ShippingAddress { get; set; }
        public int EventId { get; set; }
        public required string EventType {  get; set; }

    }

    public record ShippingResponseDto
    {
        public Guid ShippingTrackingNumber { get; set; }
        public required string ShippingStatus { get; set; }
       
    }
}
    