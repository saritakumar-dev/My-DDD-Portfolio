
namespace BankLedger.WriteProject.Application.Common.Exceptions
{
    public class AccountDeactivatedException : ApplicationDomainException
    {
        public Guid AccountId { get; init; }

        public AccountDeactivatedException(Guid accountId)
            : base(
                title: "Account Restricted",
                detail: $"The transaction was rejected because account '{accountId}' is closed or anonymized.",
                statusCode: 400) // 400 Bad Request
        {
            AccountId = accountId;
        }
    }
}

