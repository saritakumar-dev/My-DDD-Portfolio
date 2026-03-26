using Microsoft.EntityFrameworkCore;
using Orders.Api.Domain;

namespace Orders.Api.Infrastructure
{
    public class OutboxMessageRepository : IOutboxMessageRepository
    {
        private readonly OrderDbContext _context;

        public OutboxMessageRepository(OrderDbContext context) { _context = context; }
        public async Task<IEnumerable<OutboxMessage>> GetOutboxMessagesAsync()
        {
            return await _context.OutboxMessages.Where(om => om.ProcessedOnUtc == null)
                  .OrderBy(om => om.OccurredOnUtc).ToListAsync();
        }

        public async Task<bool> SaveProcessStatusAsync(int eventId)
        {
            var message = await _context.OutboxMessages.FindAsync(eventId);
            if (message == null) return false;

            message.ProcessedOnUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
