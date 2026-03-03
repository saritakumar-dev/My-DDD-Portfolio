using EventSourcingDomainModelApp.Domain;
using EventSourcingDomainModelApp.Infrastructure;

namespace EVentSourceDomainAppTestProject
{
    public class InMemoryEventStoreTests
    {
        private readonly string owner = "John Doe";

        [Fact]
        public async Task LoadEvents_ShouldReturnEmpty_WhenAccountDoesNotExist()
        {
            var store = new InMemoryEventStore();
            var aggregateId = Guid.NewGuid();

            var domainEvents = await store.LoadEventsAsync(aggregateId);
            Assert.Empty(domainEvents);
        }

        [Fact]
        public async Task SaveEvents_ShouldStoreEvents_WhenStoreIsEmpty()
        {
            var store = new InMemoryEventStore();
            var aggregateId = Guid.NewGuid();
            var events = new List<DomainEvent> { new AccountOpened(aggregateId, owner, 100, DateTime.UtcNow) };

            await store.SaveEventsAsync(aggregateId, events, 0);

            var domainEvents = await store.LoadEventsAsync(aggregateId);
            Assert.Single(domainEvents);
            Assert.IsType<AccountOpened>(domainEvents.First());
        }

        [Fact]
        public async Task SaveEvents_ShouldAppendToExistingStream()
        {
            var store = new InMemoryEventStore();
            var aggregateId = Guid.NewGuid();
            var events = new List<DomainEvent> { new AccountOpened(aggregateId, owner, 100, DateTime.UtcNow) };

            await store.SaveEventsAsync(aggregateId, events, 0);

            await store.SaveEventsAsync(aggregateId, new[] { new MoneyDeposited(aggregateId, 200, DateTime.UtcNow) }, 1);

            var history = await store.LoadEventsAsync(aggregateId);
            Assert.Equal(2, history.Count());

        }

        [Fact]
        public async Task SaveEvents_ShouldThrowException_WhenVersionMismatch()
        {
            var store = new InMemoryEventStore();
            var aggregateId = Guid.NewGuid();
            await store.SaveEventsAsync(aggregateId, new[] { new AccountOpened(aggregateId, owner, 100, DateTime.UtcNow) }, 0);

            var newEvents = new List<DomainEvent> { new MoneyDeposited(aggregateId, 200, DateTime.UtcNow) };
            var exception = await Assert.ThrowsAsync<Exception>(async () =>
           await store.SaveEventsAsync(aggregateId, newEvents, 0));

            Assert.Contains("CONCURRENCY CONFLICT", exception.Message);
        }
    }
}
