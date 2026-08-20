using System;

namespace EduFlowAI.AI.Application.DocumentVerification;

// A known, safe-to-report final failure. The handler converts this into
// ApplicantDocumentVerificationFailedV1. Anything else escapes for Wolverine's
// retry/DLQ policies instead - see VerifyApplicantDocumentV1Handler.
public sealed class DocumentVerificationFinalException : Exception
{
    public string ErrorCode { get; }
    public string SafeMessage { get; }
    public int AttemptCount { get; }

    public DocumentVerificationFinalException(
        string errorCode,
        string safeMessage,
        int attemptCount,
        Exception? innerException = null)
        : base(safeMessage, innerException)
    {
        ErrorCode = errorCode;
        SafeMessage = safeMessage;
        AttemptCount = attemptCount;
    }
}
