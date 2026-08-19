
namespace BankLedger.Domain.Common
{
    public record AccountStateResult(Guid AccountId, bool IsClosed, decimal CurrentBalance);

    public interface IAccountReadModelService
    {
        Task<AccountStateResult> GetAccountStateResultAsync(Guid accountId, CancellationToken cancellationToken);
    }
}
