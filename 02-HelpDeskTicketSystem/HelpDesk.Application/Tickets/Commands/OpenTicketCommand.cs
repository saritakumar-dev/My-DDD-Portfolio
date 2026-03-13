using HelpDesk.Domain.ValueObjects;
using System;   
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace HelpDesk.Application.Tickets.Commands
{

    public record OpenTicketCommand
    {
        public Guid CustomerId { get; init; }
        public Priority Priority { get; init; }

        public string InitialMessage { get; init; }

        private OpenTicketCommand() { }

        public OpenTicketCommand(Guid customerId,  Priority priority, string initialMessage)
        {

            if (customerId == Guid.Empty) throw new ArgumentNullException("CustomerId cannot be null");

            if(string.IsNullOrWhiteSpace( priority.Value)) throw new ArgumentNullException("Priority cannot be null");

            if (string.IsNullOrWhiteSpace(initialMessage))
            {
                throw new ArgumentNullException("Message content cannot be empty.");
            }
            CustomerId = customerId;
            Priority = priority;
            InitialMessage = initialMessage;
        }
    }
}
