
using PaymentService.Domain;
using PaymentService.Infrastructure;

namespace PaymentService.Application
{
    public static class GatewayFactory
    {
        public static IPaymentGateway GetGateway(string currency)
        {
            return currency.ToUpper() switch
            {
                "USD" => new PaypalGateway(),
                "POUND" => new StripeGateway(),
                "INR" => new RazorpayGateway(),
                _ => throw new ArgumentException("Currency not supported")
            };
        }
    }
}
