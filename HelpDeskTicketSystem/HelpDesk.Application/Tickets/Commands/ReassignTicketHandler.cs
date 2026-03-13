using HelpDesk.Application.Common;
using HelpDesk.Domain.ValueObjects;


namespace HelpDesk.Application.Tickets.Commands
{
    public class ReassignTicketHandler
    {
        private ITicketRepository _repository;
        public ReassignTicketHandler(ITicketRepository repository) => _repository = repository;

        public  async Task Handle(ReassignTicketCommand command, TimeProvider timeProvider)
        {
            var ticket= await  _repository.GetTicketAsync(new TicketId(command.TicketId));
            if (ticket == null) throw new ArgumentNullException("The ticket cannot be null");

            ticket.ReassignTicket(new UserId(command.NewAgentId), timeProvider);

            await _repository.SaveChangesAsync();
        }
    }
}
