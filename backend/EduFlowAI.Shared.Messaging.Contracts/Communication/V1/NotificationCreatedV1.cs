using EduFlowAI.Shared.Messaging.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using Wolverine.Attributes;

namespace EduFlowAI.Shared.Messaging.Contracts.Communication.V1
{
    [MessageIdentity("eduflow.communication.notification-created", Version = 1)]
    public sealed record NotificationCreatedV1(
        Guid MessageId,
        Guid CorrelationId,
        Guid? CausationId,
        Guid NotificationId,
        string UserId,
        Guid? ApplicationId,
        string NotificationType,
        string Message,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset OccurredAtUtc
    ) : IIntegrationMessage;
}
