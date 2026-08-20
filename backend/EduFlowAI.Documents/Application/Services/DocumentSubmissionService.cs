using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Documents.Application.DbContextAbstraction;
using EduFlowAI.Documents.Application.Interfaces;
using EduFlowAI.Documents.Application.Mapping;
using EduFlowAI.Documents.Application.Messaging;
using EduFlowAI.Documents.Domain.Enums;
using EduFlowAI.Identity.Application.Interfaces;
using EduFlowAI.Shared.Kernel.Common;
using EduFlowAI.Shared.Messaging.Contracts.Documents.V1;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EduFlowAI.Documents.Application.Services;

public sealed class DocumentSubmissionService : IDocumentSubmissionService
{
    private readonly IDocumentVerificationOutbox _outbox;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationAcessReader _applicationAccessReader;
    private readonly IProgramRequirementReader _programRequirementReader;
    private readonly IProfileService _profileService;
    private readonly IStatusTransitionCoordinator _statusTransitionCoordinator;

    public DocumentSubmissionService(
        IDocumentVerificationOutbox outbox,
        ICurrentUserService currentUserService,
        IApplicationAcessReader applicationAccessReader,
        IProgramRequirementReader programRequirementReader,
        IProfileService profileService,
        IStatusTransitionCoordinator statusTransitionCoordinator)
    {
        _outbox = outbox;
        _currentUserService = currentUserService;
        _applicationAccessReader = applicationAccessReader;
        _programRequirementReader = programRequirementReader;
        _profileService = profileService;
        _statusTransitionCoordinator = statusTransitionCoordinator;
    }

    public async Task<Result<int>> SubmitPackageAsync(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        // 1. Authentication Guard
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<int>.Failure(401, "User identity could not be verified.");
        }

        // 2. Authorization Guard
        var hasAccess = await _applicationAccessReader.CanAccessDocumentsAsync(applicationId, userId, cancellationToken);
        if (!hasAccess)
        {
            return Result<int>.Failure(403, "You are not authorized to submit documents for this application.");
        }

        // 3. Gather Required Data for Validation
        var programId = await _applicationAccessReader.GetProgramIdAsync(applicationId, cancellationToken);
        var profile = await _profileService.GetAsync(userId, cancellationToken);

        if (!profile.IsSuccess || profile.Value is null)
        {
            return Result<int>.Failure(400, "Applicant profile not found.");
        }

        var gender = GenderMapper.ToGender(profile.Value.Gender);

        if (gender is null)
        {
            return Result<int>.Failure(400, "Invalid or missing gender in applicant profile.");
        }

        var requirementSet = await _programRequirementReader.ResolveAsync(programId, (Admission.Domain.Enums.Gender)gender, cancellationToken);

        if (requirementSet is null)
        {
            return Result<int>.Failure(400, "Could not determine document requirements for this program. Please contact support.");
        }

        var requiredDocumentTypes = requirementSet.DocumentTypes;

        // 4. Single DB Query: Fetch ALL documents for this application to verify completeness
        var existingDocuments = await _outbox.DbContext.ApplicantDocuments
            .Where(d => d.ApplicationId == applicationId)
            .ToListAsync(cancellationToken);

        // Filter out Rejected documents when calculating what the applicant has successfully provided
        var validDocumentTypes = existingDocuments
            .Where(d => d.Status != DocumentStatus.Rejected)
            .Select(d => d.DocumentType)
            .ToList();

        // 5. Business Rule: "Complete Package" Validation (Direct comparison since they share the same enum)
        var missingTypes = requiredDocumentTypes.Except(validDocumentTypes).ToList();
        if (missingTypes.Any())
        {
            var missingNames = string.Join(", ", missingTypes.Select(m => m.ToString()));
            return Result<int>.Failure(400, $"Cannot submit application package. Missing required documents: {missingNames}");
        }

        // 6. Filter to ONLY the pending documents that need AI Verification
        var documentsToSubmit = existingDocuments
            .Where(d => d.Status == DocumentStatus.Uploaded)
            .ToList();

        if (!documentsToSubmit.Any())
        {
            return Result<int>.Failure(400, "All required documents are already submitted and processing.");
        }

        // Capture a single timestamp for the entire logical transaction
        var now = DateTimeOffset.UtcNow;

        var transition = await _statusTransitionCoordinator.MarkDocumentVerificationStartedAsync(applicationId);
        if (!transition.IsSuccess)
        {
            return Result<int>.Failure(409, transition.ErrorMessage);
        }

        // 7. In-Memory Orchestration
        foreach (var document in documentsToSubmit)
        {
            document.Status = DocumentStatus.Verifying;
            document.VerificationDetailsJson = null;
            document.VerifiedAt = null;
            document.UpdatedAt = now;

            var command = new VerifyApplicantDocumentV1(
                MessageId: Guid.NewGuid(),
                CorrelationId: document.ApplicationId,
                CausationId: null,
                DocumentId: document.Id,
                ApplicationId: document.ApplicationId,
                DocumentType: document.DocumentType.ToString(),
                SourceStorageKey: document.StorageKey,
                OriginalFileName: document.OriginalFileName,
                OccurredAtUtc: now);

            await _outbox.PublishAsync(command, cancellationToken);
        }

        // 8. Atomic Transaction
        await _outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);

        return Result<int>.Success(documentsToSubmit.Count, 200, "Document package submitted successfully. Verification is in progress.");
    }
}