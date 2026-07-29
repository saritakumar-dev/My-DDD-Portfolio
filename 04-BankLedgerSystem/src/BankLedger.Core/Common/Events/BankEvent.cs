
namespace BankLedger.Core.Common.Events
{
    public abstract record BankEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public Guid AggregateId { get; init; }
        public int Version { get; init; }
        public DateTime OccuredAt { get; init; } = DateTime.UtcNow;

        protected BankEvent() { }
        protected BankEvent(Guid aggregateId, int version)
        {
            AggregateId = aggregateId;
            Version = version;
        }
    }

    public record AccountOpenedEvent : BankEvent
    {
        [GdprEncrypted]
        public string CustomerName { get; init; } = string.Empty;
        public string Currency { get; init; } = string.Empty;
        public AccountOpenedEvent() { }

        public AccountOpenedEvent(Guid accountId, string customerName, string currency, int version)
            : base(accountId, version)
        {
            CustomerName = customerName;
            Currency = currency;
        }

    }

    public record MoneyWithdrawnEvent : BankEvent
    {
        public decimal Amount { get; init; }

        public string Currency { get; init; }

        public string Reference { get; init; } = string.Empty;

        public MoneyWithdrawnEvent() { }

        public MoneyWithdrawnEvent(Guid accountId, decimal amount, string currency, string reference, int version) : base(accountId, version)
        {
            Amount = amount;
            Currency = currency;
            Reference = reference;
        }
    }

    public record MoneyDepositedEvent : BankEvent
    {
        public decimal Amount { get; init; }

        public string Currency { get; init; }
        public string Reference { get; init; } = string.Empty;

        public MoneyDepositedEvent() { }

        public MoneyDepositedEvent(Guid accountId, decimal amount, string currency, string reference, int version)
            : base(accountId, version)
        {
            Amount = amount;
            Currency = currency;
            Reference = reference;
        }
    }

    public record DepositMoneyFailedEvent : BankEvent
    {
        public decimal Amount { get; init; }

        public string Currency { get; init; }
        public string Reference { get; init; } = string.Empty;
        public string Reason { get; init; } = string.Empty;

        public DepositMoneyFailedEvent(Guid accountId, decimal amount, string reference, string reason, int version) : base(accountId, version)
        {
            Amount = amount;
            Reference = reference;
            Reason = reason;
        }
    }

    public record UserForgottenEvent : BankEvent
    {
        
        public DateTime ErasedAt {  get; init; }

        public UserForgottenEvent() { }

        public UserForgottenEvent(Guid accountId, int version): base(accountId, version)
        {
            ErasedAt = DateTime.UtcNow;
        }
    }
}
