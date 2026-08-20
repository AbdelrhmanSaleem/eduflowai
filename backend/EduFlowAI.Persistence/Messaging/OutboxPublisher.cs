using EduFlowAI.Shared.Kernel.Messaging;
using System;
using System.Collections.Generic;
using System.Text;
using Wolverine.EntityFrameworkCore;

namespace EduFlowAI.Persistence.Messaging
{
    public sealed class OutboxPublisher : IOutboxPublisher
    {
        private readonly IDbContextOutbox<EduFlowAIDbContext> _outbox;

        public OutboxPublisher(IDbContextOutbox<EduFlowAIDbContext> outbox) => _outbox = outbox;

        public ValueTask PublishAsync<TMessage>(TMessage message) where TMessage : notnull
            => _outbox.PublishAsync(message);

        public Task SaveChangesAndFlushMessagesAsync(CancellationToken cancellationToken)
            => _outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
    }
}
