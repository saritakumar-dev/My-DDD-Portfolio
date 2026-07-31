using BankLedger.Domain.Common;

namespace BankLedger.WriteProject.Application
{
    public record DepositMoneyCommand(Guid AccountId, decimal Amount, string Currency, string Reference);
    public record WithdrawMoneyCommand(Guid AccountId, decimal Amount, string Currency, string Reference);
    public record OpenAccountCommand(Guid AccountId, string CustomerName, string Currency);
    public record ForgetUserCommand(Guid AccountId, ClosureReason ClosureReason);

}
