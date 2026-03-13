using HelpDesk.Domain;
using HelpDesk.Domain.DomainEvents;
using HelpDesk.Domain.ValueObjects;
using Microsoft.Extensions.Time.Testing;
using FluentAssertions;

namespace DomainTestProject
{
    public class TicketTests
    {
        [Fact]
        public void Constructor_WhenCalled_ShouldInitializeOpenTicketWithSlaAndEvent()
        {
            var ticketId = TicketId.New();

            var userId = UserId.New();

            var priority = Priority.Urgent;

            var ticket = new Ticket(ticketId, userId, priority);

            var slaDeadline = DateTime.UtcNow.AddHours(4);

            Assert.Equal(TicketStatus.Open, ticket.Status);
            Assert.Equal(userId, ticket.CustomerId);
            Assert.Equal(ticketId, ticket.TicketId);

            // Assert 2: Time Logic (Allowing for a small execution delay)
            ticket.CurrentSla.Deadline.Should().BeCloseTo(slaDeadline, TimeSpan.FromSeconds(5));
            Assert.True(ticket.CreatedAt <= DateTime.UtcNow);

            // Assert 3: Domain Events 
            Assert.Single(ticket.DomainEvents);
            var @event = ticket.DomainEvents.First() as TicketOpened;
            Assert.NotNull(@event);
            Assert.Equal(ticketId, @event.TicketId);
            Assert.Equal(priority, @event.Priority);
        }

        [Fact]
        public void Escalate_WhenCalled_ShouldReduceSlaAndEvent()
        {
            var ticketId = TicketId.New();
            var managerId = UserId.New();
            var customerId = UserId.New();

            var ticket = new Ticket(ticketId, customerId, Priority.Urgent);
            var originalDeadline = ticket.CurrentSla.Deadline;

            //Act
            ticket.Escalate(managerId);
            var newSlaDeadline = ticket.CurrentSla.Deadline;

            Assert.True(newSlaDeadline < originalDeadline);
            Assert.Equal(TicketStatus.Escalated, ticket.Status);
            Assert.Equal(managerId, ticket.ManagerId);

            Assert.Equal(2, ticket.DomainEvents.Count);
            var @event = ticket.DomainEvents.OfType<TicketEscalated>().Single();
            Assert.NotNull(@event);
            Assert.Equal(managerId, @event.ManagerId);
        }

        [Fact]
        public void ReassignTicket_WhenTimeHasPassedAndNotOpened_ShouldChangeAgent()
        {
            var ticketId = TicketId.New();
            var agentId = UserId.New();
            var newAgentId = UserId.New();
            var customerId = UserId.New();
            var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow);

            var ticket = new Ticket(ticketId, customerId, Priority.High);
            ticket.AssignAgent(agentId);
            ticket.Escalate(UserId.New());

            fakeClock.Advance(TimeSpan.FromHours(13));

            //Act
            ticket.ReassignTicket(newAgentId, fakeClock);

            Assert.Equal(newAgentId, ticket.AssignedAgentId);
            Assert.Contains(ticket.DomainEvents, e => e is TicketReassigned);

        }

        [Fact]
        public void CloseTicket_EscalatedTicketCannotBeClosedBySupportAgent()
        {
            var ticketId = TicketId.New();
            var customerId = UserId.New();
            var managerId = UserId.New();
            var ticket = new Ticket(ticketId, customerId, Priority.High);

            ticket.Escalate(managerId);

            //Act
            Assert.Throws<UnauthorizedAccessException>(() => ticket.Close(customerId, UserRole.SupportAgent));
            Assert.Empty(ticket.DomainEvents.OfType<TicketClosed>());
        }

        [Fact]
        public void CloseTicket_EscalatedTicket_ClosedByCustomer()
        {
            var ticketId = TicketId.New();
            var customerId = UserId.New();
            var managerId = UserId.New();
            var ticket = new Ticket(ticketId, customerId, Priority.High);

            ticket.Escalate(managerId);

            //Act
            ticket.Close(customerId, UserRole.Customer);

            Assert.Equal(TicketStatus.Closed, ticket.Status);
            Assert.Equal(customerId, ticket.TicketClosedBy);

            var @event = ticket.DomainEvents.OfType<TicketClosed>().Single();
            Assert.Equal(customerId, @event.TicketClosedBy);
            @event.ClosedAt?.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Reopen_OpenedByCustomerWithinSevenDays()
        {
            var ticketId = TicketId.New();
            var customerId = UserId.New();
            var managerId = UserId.New();
            var message = new Message("unsatisfactory resolution", customerId, DateTime.UtcNow);
            var ticket = new Ticket(ticketId,customerId, Priority.High);
            ticket.Close(customerId, UserRole.Customer);

            var fakeClock = new FakeTimeProvider(DateTimeOffset.UtcNow);
            //Act
            ticket.Reopen(customerId, 0, message, fakeClock);

            Assert.Equal(TicketStatus.Open, ticket.Status);
            Assert.Equal(customerId, ticket.LastReopenedBy);
            Assert.Contains(message, ticket.Messages);

            var @event = ticket.DomainEvents.OfType<TicketReopened>().Single();
            Assert.Equal(customerId, @event.ReopenedBy);
        }
    }
}