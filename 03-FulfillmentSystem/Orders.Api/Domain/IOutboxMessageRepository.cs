using Orders.Api.Infrastructure;

namespace Orders.Api.Domain
{
    public interface IOutboxMessageRepository
    {
        Task<IEnumerable<OutboxMessage>> GetOutboxMessagesAsync();

        Task<bool> SaveProcessStatusAsync(int eventId);
    }
}
