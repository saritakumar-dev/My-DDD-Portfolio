using BankLedger.Core.Common.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankLedger.WriteProject.Application.Common
{
    public interface IEventStore
    {
        Task AppendEventsAsync(Guid aggregateId, int expectedVersion, IEnumerable<BankEvent> events, CancellationToken cancellationToken);

        Task<List<BankEvent>> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken=default);
    }
}
