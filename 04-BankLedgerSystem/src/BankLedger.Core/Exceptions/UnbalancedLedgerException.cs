
namespace BankLedger.Domain.Exceptions
{
    public class UnbalancedLedgerException : Exception
    {
        public decimal NetImbalance { get; init; }

        public UnbalancedLedgerException(decimal netImbalance)
            : base($"Core Ledger Invariant Breach: The transaction payload is mathematically unbalanced by {netImbalance}.")
        {
            NetImbalance = netImbalance;
        }
    }
}
