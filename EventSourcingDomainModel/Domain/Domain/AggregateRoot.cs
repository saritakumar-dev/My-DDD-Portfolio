using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcingDomainModelApp.Domain
{
    public abstract class AggregateRoot
    {
        public Guid Id { get; protected set; }
        public int Version { get; protected set; } = -1;

        private readonly List<DomainEvent> _changes = new();

        public IEnumerable<DomainEvent> GetUncommittedChanges() => _changes;

        public void MarkChangesAsCommitted() => _changes.Clear();

        // Replays history to rebuild state
        public void LoadFromHistory(IEnumerable<DomainEvent> history)
        {
            foreach(var @event in history)
            {
                Apply(@event, isNew:false);
            }
        }

        protected void RaiseEvent(DomainEvent @event) => Apply(@event, isNew: true);
        
        private void Apply(DomainEvent @event, bool isNew)
        {
            this.When(@event);
            if(isNew) _changes.Add(@event);
            this.Version++;
        }

        protected abstract void When(DomainEvent @event);
        
    }
}
