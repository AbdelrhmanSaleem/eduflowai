using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Enums;
using EduFlowAI.Documents.Application.DbContextAbstraction;
using EduFlowAI.Documents.Application.Services;
using EduFlowAI.Documents.Domain.Enums;
using EduFlowAI.Shared.Messaging.Contracts.Documents.V1;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Wolverine.Attributes;

namespace EduFlowAI.Documents.Application.Messaging;

public sealed class ApplicantDocumentVerificationFailedV1Handler
{
    private readonly IDocumentDbContext _dbContext;
    private readonly IStatusTransitionCoordinator _statusCoordinator;

    public ApplicantDocumentVerificationFailedV1Handler(
        IDocumentDbContext dbContext,
        IStatusTransitionCoordinator statusCoordinator)
    {
        _dbContext = dbContext;
        _statusCoordinator = statusCoordinator;
    }

    [Transactional(typeof(IDocumentDbContext))]
    public async Task Handle(
        ApplicantDocumentVerificationFailedV1 message,
        CancellationToken cancellationToken)
    {
        var document = await _dbContext.ApplicantDocuments
            .SingleOrDefaultAsync(
                x => x.Id == message.DocumentId,
                cancellationToken);

        if (document is null)
        {
            throw new InvalidOperationException(
                $"ApplicantDocument {message.DocumentId} was not found.");
        }

        // Concurrency check: Ensure a replacement wasn't uploaded during the crash
        if (!string.Equals(
                document.StorageKey,
                message.SourceStorageKey,
                StringComparison.Ordinal))
        {
            return;
        }

        // ADDED: Business Idempotency Guard
        // Do not let a delayed failure message overwrite a document that is already 
        // successfully approved or already flagged for human review.
        if (document.Status is DocumentStatus.Approved or DocumentStatus.NeedsHumanReview)
        {
            return;
        }

        // Apply the failure state
        document.Status = DocumentStatus.NeedsHumanReview;
        document.VerifiedAt = null;
        document.UpdatedAt = DateTimeOffset.UtcNow;

        document.VerificationDetailsJson = JsonSerializer.Serialize(new
        {
            message.ErrorCode,
            message.SafeErrorMessage,
            message.AttemptCount,
            message.FailedAtUtc
        });

        // Notify Halim's Application State Machine about the technical failure after resolve all docs status
        var reviewResult = await DocumentPackageReviewResolver.ResolveAsync(
            _dbContext, document.ApplicationId,
            document.Id,
            DocumentStatus.NeedsHumanReview,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);    

        if (reviewResult is not null)
        {
            var transition = await _statusCoordinator.ProcessDocumentReviewAsync(document.ApplicationId, reviewResult);
            if (!transition.IsSuccess)
            {
                throw new InvalidOperationException(transition.ErrorMessage);
            }
        }
    }
}
