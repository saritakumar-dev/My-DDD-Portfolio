using HelpDesk.Application.Common;
using HelpDesk.Domain.ValueObjects;

namespace HelpDesk.Application.Tickets.Commands
{
    public class ReopenTicketHandler
    {
        private readonly ITicketRepository _repository;

        public ReopenTicketHandler(ITicketRepository repository) => _repository = repository;

        public async Task Handle(ReopenTicketCommand command, TimeProvider clock)
        {
            var ticket = await _repository.GetTicketAsync(new TicketId(command.TicketId)) ?? throw new ArgumentNullException("Ticket cannot be null");
            var customerId = new UserId(command.CustomerId);
            ticket.Reopen(customerId,(UserRole)command.Role, new Message(command.Message, customerId, clock.GetUtcNow().UtcDateTime), clock);
            await _repository.SaveChangesAsync();
        }
    }
}
