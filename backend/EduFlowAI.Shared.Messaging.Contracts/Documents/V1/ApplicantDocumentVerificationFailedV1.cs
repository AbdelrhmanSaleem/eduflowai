using EduFlowAI.Shared.Messaging.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using Wolverine.Attributes;

namespace EduFlowAI.Shared.Messaging.Contracts.Documents.V1
{
    [MessageIdentity("eduflow.documents.applicant-document-verification-failed", Version = 1)]
        public sealed record ApplicantDocumentVerificationFailedV1(
        Guid MessageId,
        Guid CorrelationId,
        Guid? CausationId,
        Guid DocumentId,
        Guid ApplicationId,
        string SourceStorageKey,
        string ErrorCode,
        string SafeErrorMessage,
        int AttemptCount,
        DateTimeOffset FailedAtUtc,
        DateTimeOffset OccurredAtUtc
    ) : IIntegrationMessage;
}
