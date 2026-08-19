using BankLedger.Core.Common.Events;

namespace BankLedger.WriteProject.Application.Common
{
    public interface IEventStore
    {
        Task AppendEventsAsync(Guid aggregateId, string streamCategory, int expectedVersion, IEnumerable<BankEvent> events, CancellationToken cancellationToken);

        Task<List<BankEvent>> GetEventsAsync(Guid aggregateId, int version, CancellationToken cancellationToken=default);
    }
}
