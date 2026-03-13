/* 
 * 📌 ARCHITECTURAL STICKY NOTE:
 * 1. Events are immutable facts (Records).
 * 2. The Ticket here is the Aggregate whose properties are value objects.
 * 3. The aggregate is a consistency enforcement boundary.the consistency is enforced by allowing only
      the aggregate’s business logic to modify its state and be at only one place : the aggregate itself. 
 * 4. The aggregate is responsible for validating the input and enforcing all of the relevant business rules and invariants.
 * 5. A domain event is a message describing a significant event that has occurred in the business domain. 
 */

using HelpDesk.Domain.Common;
using HelpDesk.Domain.DomainEvents;
using HelpDesk.Domain.ValueObjects;

namespace HelpDesk.Domain
{
    public class Ticket : AggregateRoot
    {
        private readonly List<Message> _messages = [];

        public TicketId TicketId { get; private set; }
        public Priority Priority { get; private set; }
        public TicketStatus Status { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime? EscalatedAt { get; private set; }

        public SlaLimit CurrentSla { get; private set; }

        public UserId? AssignedAgentId { get; private set; } //Doesn't belong to Aggregate Boundary

        public UserId? ManagerId { get; private set; } // Doesn't belong to Aggregate Boundary

        public DateTime LastActivityAt { get; private set; }
        public bool IsOpenedByAgent { get; private set; }

        public UserId CustomerId { get; private set; } // Doesn't belong to Aggregate Boundary

        public List<Message> Messages => _messages; // belongs to Aggregate Boundary

        public UserId TicketClosedBy { get; private set; } // Doesn't belong to Aggregate Boundary
        public DateTime? TicketClosedAt { get; private set; }
        public DateTime? LastReopenedAt { get; private set; }

        public UserId LastReopenedBy {  get; private set; } //  Doesn't belong to Aggregate Boundary
        public int? ReopenedCount { get; private set; }


        //For EF Core, allow EF Core to create the object and then "set" the properties via reflection, while still forcing
        //the Application Layer to use the "Rich" constructor.
        private Ticket() { }
        public Ticket(TicketId ticketId, UserId userId, Priority priority)
        {
            TicketId = ticketId;
            Priority = priority;
            CustomerId = userId;
            CurrentSla = SlaLimit.Create(priority, DateTime.UtcNow);
            Status = TicketStatus.Open;
            CreatedAt = DateTime.UtcNow;
            LastActivityAt = DateTime.UtcNow;
            this.AddDomainEvent(new TicketOpened(TicketId, CustomerId, priority, CurrentSla, CreatedAt));
        }

        public void AssignAgent(UserId agentId)
        {
            if (this.Status != TicketStatus.Open) throw new InvalidOperationException("Can assign an agent only for an open ticket.");
            AssignedAgentId = agentId;
            LastActivityAt = DateTime.UtcNow;
            this.IsOpenedByAgent = false;
            this.AddDomainEvent(new AgentAssigned(this.TicketId, agentId, DateTime.UtcNow));
        }

        public void AddMessage(UserId authorId, string content)
        {
            var message = new Message(content, authorId, DateTime.UtcNow);
            this._messages.Add(message);

            // Business Rule: Reset activity timer whenever correspondence happens
            this.LastActivityAt = DateTime.UtcNow;
            this.AddDomainEvent(new MessageAdded(this.TicketId, message));
        }

        public void Escalate(UserId managerId)
        {
            if (this.Status == TicketStatus.Closed)
                throw new InvalidOperationException("Cannot escalate a closed ticket.");

            //if(!this.CurrentSla.IsBreached())
            //  throw new InvalidOperationException("Ticket cannot be escalated yet. The agent still has time to reply within the SLA.");

            this.EscalatedAt = DateTime.UtcNow;
            this.LastActivityAt = DateTime.UtcNow;
            this.ManagerId = managerId;
            this.Status = TicketStatus.Escalated;

            // Business Rule: Escalation reduces response time limit by 33%
            this.CurrentSla = this.CurrentSla.ApplyEscalation();
            this.AddDomainEvent(new TicketEscalated(this.TicketId, managerId, EscalatedAt));
        }



        // Rule: Auto-reassign if not opened within 50% of the response time limit
        public void ReassignTicket(UserId newAgentId, TimeProvider clock)
        {
            if (this.Status != TicketStatus.Escalated || !EscalatedAt.HasValue) throw new Exception("Only Escalated Tickets can be reassigned");
            var slaWindow = CurrentSla.Deadline - this.EscalatedAt.Value;

            DateTimeOffset now = clock.GetUtcNow();

            var halfWindow = TimeSpan.FromTicks(slaWindow.Ticks / 2);

            var reassignmentDeadline = this.EscalatedAt.Value.Add(halfWindow);

            if (now.UtcDateTime <= reassignmentDeadline)
                throw new InvalidOperationException("Cannot reassign yet; the 50% time limit has not passed.");

            if (this.IsOpenedByAgent)
                throw new InvalidOperationException("Cannot reassign; the agent has already opened the ticket.");

            this.AssignedAgentId = newAgentId;
            this.LastActivityAt = DateTime.UtcNow;

            this.AddDomainEvent(new TicketReassigned(this.TicketId, newAgentId, DateTime.UtcNow));
        }

        public void Close(UserId actorId, UserRole role)
        {
            // Business Rule: Escalated tickets can only be closed by customer or manager
            if (this.EscalatedAt.HasValue && role == UserRole.SupportAgent)
                throw new UnauthorizedAccessException("Agents cannot close escalated tickets.");

            this.Status = TicketStatus.Closed;
            this.TicketClosedBy = actorId;
            this.TicketClosedAt = DateTime.UtcNow;
            this.AddDomainEvent(new TicketClosed(this.TicketId, TicketClosedBy, this.TicketClosedAt));
        }

        public void Reopen(UserId customerId, UserRole role, Message message, TimeProvider clock)
        {
            var now = clock.GetUtcNow().UtcDateTime;

            if (this.Status != TicketStatus.Closed || !this.TicketClosedAt.HasValue)
                throw new InvalidOperationException("Only closed tickets can be reopened");
            if ((now - this.TicketClosedAt.Value).TotalDays > 7)
                throw new InvalidOperationException("Tickets closed more than 7 days ago cannot be reopened.");
            if (UserRole.Customer != role || this.CustomerId != customerId)
                throw new InvalidOperationException("Only the original customer can reopen a ticket");
            if (customerId == null)
                throw new ArgumentNullException("Customer cannot be null");
            if(message == null ) throw new ArgumentNullException("Please provide a valid reason to reopen the ticket");

            this.Status = TicketStatus.Open;
            this.LastActivityAt = now;
            this.LastReopenedAt = now;
            this.LastReopenedBy = customerId;
            this._messages.Add(message);
            this.ReopenedCount++;
            this.AddDomainEvent(new TicketReopened(this.TicketId, message, this.LastReopenedBy, this.LastReopenedAt));
        }

        public void AutoClose(UserId agentId, TimeProvider clock)
        {
            var now = clock?.GetUtcNow().UtcDateTime;

            // Escalated tickets cannot be closed automatically
            if (this.Status == TicketStatus.Escalated) throw new InvalidOperationException("Escalated Tickets cannot be closed automatically");

            if (now < LastActivityAt.AddDays(7))
                throw new InvalidOperationException("Wait for 7 days for the customer response");

            this.Status = TicketStatus.Closed;
            this.TicketClosedAt = now;
            this.TicketClosedBy = agentId;
            this.AddDomainEvent(new TicketClosed(TicketId, TicketClosedBy, TicketClosedAt));
        }
    }

}
