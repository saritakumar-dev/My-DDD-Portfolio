using HelpDesk.Application.Common;
using HelpDesk.Domain;
using HelpDesk.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Persistence.Repositories
{
    public class TicketRepository : ITicketRepository
    {

        private readonly HelpDeskDbContext _context;

        public TicketRepository(HelpDeskDbContext context)=> _context = context;

        public async Task<Ticket?> GetTicketAsync(TicketId ticketId)
        {
            return await _context.Tickets
                                .Include(t=>t.Messages)
                                .FirstOrDefaultAsync(t=>t.TicketId == ticketId);
        }

        public async Task AddTicket(Ticket ticket)
        {
            await _context.AddAsync(ticket);
        }

        public async Task SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception )
            {
                throw ;
            }
        }
    }
}
