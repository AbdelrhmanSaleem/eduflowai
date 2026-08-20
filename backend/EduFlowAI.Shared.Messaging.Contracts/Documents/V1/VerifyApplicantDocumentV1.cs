using EduFlowAI.Shared.Messaging.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using Wolverine.Attributes;

namespace EduFlowAI.Shared.Messaging.Contracts.Documents.V1
{
    [MessageIdentity("eduflow.documents.verify-applicant-document", Version = 1)]
    public sealed record VerifyApplicantDocumentV1(
        Guid MessageId,
        Guid CorrelationId,
        Guid? CausationId,
        Guid DocumentId,
        Guid ApplicationId,
        string DocumentType,
        string SourceStorageKey,
        string OriginalFileName,
        DateTimeOffset OccurredAtUtc
    ) : IIntegrationMessage;
}
