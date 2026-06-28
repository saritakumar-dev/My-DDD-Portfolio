using BankLedger.Core.Common;
using BankLedger.Core.Common.MessageBus;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankLedger.WriteProject.Infrastructure.Messaging
{
    public class InMemoryMessageBus : IMessageBus
    {
        private readonly IServiceProvider _serviceProvider;

        public InMemoryMessageBus(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
        {
            var handlers = _serviceProvider.GetServices<IDomainEventHandler<TEvent>>();

            foreach (var handler in handlers)
            {
                await handler.HandleAsync(@event, cancellationToken);
            }
        }
    }
}
