using EventSourcingDomainModelApp.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcingDomainModelApp.Infrastructure
{
    public interface IEventStore
    {
        public Task SaveEventsAsync(Guid aggregateId, IEnumerable<DomainEvent> events, int expectedVersion);

        public Task<IEnumerable<DomainEvent>> LoadEventsAsync(Guid aggregateId);
    }
}
