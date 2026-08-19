
namespace BankLedger.Core.Common.MessageBus
{
    public interface IMessageBus
    {
        Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;
    }
}
