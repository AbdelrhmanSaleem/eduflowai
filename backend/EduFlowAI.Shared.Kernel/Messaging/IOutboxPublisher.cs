using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Shared.Kernel.Messaging
{
    public interface IOutboxPublisher
    {
        ValueTask PublishAsync<TMessage>(TMessage message) where TMessage : notnull;
        Task SaveChangesAndFlushMessagesAsync(CancellationToken cancellationToken);
    }
}
