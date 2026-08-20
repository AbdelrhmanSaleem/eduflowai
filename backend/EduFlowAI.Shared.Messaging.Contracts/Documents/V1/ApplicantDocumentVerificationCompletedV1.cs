using EduFlowAI.Shared.Messaging.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using Wolverine.Attributes;

namespace EduFlowAI.Shared.Messaging.Contracts.Documents.V1
{
    [MessageIdentity("eduflow.documents.applicant-document-verification-completed", Version = 1)]
    public sealed record ApplicantDocumentVerificationCompletedV1(
        Guid MessageId,
        Guid CorrelationId,
        Guid? CausationId,
        Guid DocumentId,
        Guid ApplicationId,
        string SourceStorageKey,
        DocumentVerificationOutcomeV1 Outcome,
        DocumentVerificationDetailsV1 Details,
        DateTimeOffset VerifiedAtUtc,
        DateTimeOffset OccurredAtUtc

    ) : IIntegrationMessage;
}
