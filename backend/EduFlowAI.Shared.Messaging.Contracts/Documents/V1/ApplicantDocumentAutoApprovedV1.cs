using EduFlowAI.Shared.Messaging.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using Wolverine.Attributes;

namespace EduFlowAI.Shared.Messaging.Contracts.Documents.V1
{
    [MessageIdentity("eduflow.documents.applicant-document-auto-approved", Version = 1)]
        public sealed record ApplicantDocumentAutoApprovedV1(
        Guid MessageId, Guid CorrelationId, Guid? CausationId,
        Guid DocumentId, Guid ApplicationId, string ApplicantUserId,
        string DocumentType,
        DateTimeOffset OccurredAtUtc

    ) : IIntegrationMessage;
}
