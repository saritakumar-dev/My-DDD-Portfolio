using HelpDesk.Application.Common;
using HelpDesk.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpDesk.Application.Tickets.Commands
{
    public class EscalateTicketHandler
    {
        ITicketRepository _repository;
        public EscalateTicketHandler(ITicketRepository repository) => _repository = repository;

        public async Task Handle(EscalateTicketCommand command)
        {
          var ticket = await _repository.GetTicketAsync(new TicketId(command.TicketId));
            if (ticket == null) throw new Exception("Ticket not found");

            ticket.Escalate(new UserId(command.ManagerId));

            await _repository.SaveChangesAsync();
        }
    }
}
