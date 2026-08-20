using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.AI.Application.DocumentVerification;
using EduFlowAI.Shared.Messaging.Contracts.Documents.V1;
using Wolverine;
using Wolverine.Attributes;

namespace EduFlowAI.AI.Application.DocumentVerification.Messaging;

public sealed class VerifyApplicantDocumentV1Handler
{
    private readonly IDocumentVerificationFileReader _fileReader;
    private readonly IDocumentVerificationContextReader _contextReader;
    private readonly IDocumentVerificationService _verificationService;

    public VerifyApplicantDocumentV1Handler(
        IDocumentVerificationFileReader fileReader,
        IDocumentVerificationContextReader contextReader,
        IDocumentVerificationService verificationService)
    {
        _fileReader = fileReader;
        _contextReader = contextReader;
        _verificationService = verificationService;
    }

    // AI and file I/O are long external operations - never hold an EF transaction open around
    // this handler. It performs no business-table writes; it only publishes the outcome.
    [NonTransactional]
    [RetryNow(typeof(HttpRequestException), 1000, 3000, 10000, 12000)]
    [MaximumAttempts(5)]
    public async Task Handle(
        VerifyApplicantDocumentV1 message,
        IMessageContext messages,
        CancellationToken cancellationToken)
    {
        try
        {
            Stream file;
            try
            {
                file = await _fileReader.OpenReadAsync(message.SourceStorageKey, cancellationToken);
            }
            catch (FileNotFoundException ex)
            {
                throw new DocumentVerificationFinalException(
                    "DOCUMENT_FILE_NOT_FOUND",
                    "The source document could not be found in storage.",
                    attemptCount: 1,
                    innerException: ex);
            }

            await using (file)
            {
                var context = await _contextReader.GetAsync(
                    message.ApplicationId,
                    message.DocumentType,
                    cancellationToken);

                var result = await _verificationService.VerifyAsync(
                    new DocumentVerificationInput(
                        DocumentId: message.DocumentId,
                        DocumentType: message.DocumentType,
                        OriginalFileName: message.OriginalFileName,
                        File: file,
                        ExpectedFields: context.ExpectedFields),
                    cancellationToken);

                var completed = new ApplicantDocumentVerificationCompletedV1(
                    MessageId: Guid.NewGuid(),
                    CorrelationId: message.CorrelationId,
                    CausationId: message.MessageId,
                    DocumentId: message.DocumentId,
                    ApplicationId: message.ApplicationId,
                    SourceStorageKey: message.SourceStorageKey,
                    Outcome: result.Outcome,
                    Details: result.Details,
                    VerifiedAtUtc: DateTimeOffset.UtcNow,
                    OccurredAtUtc: DateTimeOffset.UtcNow);

                await messages.SendAsync(completed);
            }
        }
        catch (DocumentVerificationFinalException exception)
        {
            var failed = new ApplicantDocumentVerificationFailedV1(
                MessageId: Guid.NewGuid(),
                CorrelationId: message.CorrelationId,
                CausationId: message.MessageId,
                DocumentId: message.DocumentId,
                ApplicationId: message.ApplicationId,
                SourceStorageKey: message.SourceStorageKey,
                ErrorCode: exception.ErrorCode,
                SafeErrorMessage: exception.SafeMessage,
                AttemptCount: exception.AttemptCount,
                FailedAtUtc: DateTimeOffset.UtcNow,
                OccurredAtUtc: DateTimeOffset.UtcNow);

            await messages.SendAsync(failed);
        }
    }
}
