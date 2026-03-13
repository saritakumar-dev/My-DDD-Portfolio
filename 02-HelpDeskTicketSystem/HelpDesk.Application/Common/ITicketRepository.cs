using HelpDesk.Domain;
using HelpDesk.Domain.ValueObjects;

namespace HelpDesk.Application.Common
{
  public  interface ITicketRepository
    {
        Task<Ticket?> GetTicketAsync(TicketId ticketId);

        Task AddTicket(Ticket ticket);

        Task SaveChangesAsync();
    }
}
