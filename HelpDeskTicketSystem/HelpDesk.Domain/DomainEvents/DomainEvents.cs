using HelpDesk.Domain.ValueObjects;

namespace HelpDesk.Domain.DomainEvents
{
    public interface IDomainEvent { }

    public record TicketClosed(TicketId TicketId, UserId TicketClosedBy, DateTime? ClosedAt) : IDomainEvent;

    public record TicketOpened(TicketId TicketId, UserId CustomerId, Priority Priority, SlaLimit CurrentSla, DateTime OpenedAt):IDomainEvent;

    public record MessageAdded(TicketId TicketId, Message Message) : IDomainEvent;

    public record AgentAssigned(TicketId TicketId, UserId AgentId, DateTime AssignedAt):IDomainEvent;

    public record TicketEscalated(TicketId TicketId, UserId ManagerId, DateTime? EscalatedAt) : IDomainEvent;

    public record TicketReassigned(TicketId TicketId, UserId AgentId, DateTime ReassignedAt) : IDomainEvent;

    public record TicketReopened(TicketId TicketId, Message Message, UserId ReopenedBy, DateTime? ReopenedAt):IDomainEvent;
}
