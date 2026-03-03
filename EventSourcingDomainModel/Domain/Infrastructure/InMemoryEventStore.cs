using EventSourcingDomainModelApp.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcingDomainModelApp.Infrastructure
{
    public class InMemoryEventStore : IEventStore
    {
        private Dictionary<Guid, List<DomainEvent>> _store = new();
        public Task<IEnumerable<DomainEvent>> LoadEventsAsync(Guid aggregateId)
        {
            if(!_store.ContainsKey(aggregateId))
                return Task.FromResult(Enumerable.Empty<DomainEvent>());

          return Task.FromResult( _store[aggregateId].AsEnumerable());
        }

        public Task SaveEventsAsync(Guid aggregateId, IEnumerable<DomainEvent> events, int expectedVersion)
        {
            if(!_store.ContainsKey(@aggregateId))
                _store[aggregateId]= new List<DomainEvent>();

            var currentVersion = _store[aggregateId].Count;
            if(currentVersion != expectedVersion)
                throw new Exception("CONCURRENCY CONFLICT: The account state has changed. Please retry.");

            _store[aggregateId].AddRange(events);
            return Task.CompletedTask;
        }
    }
}
