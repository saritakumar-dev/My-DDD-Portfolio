using System;

namespace BankLedger.Core.Common
{
    public interface ICommandHandler<in TCommand> where TCommand : class
    {
        Task HandleAsync(TCommand command, CancellationToken cancellationToken);
    }

    public interface IDomainEventHandler<in TEvent> where TEvent : class
    {
        Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
    }
}
