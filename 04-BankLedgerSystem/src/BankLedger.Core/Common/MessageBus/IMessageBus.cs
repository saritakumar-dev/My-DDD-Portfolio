using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankLedger.Core.Common.MessageBus
{
    public interface IMessageBus
    {
        Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;
    }
}
