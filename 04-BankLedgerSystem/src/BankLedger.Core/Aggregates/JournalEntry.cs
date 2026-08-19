

using BankLedger.Core.Common.Events;
using BankLedger.Domain.Exceptions;

namespace BankLedger.Domain.Aggregates
{
    public class JournalEntry
    {
        private List<LedgerEntry> _ledgerEntries = new();

        private readonly List<BankEvent> _uncommittedEvents = new();
        public Guid JournalEntryId { get; private set; }
        public IReadOnlyCollection<LedgerEntry> Entries => _ledgerEntries.AsReadOnly();
        public IReadOnlyCollection<BankEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();
        public bool IsPosted { get; private set; }

        public JournalEntry(Guid journalEntryId)
        {
            JournalEntryId = journalEntryId;
            this.IsPosted = false;
        }
        public void Post(List<LedgerEntry> ledgerEntries)
        {
            if (this.IsPosted)
                throw new InvalidOperationException("This journal entry is already posted.");

            if (ledgerEntries == null || ledgerEntries.Count < 2)
            {
                throw new ArgumentException("A valid double-entry transaction requires at least two ledger lines.");
            }

            var netBalance = ledgerEntries.Select(e => e.Amount).Sum();
            if (netBalance != 0) { throw new UnbalancedLedgerException(netBalance); }

            IsPosted = true;
            _ledgerEntries = ledgerEntries;
            RaiseEvent(new JournalEntryPostedEvent(JournalEntryId, Entries));
        }

        public void ClearUncommittedEvents()
        {
            _uncommittedEvents.Clear();
        }

        private void RaiseEvent(BankEvent @event)
        {
            _uncommittedEvents.Add(@event);
          //  ApplyEvent(@event);
          //  Version = @event.Version;
        }
    }

    public record LedgerEntry(Guid AccountId, decimal Amount, string Description);

}
