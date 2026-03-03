using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcingDomainModelApp.Domain
{
    public class CreditAccount : AggregateRoot
    {

        public decimal Balance { get; private set; }
        public decimal OverdraftLimit { get; private set; }
        public string Owner { get; private set; }

        public static CreditAccount Open(Guid id, string owner, decimal limit)
        {
            var account = new CreditAccount();
            account.RaiseEvent(new AccountOpened(id, owner, limit, DateTime.UtcNow));
            return account;
        }

        public void Withdraw (decimal amount)
        {
            if (Balance - amount < OverdraftLimit)
                throw new Exception("Overdraft Limit Exceeded");
            this.RaiseEvent(new WithdrawalPerformed(Id, amount, DateTime.UtcNow));
        }

        public void Deposit(decimal amount)
        {
            this.RaiseEvent(new MoneyDeposited(Id, amount, DateTime.UtcNow));
        }
        protected override void When(DomainEvent @event)
        {
            switch (@event)
            {

                case AccountOpened e:
                    Id = e.Id;
                    Owner = e.Owner;
                    OverdraftLimit = e.Limit;
                    Balance = 0;
                    break;

                case MoneyDeposited e:
                    Balance += e.Amount;
                    break;

                case WithdrawalPerformed e:
                    Balance-= e.Amount;
                    break;
            }
        }
    }
}
