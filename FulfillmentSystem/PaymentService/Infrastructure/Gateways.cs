

using PaymentService.Domain;

namespace PaymentService.Infrastructure
{
    public class StripeGateway : IPaymentGateway
    {
        public async Task<Guid> ChargeAsync(decimal amount)
        {
            await Task.Delay(100);
            var transactionId = Guid.NewGuid();
            Console.WriteLine($"Paid ${amount} via Stripe (International) with transaction id ${transactionId} ");
            return transactionId;
        }
    }

    public class RazorpayGateway : IPaymentGateway
    {
        public async Task<Guid> ChargeAsync(decimal amount)
        {
            await Task.Delay(100);
            var transactionId = Guid.NewGuid();
            Console.WriteLine($"Paid ${amount} via Razorpay with transaction id ${transactionId}.");
            return transactionId;
        }
    }

    public class PaypalGateway : IPaymentGateway
    {
        public async Task<Guid> ChargeAsync(decimal amount)
        {
            await Task.Delay(100);
            var transactionId = Guid.NewGuid();
            Console.WriteLine($"Paid ${amount} via Paypal (International) with transaction id ${transactionId}");
            return transactionId;
        }
    }
}
