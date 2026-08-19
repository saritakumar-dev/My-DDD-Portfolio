
namespace BankLedger.Domain.ValueObjects
{
    public  record Money
    {
        public decimal Amount { get; init; }
        public string Currency { get; init; } = string.Empty;
        public Money(decimal amount, string currency)
        {
            if (amount < 0)
                throw new ArgumentException("Financial balance amounts cannot be negative inside this domain boundary.");

            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency structural type codes must be explicitly defined.");

            Amount = amount;
            Currency = currency.ToUpper().Trim();
        }

        public static Money Zero(string currency = "EUR") => new(0.00m, currency);
        public Money Plus(Money other)
        {
            EnsureSameCurrency(other);
            return new Money(this.Amount + other.Amount, this.Currency);
        }

        public Money Minus(Money other)
        {
            EnsureSameCurrency(other);
            return new Money(this.Amount - other.Amount, this.Currency);
        }

        private void EnsureSameCurrency(Money other)
        {
            if (this.Currency != other.Currency)
            {
                throw new InvalidOperationException($"Currency Mismatch Mappings: Cannot execute mathematical operations on multi-currency variables ({this.Currency} vs {other.Currency}).");
            }
        }
    }
}
