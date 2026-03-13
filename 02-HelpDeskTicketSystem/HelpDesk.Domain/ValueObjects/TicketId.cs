
namespace HelpDesk.Domain.ValueObjects
{
    public record TicketId
    {
        public Guid Value { get; init; }
        public static TicketId New() => new(Guid.NewGuid());

        
        public TicketId(Guid value)
        {
            // Explicitly handle empty GUIDs to maintain domain invariants
            if (value == Guid.Empty)
                throw new ArgumentException("Ticket ID cannot be empty.");

            Value = value;
        }
    
    }
}

