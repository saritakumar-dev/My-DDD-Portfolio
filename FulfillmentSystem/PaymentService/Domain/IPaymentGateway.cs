
namespace PaymentService.Domain
{
    public interface IPaymentGateway
    {
        Task<Guid> ChargeAsync(decimal amount);
    }
}
