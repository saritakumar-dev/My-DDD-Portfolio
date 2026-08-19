

namespace BankLedger.WriteProject.Application.Common.Exceptions
{
    public class InsufficientFundsException : ApplicationDomainException
    {
        public Guid AccountId { get; init; }
        public decimal AvailableBalance { get; init; }

        public decimal AttemptedDebit { get; init; }
        public InsufficientFundsException(Guid accountId, decimal availableBalance, decimal attemptedDebit)
        : base(
            title: "Insufficient Funds",
            detail: $"Account '{accountId}' has an available balance of {availableBalance:C}, which is insufficient for the attempted total batch debit of {Math.Abs(attemptedDebit):C}.",
            statusCode: 422) // 422 Unprocessable Entity is excellent for business rule failures
        {
            AccountId = accountId;
            AvailableBalance = availableBalance;
            AttemptedDebit = attemptedDebit;
        }
    }
}
