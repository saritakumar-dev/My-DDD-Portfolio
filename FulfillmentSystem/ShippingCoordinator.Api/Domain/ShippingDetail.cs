
namespace ShippingCoordinator.Api.Domains
{
    public class ShippingDetail
    {
        public int Id {  get; set; }

        public int OrderId {  get; set; }

        public int PaymentId {  get; set; }

        public string Carrier { get; set; } = "Fedex";

        public string ShippingAddress { get; set; } = string.Empty;

        public string Status = "Preparing For Dispatch";

        public Guid TrackingNumber { get; set; } = Guid.Empty;
                
    }
}
