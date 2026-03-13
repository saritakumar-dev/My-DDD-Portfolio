using HelpDesk.Application.Common;
using HelpDesk.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpDesk.Application.Tickets.Commands
{
    public class AssignAgentHandler
    {
        private ITicketRepository _repository;
        public AssignAgentHandler(ITicketRepository repository) => _repository = repository;

        public async Task Handle(AssignAgentCommand command)
        {
            var ticket = await _repository.GetTicketAsync(new TicketId(command.TicketId));

            if (ticket == null) throw new Exception("Ticket not found");

             ticket.AssignAgent(new UserId(command.SupportAgentId));

            await _repository.SaveChangesAsync();
        }
    }
}
