using EventSourcingDomainModelApp.Domain;
using EventSourcingDomainModelApp.Infrastructure;
using static EventSourcingDomainModelApp.Domain.CreditAccount;

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


            await _eventStore.SaveEventsAsync(id, account.GetUncommittedChanges(), 0);

            account.MarkChangesAsCommitted();
        }   

        public async Task HandleAmountDeposit(Guid id, decimal amount)
        {
            var events = await (_eventStore.LoadEventsAsync(id));       

            var account = new CreditAccount();
            account.LoadFromHistory(events);
            var originalVersion = account.Version;

            account.Deposit(amount);
            var changes = account.GetUncommittedChanges().ToList();
            await _eventStore.SaveEventsAsync(id, changes, originalVersion);

            //account.MarkChangesAsCommitted();
        }

        public async Task HandleWithdrawal(Guid id, decimal amount)
        {
            var events = await _eventStore.LoadEventsAsync(id);

            var account = new CreditAccount();
            account.LoadFromHistory(events);
            var originalVersion = account.Version;
            account.Withdraw(amount);
            var changes = account.GetUncommittedChanges().ToList();
            await _eventStore.SaveEventsAsync(id, changes, originalVersion);

            //account.MarkChangesAsCommitted();
        }

        public async Task<decimal> HandleDisplayBalance(Guid id)
        {
            var events = await _eventStore.LoadEventsAsync(id);

            if (!events.Any())
                throw new Exception("Account not found.");

            var account = new CreditAccount();
            account.LoadFromHistory(events);

            return account.Balance;
        }

        public async Task<Status> HandleCloseAccount(Guid id)
        {
            var events = await _eventStore.LoadEventsAsync(id);

            if (!events.Any()) throw new Exception("Account Not Found");

            var account = new CreditAccount();
            account.LoadFromHistory(events);
            var originalVersion = account.Version;
            account.CloseAccount(id);

            var changes = account.GetUncommittedChanges().ToList();
            await _eventStore.SaveEventsAsync(id, changes, originalVersion);

            return account.BankStatus;
        }
    }
}
