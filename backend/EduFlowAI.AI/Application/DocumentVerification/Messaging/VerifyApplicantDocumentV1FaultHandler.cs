using EduFlowAI.Shared.Messaging.Contracts.Documents.V1;
using System;
using System.Collections.Generic;
using System.Text;
using Wolverine;

namespace EduFlowAI.AI.Application.DocumentVerification.Messaging
{
    public static class VerifyApplicantDocumentV1FaultHandler
    {
        public static ApplicantDocumentVerificationFailedV1 Handle(
            Fault<VerifyApplicantDocumentV1> fault)
        {
            var original = fault.Message;
            var now = DateTimeOffset.UtcNow;

            return new ApplicantDocumentVerificationFailedV1(
                MessageId: Guid.NewGuid(),
                CorrelationId: original.CorrelationId,
                CausationId: original.MessageId,
                DocumentId: original.DocumentId,
                ApplicationId: original.ApplicationId,
                SourceStorageKey: original.SourceStorageKey,
                ErrorCode:
                    "DOCUMENT_VERIFICATION_RETRIES_EXHAUSTED",
                SafeErrorMessage:
                    "Document verification could not be completed after multiple attempts and requires manual review.",
                AttemptCount: 5,
                FailedAtUtc: now,
                OccurredAtUtc: now);
        }
    }
}
