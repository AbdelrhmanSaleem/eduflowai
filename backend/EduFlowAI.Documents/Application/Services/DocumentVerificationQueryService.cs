using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Documents.Application.DbContextAbstraction;
using EduFlowAI.Documents.Application.DTOs;
using EduFlowAI.Documents.Application.Interfaces;
using EduFlowAI.Documents.Domain.Enums;
using EduFlowAI.Identity.Application.Interfaces;
using EduFlowAI.Shared.Kernel.Common;
using EduFlowAI.Shared.Messaging.Contracts.Documents.V1;
using Microsoft.EntityFrameworkCore;

namespace EduFlowAI.Documents.Application.Services;

// Read-only. Never writes ApplicantDocument.Status or any other field - Mansy owns that.
public sealed class DocumentVerificationQueryService : IDocumentVerificationQueryService
{
    private readonly IDocumentDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationAcessReader _applicationAccessReader;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public DocumentVerificationQueryService(
        IDocumentDbContext dbContext,
        ICurrentUserService currentUserService,
        IApplicationAcessReader applicationAccessReader)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _applicationAccessReader = applicationAccessReader;
    }

    public async Task<Result<DocumentVerificationResponseDto>> GetVerificationAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<DocumentVerificationResponseDto>.Failure(401, "User is not authenticated.");
        }

        var document = await _dbContext.ApplicantDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document is null)
        {
            return Result<DocumentVerificationResponseDto>.Failure(404, "Document not found.");
        }

        var hasAccess = await _applicationAccessReader.CanAccessDocumentsAsync(
            document.ApplicationId, userId, cancellationToken);

        if (!hasAccess)
        {
            return Result<DocumentVerificationResponseDto>.Failure(
                403, "You do not have permission to view this document's verification result.");
        }

        // The completed handler always sets VerifiedAt; the failed handler always leaves it null.
        // That's the only reliable way to tell the two JSON shapes stored in the same column apart
        // - both deserialize "successfully" (with nulls) into the wrong DTO otherwise.
        var successDetails = document.VerifiedAt.HasValue
            ? TryParseSuccessDetails(document.VerificationDetailsJson)
            : null;

        var failureDetails = !document.VerifiedAt.HasValue
            ? TryParseFailureDetails(document.VerificationDetailsJson)
            : null;

        var response = new DocumentVerificationResponseDto(
            DocumentId: document.Id,
            Status: document.Status.ToString(),
            OverallResult: ResolveOverallResult(document.Status, document.VerifiedAt, document.VerificationDetailsJson),
            Fields: (successDetails?.Fields ?? Array.Empty<VerificationFieldComparisonV1>())
                .Select(f => new DocumentVerificationFieldDto(f.FieldName, f.ExtractedValue, f.ExpectedValue, f.IsMatch, f.Notes))
                .ToList(),
            MissingFields: successDetails?.MissingFields ?? new List<string>(),
            Warnings: successDetails?.Warnings ?? new List<string>(),
            ConfidenceScore: successDetails?.ConfidenceScore,
            ModelName: successDetails?.ModelName,
            TechnicalErrorCode: failureDetails?.ErrorCode,
            TechnicalErrorMessage: failureDetails?.SafeErrorMessage,
            ProcessedAt: document.VerifiedAt);

        return Result<DocumentVerificationResponseDto>.Success(response);
    }

    public async Task<Result<IReadOnlyCollection<DocumentVerificationResponseDto>>> GetVerificationAttemptsAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var current = await GetVerificationAsync(documentId, cancellationToken);
        if (!current.IsSuccess)
        {
            return Result<IReadOnlyCollection<DocumentVerificationResponseDto>>.Failure(
                current.StatusCode, current.Message);
        }

        // No attempt-history table exists yet (see SEIF_IMPLEMENTATION_GUIDE.md 1.2) - the current
        // result is the only "attempt" available today.
        IReadOnlyCollection<DocumentVerificationResponseDto> attempts = new[] { current.Data };
        return Result<IReadOnlyCollection<DocumentVerificationResponseDto>>.Success(attempts);
    }

    private static string? ResolveOverallResult(
        DocumentStatus status,
        DateTimeOffset? verifiedAt,
        string? detailsJson)
    {
        if (string.IsNullOrWhiteSpace(detailsJson))
        {
            return status == DocumentStatus.Verifying ? "Processing" : null;
        }

        if (status == DocumentStatus.Approved)
        {
            // The completed handler only sets Approved when the AI outcome was ExactMatch.
            return "ExactMatch";
        }

        if (verifiedAt.HasValue)
        {
            // A non-exact AI outcome (ValidButDifferent/MissingRequiredData/UnreadableDocument/
            // InvalidDocumentType) all collapse into NeedsHumanReview on ApplicantDocument today,
            // so the original outcome can't be distinguished here - only that review is required.
            return "ReviewRequired";
        }

        return "VerificationFailed";
    }

    private static DocumentVerificationDetailsV1? TryParseSuccessDetails(string? detailsJson)
    {
        if (string.IsNullOrWhiteSpace(detailsJson))
        {
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<DocumentVerificationDetailsV1>(detailsJson, JsonOptions);
            return parsed?.Fields is null ? null : parsed;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record TechnicalFailureDetails(
        string? ErrorCode,
        string? SafeErrorMessage,
        int? AttemptCount,
        DateTimeOffset? FailedAtUtc);

    private static TechnicalFailureDetails? TryParseFailureDetails(string? detailsJson)
    {
        if (string.IsNullOrWhiteSpace(detailsJson))
        {
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<TechnicalFailureDetails>(detailsJson, JsonOptions);
            return parsed?.ErrorCode is null ? null : parsed;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
