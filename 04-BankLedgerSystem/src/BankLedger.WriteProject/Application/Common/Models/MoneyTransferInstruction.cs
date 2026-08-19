

namespace BankLedger.WriteProject.Application.Common.Models
{
    public record MoneyTransferInstruction(Guid AccountId, decimal Amount, string Description);
}
