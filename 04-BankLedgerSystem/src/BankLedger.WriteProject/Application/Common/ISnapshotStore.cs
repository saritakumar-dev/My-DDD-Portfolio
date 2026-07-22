using BankLedger.WriteProject.Domain.Aggregates;

namespace BankLedger.WriteProject.Application.Common
{
    public interface ISnapshotStore
    {
        Task<BankAccountSnapshot> GetLatestAsync(Guid aggregateId);

        Task SaveAsync(BankAccountSnapshot snapshot);
    }
}
