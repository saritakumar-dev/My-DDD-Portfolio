using HelpDesk.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpDesk.Application.Tickets.Commands
{
    public record ReassignTicketCommand
    {
        public Guid TicketId { get; init; }
        public Guid NewAgentId { get; init; }

        private ReassignTicketCommand() { }

        public ReassignTicketCommand(Guid ticketId, Guid agentId)
        {
            TicketId=ticketId;
            NewAgentId = agentId;
        }
    }

}
