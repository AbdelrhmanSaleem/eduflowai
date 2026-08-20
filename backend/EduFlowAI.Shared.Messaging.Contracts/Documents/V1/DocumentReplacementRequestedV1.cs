using EduFlowAI.Shared.Messaging.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using Wolverine.Attributes;

namespace EduFlowAI.Shared.Messaging.Contracts.Documents.V1
{
    [MessageIdentity("eduflow.documents.replacement-requested", Version = 1)]
    public sealed record DocumentReplacementRequestedV1(
        Guid MessageId, Guid CorrelationId, Guid? CausationId,
        Guid ReplacementRequestId, Guid DocumentId, Guid ApplicationId,
        string ApplicantUserId, string RequestedByUserId,
        string DocumentType, string Reason,
        DateTimeOffset RequestedAtUtc, DateTimeOffset OccurredAtUtc
    ) : IIntegrationMessage;
}
