using HelpDesk.Domain.DomainEvents;

namespace HelpDesk.Domain.Common
{
    public class AggregateRoot
    {
        public Guid Id { get; set; }

        private List<IDomainEvent> _domainEvents = new();

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

        public void ClearDomainEvent() => _domainEvents.Clear();
    }
}
