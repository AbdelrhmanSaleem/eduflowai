using EduFlowAI.Shared.Messaging.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using Wolverine.Attributes;

namespace EduFlowAI.Shared.Messaging.Contracts.Documents.V1
{
    [MessageIdentity("eduflow.documents.replacement-fulfilled", Version = 1)]
    public sealed record DocumentReplacementFulfilledV1(
        Guid MessageId,
        Guid CorrelationId,
        Guid? CausationId,
        Guid ReplacementRequestId,
        Guid DocumentId,
        Guid ApplicationId,
        string ApplicantUserId,
        string NewStorageKey,
        string NewOriginalFileName,
        string DocumentType,
        DateTimeOffset FulfilledAtUtc, DateTimeOffset OccurredAtUtc
    ) : IIntegrationMessage;
}
