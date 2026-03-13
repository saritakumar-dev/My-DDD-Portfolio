using HelpDesk.Domain.ValueObjects;

namespace HelpDesk.Application.Tickets.Commands
{
    public record CloseTicketCommand
    {
        public Guid TicketId { get; init; }
        public Guid UserId { get; init; }
        public int UserRole { get; init; }

        public CloseTicketCommand(Guid ticketId, Guid userId, int userRole)
        {
            TicketId = ticketId;
            UserId = userId;
            UserRole = userRole;
        }
    }
}