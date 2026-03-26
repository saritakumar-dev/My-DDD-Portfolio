using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;
using PaymentService.Dtos;
using PaymentService.Infrastructure;
using System.Text.Json;

namespace PaymentService.Application
{
    public class PaymentOrchestrator
    {
        private readonly PaymentDbContext _dbContext;

        public PaymentOrchestrator(PaymentDbContext dbContext) { _dbContext = dbContext; }
        public async Task ProcessPaymentAsync(PaymentRequestDto request)
        {

            var orderId = request.OrderId;
            var amount = request.Amount;
            var currency = request.Currency;

            var existingPayment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.OrderId == request.OrderId);
            if (existingPayment != null && existingPayment.Status == PaymentStatus.Completed) return;

            var gateway = GatewayFactory.GetGateway(request.Currency);
            var transactionId = await gateway.ChargeAsync(amount);
            var payment = new Payment
            {
                OrderId = orderId,
                Amount = amount,
                Status = transactionId == Guid.Empty ? PaymentStatus.Failed : PaymentStatus.Completed,
                TransactionId = transactionId
            };

            _dbContext.Payments.Add(payment);
            await _dbContext.SaveChangesAsync(); // to get the paymentId
            _dbContext.OutboxMessages.Add(new OutboxMessage
            {
                AggregateId = payment.Id,
                AggregateType = "Payment",
                Type = "PaymentCompleted",
                Content = JsonSerializer.Serialize(new
                {
                    PaymentId = payment.Id,
                    OrderId = orderId,
                    Amount = amount,
                    Currency = currency,
                    TransactionId = transactionId,
                    Gateway = gateway.GetType().Name
                }),
                OccurredOnUtc = DateTime.UtcNow,
                Error = payment.Status == PaymentStatus.Failed ? "Payment Failed" : string.Empty
            });
            
            // We do NOT call SaveChangesAsync here if we want the Worker to control the transaction.
            // Or, we call it, but the Worker calls Commit().
            await _dbContext.SaveChangesAsync();
            //await transaction.CommitAsync(cancellationToken);
        }
    }
}
