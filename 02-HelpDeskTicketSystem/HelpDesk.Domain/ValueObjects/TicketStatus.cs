
namespace HelpDesk.Domain.ValueObjects
{
    public record TicketStatus
    {
        public string Value {  get; init; }

        public TicketStatus(string value)=> Value = value;

        public static TicketStatus Open = new("Open");
        public static TicketStatus Assigned = new("Assigned");
        public static TicketStatus Escalated = new("Escalated");
        public static TicketStatus Closed = new("Closed");
        public static TicketStatus Error = new("Error");


        public bool CanReopen() => Value == "Closed";
    }
}
