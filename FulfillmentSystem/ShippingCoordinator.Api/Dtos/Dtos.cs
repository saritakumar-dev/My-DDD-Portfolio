

namespace ShippingCoordinator.Api.Dtos
{
    public record ShippingRequestDto
    {
        public int OrderId {  get; set; }
        public int PaymentId {  get; set; }
        public required string ShippingAddress {  get; set; }

        public int EventId { get; set; }

        public required string EventType {  get; set; }
    }

    public record ShippingResponseDto
    {
        public Guid ShippingTrackingNumber {  get; set; }

        public required string ShippingStatus { get; set; }

        public bool IsSuccessStatusCode { get; set; }

    }
}
