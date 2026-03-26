
using PaymentService.Domain;

namespace PaymentService.ACL
{
    //The Data Structure coming from Order Service (External Contract) Match the property names to the JSON fields in Order Service Outbox
    public record OrderPlacedEvent(
        int OrderId,
        decimal TotalAmount,
        string Currency,
        string ShippingAddress
    );

    public interface IPaymentProviderAcl
    {
        Payment MapToDomain(OrderPlacedEvent externalEvent);
    }

    public class PaymentProviderAcl  : IPaymentProviderAcl
    {
        public Payment MapToDomain(OrderPlacedEvent externalEvent)
        {
            // 1. Logic for "Stateless Translation" (e.g., Currency Mapping)
            var internalCurrency = MapCurrency(externalEvent.Currency);

            // 2. Mapping External Data to Internal Payment Aggregate
            return new Payment {
                OrderId= externalEvent.OrderId,
                Amount= externalEvent.TotalAmount,
                Currency= internalCurrency,
            };
        }

        private string MapCurrency(string externalCurrency)
        {
            // Example logic: Payment Gateway only accepts uppercase ISO codes
            return externalCurrency?.ToUpper() switch
            {
                "RS" => "INR",
                "DOLLAR" => "USD",
                _ => externalCurrency ?? "USD"
            };
        }
    }
}
