using EduFlowAI.Shared.Messaging.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using Wolverine.Attributes;

namespace EduFlowAI.Shared.Messaging.Contracts.Communication.V1
{
    [MessageIdentity("eduflow.communication.send-email", Version = 1)]
    public sealed record SendEmailV1(
        Guid MessageId, Guid CorrelationId, Guid? CausationId,
        int EmailType,
        string RecipientEmail,
        string? DataJson,
        DateTimeOffset OccurredAtUtc
    ) : IIntegrationMessage;
}
