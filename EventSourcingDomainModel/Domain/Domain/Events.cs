using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcingDomainModelApp.Domain
{
    // The base for all things that happened
    public abstract record DomainEvent(Guid Id, DateTime OccurredOn);

    // Specific business facts
    public record AccountOpened(Guid Id, string Owner, decimal Limit, DateTime OccurredOn) : DomainEvent(Id, OccurredOn);

    public record MoneyDeposited(Guid Id, decimal Amount, DateTime OccurredOn) : DomainEvent(Id, OccurredOn);
    public record WithdrawalPerformed(Guid Id, decimal Amount, DateTime OccurredOn) : DomainEvent(Id, OccurredOn);

    public record AccountClosed(Guid Id, DateTime OccurredOn) :DomainEvent(Id, OccurredOn);

}
