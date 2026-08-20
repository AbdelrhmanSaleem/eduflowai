using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Shared.Messaging.Contracts.Abstractions
{
    public interface IIntegrationMessage
    {
        Guid MessageId { get; }

        // connect all messages belonging to one business flow
        Guid CorrelationId { get; }
        // the message that caused this one
        Guid? CausationId { get; }

        DateTimeOffset OccurredAtUtc { get; }
    }
}
