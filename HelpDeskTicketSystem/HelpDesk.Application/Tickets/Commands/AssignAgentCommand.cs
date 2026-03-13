using HelpDesk.Domain;
using HelpDesk.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpDesk.Application.Tickets.Commands
{
    public record AssignAgentCommand
    {
        public Guid TicketId { get; init; }
        public Guid SupportAgentId {  get; init; }


        private AssignAgentCommand() { }

        public AssignAgentCommand(Guid ticketId, Guid supportAgentId) {
            if (ticketId == Guid.Empty || supportAgentId == Guid.Empty)
                throw new ArgumentException("TicketId or SupportAgentId cannot be empty");
            TicketId = ticketId;
            SupportAgentId = supportAgentId;
        }
    }
}
