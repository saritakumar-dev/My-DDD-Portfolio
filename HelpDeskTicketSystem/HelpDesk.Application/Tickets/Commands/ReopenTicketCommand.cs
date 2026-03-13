namespace HelpDesk.Application.Tickets.Commands
{
    public record ReopenTicketCommand
    {
        public Guid TicketId {  get; init; }

        public string Message { get; init; } = string.Empty;

        public int Role {  get; init; }

        public Guid CustomerId { get; init; }

        public ReopenTicketCommand(Guid ticketId, Guid customerId, string message, int role)
        {
            TicketId = ticketId;
            CustomerId = customerId;
            Message = message;
            Role = role;
        }
    }
}