using HelpDesk.Application.Common;
using HelpDesk.Domain;
using HelpDesk.Domain.ValueObjects;


namespace HelpDesk.Application.Tickets.Commands
{
    public class OpenTicketHandler
    {
        private readonly ITicketRepository _repository;

        public OpenTicketHandler(ITicketRepository repository)=> _repository = repository;
        public async Task Handle(OpenTicketCommand command) {

            TicketId ticketId = TicketId.New();
            var customerId = new UserId( command.CustomerId);
            var priority = command.Priority;
            var ticket = new Ticket(ticketId, customerId, priority);

            ticket.AddMessage(customerId, command.InitialMessage);

            //Save to repository
          await  _repository.AddTicket(ticket);
          await  _repository.SaveChangesAsync();
        }
    }
}
