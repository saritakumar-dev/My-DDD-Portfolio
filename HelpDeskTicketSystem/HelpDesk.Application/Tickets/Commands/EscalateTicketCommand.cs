using HelpDesk.Domain.ValueObjects;

namespace HelpDesk.Application.Tickets.Commands
{
    public record EscalateTicketCommand
    {
        public Guid TicketId { get; init; }
        public Guid ManagerId { get; init; }

        private EscalateTicketCommand() { }
        public EscalateTicketCommand(Guid ticketId, Guid managerId)
        {
            if (ticketId == Guid.Empty || managerId == Guid.Empty)
                throw new ArgumentException("TicketId or SupportAgentId cannot be empty");
            TicketId = ticketId;
            ManagerId = managerId;
        }
    }
}