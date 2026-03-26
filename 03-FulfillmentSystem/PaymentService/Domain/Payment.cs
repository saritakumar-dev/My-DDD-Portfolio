
namespace PaymentService.Domain
{
    public class Payment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }

        public decimal Amount {  get; set; }

        public PaymentStatus Status =  PaymentStatus.Pending;

        public string Currency { get; set; } = "USD";

        public Guid TransactionId {  get; set; }= Guid.Empty;

    }
}
