using BankLedger.WriteProject.Application.Common;
using BankLedger.WriteProject.Application.Sagas;
using Microsoft.EntityFrameworkCore;


namespace BankLedger.WriteProject.Infrastructure.Database
{
    public class SagaStateRepository : ISagaStateRepository
    {
        private readonly BankWriteDbContext _dbContext;

        public SagaStateRepository(BankWriteDbContext dbContext) { _dbContext = dbContext; }
        public async Task<MoneyTransferSagaState?> GetStateBySagaIdAsync(Guid sagaId, CancellationToken cancellationToken)
        {
            return await _dbContext.Sagas.FirstOrDefaultAsync(saga => saga.SagaId == sagaId, cancellationToken);
        }

        public async Task SaveAsync(MoneyTransferSagaState state, CancellationToken cancellationToken)
        {
            var exists = await _dbContext.Sagas.AnyAsync(saga => saga.SagaId == state.SagaId, cancellationToken);

            if (!exists)
                await _dbContext.Sagas.AddAsync(state, cancellationToken);
            else
                _dbContext.Sagas.Update(state);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
