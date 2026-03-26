

namespace ShippingCoordinatorService.Dtos
{
   

    public record ShippingResultDto
    {
        public Guid ShippingTrackingNumber {  get; set; }

        public string Status {  get; set; } = string.Empty;
    }
}
