using EventSourcingDomainModelApp.Domain;
using EventSourcingDomainModelApp.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcingDomainModelApp.Application
{
    public class AccountService
    {
        private IEventStore _eventStore;

        public AccountService(IEventStore eventStore) => _eventStore = eventStore;


        public async Task HandleAccountOpen(Guid id, string owner, decimal limit)
        {
            var events = await _eventStore.LoadEventsAsync(id);
            var account = CreditAccount.Open(id, owner, limit);


            await _eventStore.SaveEventsAsync(id, account.GetUncommittedChanges(), -1);

            account.MarkChangesAsCommitted();
        }   

        public async Task HandleAmountDeposit(Guid id, decimal amount)
        {
            var events = await (_eventStore.LoadEventsAsync(id));       

            var account = new CreditAccount();
            account.LoadFromHistory(events);

            account.Deposit(amount);
            await _eventStore.SaveEventsAsync(id, account.GetUncommittedChanges(), account.Version-1);

            account.MarkChangesAsCommitted();
        }

        public async Task HandleWithdrawal(Guid id, decimal amount)
        {
            var events = await _eventStore.LoadEventsAsync(id);

            var account = new CreditAccount();
            account.LoadFromHistory(events);

            account.Withdraw(amount);

            await _eventStore.SaveEventsAsync(id, account.GetUncommittedChanges(), account.Version-1);
        }

        public async Task<decimal> DisplayBalance(Guid id)
        {
            var events = await _eventStore.LoadEventsAsync(id);

            if (!events.Any())
                throw new Exception("Account not found.");

            var account = new CreditAccount();
            account.LoadFromHistory(events);

            return account.Balance;
        }
    }
}
