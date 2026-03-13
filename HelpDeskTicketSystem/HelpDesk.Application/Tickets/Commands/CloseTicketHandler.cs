using HelpDesk.Application.Common;
using HelpDesk.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpDesk.Application.Tickets.Commands
{
    public class CloseTicketHandler
    {
        ITicketRepository _repository;
        public CloseTicketHandler(ITicketRepository repository) => _repository = repository;

        public async Task Handle(CloseTicketCommand command)
        {
            var ticket = await _repository.GetTicketAsync(new TicketId(command.TicketId));

            if (ticket == null) throw new ArgumentNullException("the ticket not found");

            UserRole role = (UserRole)command.UserRole;

            ticket.Close(new UserId(command.UserId), role);

            await _repository.SaveChangesAsync();
        }
    }
}
