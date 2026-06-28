
using BankLedger.WriteProject.Application.Sagas;

namespace BankLedger.WriteProject.Application.Common
{
    public interface ISagaStateRepository
    {
        Task SaveAsync(MoneyTransferSagaState moneyTransferSagaState, CancellationToken cancellationToken);

        Task<MoneyTransferSagaState?> GetStateBySagaIdAsync(Guid sagaId, CancellationToken cancellationToken);
    }
}
