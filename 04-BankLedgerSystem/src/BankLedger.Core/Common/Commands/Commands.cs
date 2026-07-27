namespace BankLedger.Core.Common.Commands
{
    public record DepositMoneyCommand(Guid AccountId, decimal Amount, string Reference);
    public record WithdrawMoneyCommand(Guid AccountId, decimal Amount, string Reference);
    public record OpenAccountCommand(Guid AccountId, string CustomerName, string Currency);
    public record ForgetUserCommand(Guid AccountId);

}
