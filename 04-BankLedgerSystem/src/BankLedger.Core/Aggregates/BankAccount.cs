using BankLedger.Core.Common.Events;
using BankLedger.Domain.Common;
using BankLedger.Domain.ValueObjects;
using BankLedger.WriteProject.Domain.Aggregates;

namespace BankLedger.Domain.Aggregates
{
    public class BankAccount
    {
        public Guid Id { get; private set; }

        public int Version { get; private set; }

        public string CustomerName { get; private set; } = string.Empty;

        //In DDD, Money is a classic Value Object. Value Objects should never be null;
        //they should represent an absolute value (even if that value is zero dollars or zero euros)
        // Initializing to Money.Zero() satisfies the constructor safety check instantly!
        public Money Balance { get; private set; } = Money.Zero();

        public bool IsClosed { get; private set; }

        private readonly List<BankEvent> _uncommittedEvents = new();

        public IReadOnlyCollection<BankEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

        public BankAccount(Guid accountId, string customerName, string currency)
        {
            if (string.IsNullOrWhiteSpace(customerName)) throw new ArgumentNullException("The customer cannot be empty");

            RaiseEvent(new AccountOpenedEvent(accountId, customerName, currency, this.Version + 1));
        }

        private BankAccount()
        {
        }

        public static BankAccount LoadFromHistory(IEnumerable<BankEvent> history)
        {
            var account = new BankAccount();

            foreach (var @event in history)
            {
                account.ApplyEvent(@event);
                account.Version = @event.Version;
            }
            return account;
        }

        public static BankAccount FromSnapshot(BankAccountSnapshot snapshot)
        {
            var account = new BankAccount()
            {
                Id = snapshot.AggregateId,
                Version = snapshot.Version,
                IsClosed = snapshot.IsClosed,
                Balance = new Money(snapshot.Balance.Amount, snapshot.Balance.Currency),
            };

            return account;
        }

        public BankAccountSnapshot CreateSnapshot()
        {
            return new BankAccountSnapshot()
            {
                AggregateId = this.Id,
                Version = this.Version,
                IsClosed = this.IsClosed,
                Balance = new Money(this.Balance.Amount, this.Balance.Currency),
                SnapshottedAt = DateTime.UtcNow
            };
        }


        // --- BUSINESS METHODS (Command Processing) ---
        public void Withdraw(decimal amount, string currency, string reference)
        {
            if (this.IsClosed)
                throw new InvalidOperationException("Cannot withdraw funds from a closed or anonymized account.");

            if (amount <= 0)
                throw new ArgumentException("Withdrawal amount must be greater than zero.");

            // Business Rule / Invariant Protection
            if (Balance.Amount - amount < 0)
                throw new InvalidOperationException("Insufficient funds for this withdrawal.");

            RaiseEvent(new MoneyWithdrawnEvent(Id, amount, currency, reference, this.Version + 1));

        }

        public void Deposit(decimal amount, string currency, string reference)
        {
            if (this.IsClosed)
                throw new InvalidOperationException("Cannot deposit funds into a closed or anonymized account.");

            if (amount <= 0)
                throw new ArgumentException("Deposit amount must be greater than zero.");

            RaiseEvent(new MoneyDepositedEvent(Id, amount, currency, reference, this.Version + 1));
        }

        public void CloseAndAnonymizeAccount(ClosureReason reason)
        {
            // 1. Enforce common state invariant
            if (this.IsClosed)
                throw new InvalidOperationException("The account is already closed.");

            // 2. Enforce conditional business rules based on the reason if needed
            if (reason == ClosureReason.CustomerContractTerminated && this.Balance.Amount != 0)
            {
                throw new InvalidOperationException("Cannot terminate a contract for an account with a remaining balance.");
            }

            // 3. Emit the single event with the dynamic reason
            RaiseEvent(new UserForgottenEvent(Id, this.Version + 1, reason));
        }


        public void ClearUncommittedEvents()
        {
            _uncommittedEvents.Clear();
        }

        // --- EVENT APPLICATION (State Mutation) ---
        private void RaiseEvent(BankEvent @event)
        {
            _uncommittedEvents.Add(@event);
            ApplyEvent(@event);
            Version = @event.Version;
        }

        private void ApplyEvent(BankEvent @event)
        {
            switch (@event)
            {
                case AccountOpenedEvent e:
                    Id = e.AggregateId;
                    CustomerName = e.CustomerName;
                    Balance = new Money(0, e.Currency);
                    break;
                case MoneyDepositedEvent e:
                    Balance = Balance.Plus(new Money(e.Amount, e.Currency));
                    break;
                case MoneyWithdrawnEvent e:
                    Balance = Balance.Minus(new Money(e.Amount, e.Currency));
                    break;
                case UserForgottenEvent e:
                    IsClosed = true;
                    break;
            }
        }
    }
}
