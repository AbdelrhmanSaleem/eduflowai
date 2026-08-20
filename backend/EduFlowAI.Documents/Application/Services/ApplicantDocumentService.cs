// Cross-module contracts (from Halim, Karim, and Abdallah)
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Enums;
using EduFlowAI.Documents.Application.DbContextAbstraction;
using EduFlowAI.Documents.Application.DTOs;
using EduFlowAI.Documents.Application.Interfaces;
using EduFlowAI.Documents.Application.Mapping;
using EduFlowAI.Documents.Domain.Entities;
using EduFlowAI.Documents.Domain.Enums;
using EduFlowAI.Identity.Application.Interfaces;
using EduFlowAI.Shared.Kernel.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EduFlowAI.Documents.Application.Services;

public sealed class ApplicantDocumentService : IApplicantDocumentService
{
    private readonly IDocumentDbContext _dbContext;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationAcessReader _applicationAccessReader; // Halim's contract
    private readonly IStatusTransitionCoordinator _statusTransitionCoordinator; // Halim's contract
    private readonly IProfileService _profileService; // Karim's contract
    private readonly IProgramRequirementReader _programRequirementReader; // Abdallah's contract
    private readonly IValidator<UploadDocumentDto> _validator;
    private readonly ILogger<ApplicantDocumentService> _logger;

    public ApplicantDocumentService(
        IDocumentDbContext dbContext,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService,
        IApplicationAcessReader applicationAccessReader,
        IProgramRequirementReader programRequirementReader,
        IValidator<UploadDocumentDto> validator,
        ILogger<ApplicantDocumentService> logger,
        IProfileService profileService,
        IStatusTransitionCoordinator statusTransitionCoordinator)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _applicationAccessReader = applicationAccessReader ?? throw new ArgumentNullException(nameof(applicationAccessReader));
        _programRequirementReader = programRequirementReader ?? throw new ArgumentNullException(nameof(programRequirementReader));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _statusTransitionCoordinator = statusTransitionCoordinator ?? throw new ArgumentNullException(nameof(statusTransitionCoordinator));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    }

    public async Task<Result<Guid>> UploadDocumentAsync(
     UploadDocumentDto dto,
     CancellationToken cancellationToken)
    {
        // PHASE 1: Validation (Fail Fast)
        var validationResult = await _validator.ValidateAsync(dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("Upload rejected: Validation failed for Application {ApplicationId}. Errors: {Errors}", dto.ApplicationId, errors);
            return Result<Guid>.Failure(400, errors);
        }

        // PHASE 2: Cross-Module Business Rules & Security
        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<Guid>.Failure(401, "User is not authenticated.");
        }

        var hasAccess = await _applicationAccessReader.CanAccessDocumentsAsync(dto.ApplicationId, userId, cancellationToken);
        if (!hasAccess)
        {
            return Result<Guid>.Failure(403, "User does not have access to upload documents for this application.");
        }

        var programId = await _applicationAccessReader.GetProgramIdAsync(dto.ApplicationId, cancellationToken);
        if (programId == Guid.Empty)
        {
            return Result<Guid>.Failure(404, "Application program context could not be found.");
        }

        var applicant = await _profileService.GetAsync(userId, cancellationToken);
        var applicantGender = GenderMapper.ToGender(applicant?.Value?.Gender) ?? Gender.None;

        var requirementSet = await _programRequirementReader.ResolveAsync(programId, applicantGender, cancellationToken);
        if (requirementSet is null || !requirementSet.DocumentTypes.Contains(dto.DocumentType))
        {
            return Result<Guid>.Failure(400, $"The document type '{dto.DocumentType}' is not required for your application.");
        }

        // PHASE 2.5: Pre-Submission Overwrite Check (Initial Read — advisory only, not the source of truth)
        var existingDocument = await _dbContext.ApplicantDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.ApplicationId == dto.ApplicationId && d.DocumentType == dto.DocumentType, cancellationToken);

        if (existingDocument is not null && existingDocument.Status != DocumentStatus.Uploaded)
        {
            return Result<Guid>.Failure(400, $"You cannot overwrite this {dto.DocumentType} because it has already been submitted and is currently {existingDocument.Status}.");
        }

        // PHASE 3: Physical File Save (the slow I/O gap where status could change)
        string newStorageKey;
        try
        {
            newStorageKey = await _fileStorageService.SaveFileAsync(dto.File, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save physical file to disk for Application {ApplicationId}", dto.ApplicationId);
            return Result<Guid>.Failure(500, "An error occurred while securely storing the file. Please try again.");
        }

        var now = DateTimeOffset.UtcNow;
        string? oldStorageKeyToDelete = null;
        Guid finalId;

        try
        {
            if (existingDocument is null)
            {
                // New upload — no prior row, safe to insert.
                var newDocument = new ApplicantDocument
                {
                    ApplicationId = dto.ApplicationId,
                    DocumentType = dto.DocumentType,
                    StorageKey = newStorageKey,
                    OriginalFileName = dto.File.FileName,
                    Status = DocumentStatus.Uploaded,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _dbContext.ApplicantDocuments.Add(newDocument);
                await _dbContext.SaveChangesAsync(cancellationToken);
                finalId = newDocument.Id;
            }
            else
            {
                // Overwrite — atomic conditional UPDATE. Re-checks Status = Uploaded in the
                // same round-trip as the write, so a status flip that happened during the
                // SaveFileAsync I/O gap is caught here instead of being silently clobbered.
                oldStorageKeyToDelete = existingDocument.StorageKey;

                var rowsAffected = await _dbContext.ApplicantDocuments
                    .Where(d => d.Id == existingDocument.Id && d.Status == DocumentStatus.Uploaded)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(d => d.StorageKey, newStorageKey)
                        .SetProperty(d => d.OriginalFileName, dto.File.FileName)
                        .SetProperty(d => d.UpdatedAt, now),
                        cancellationToken);

                if (rowsAffected == 0)
                {
                    // Status changed between our read and this write (e.g. verification
                    // picked it up). The new physical file is now orphaned — clean it up.
                    _logger.LogWarning(
                        "Overwrite conflict: document {DocId} status changed before update could apply for Application {ApplicationId}.",
                        existingDocument.Id,
                        dto.ApplicationId);

                    await SafeDeleteOrphanFileAsync(newStorageKey);
                    return Result<Guid>.Failure(409, "This document's status changed while your upload was in progress. Please refresh and try again.");
                }

                finalId = existingDocument.Id;
            }
        }
        catch (DbUpdateException ex) // e.g. unique constraint violation on concurrent first-time insert
        {
            _logger.LogWarning(ex, "Concurrency conflict or duplicate insert detected for Application {ApplicationId}. Rolling back file {StorageKey}", dto.ApplicationId, newStorageKey);

            await SafeDeleteOrphanFileAsync(newStorageKey);
            return Result<Guid>.Failure(409, "A conflicting update occurred while processing your document. Please refresh and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database save failed for Application {ApplicationId}. Rolling back file {StorageKey}", dto.ApplicationId, newStorageKey);

            await SafeDeleteOrphanFileAsync(newStorageKey);
            return Result<Guid>.Failure(500, "Failed to save document metadata. The upload was rolled back.");
        }

        // PHASE 6: Post-Transaction Cleanup (delete the old file only after a successful overwrite)
        if (oldStorageKeyToDelete is not null)
        {
            await SafeDeleteOrphanFileAsync(oldStorageKeyToDelete, isCleanup: true);
        }

        return Result<Guid>.Success(finalId);
    }


    // Helper Method

    private async Task SafeDeleteOrphanFileAsync(string storageKey, bool isCleanup = false)
    {
        try
        {
            await _fileStorageService.DeleteAsync(storageKey, CancellationToken.None);
        }
        catch (Exception ex)
        {
            if (isCleanup)
                _logger.LogError(ex, "Failed to clean up old physical file {OldStorageKey} after successful overwrite.", storageKey);
            else
                _logger.LogError(ex, "Failed to delete orphan file {StorageKey} during rollback.", storageKey);
        }
    }


    public async Task<Result<IEnumerable<ApplicantDocumentDto>>> GetDocumentsByApplicationIdAsync(
    Guid applicationId,
    CancellationToken cancellationToken = default)
    {
        // 1. Fetch the user ID directly from your injected current user service
        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<IEnumerable<ApplicantDocumentDto>>.Failure(401, "User is not authenticated.");
        }

        // 2. Validate authorization using Halim's contract
        bool hasAccess = await _applicationAccessReader.CanAccessDocumentsAsync(
            applicationId,
            userId,
            cancellationToken);

        if (!hasAccess)
        {
            _logger.LogWarning(
                "Security: User {UserId} attempted to view documents for application {ApplicationId} without authorization.",
                userId,
                applicationId);

            return Result<IEnumerable<ApplicantDocumentDto>>.Failure(403, "You do not have permission to view documents for this application.");
        }

        // 3. Query the database using highly optimized EF Core projection
        var documents = await _dbContext.ApplicantDocuments
            .AsNoTracking()
            .Where(d => d.ApplicationId == applicationId)
            .Select(d => new ApplicantDocumentDto(
                d.Id,
                d.DocumentType,
                d.OriginalFileName,
                d.Status,
                d.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result<IEnumerable<ApplicantDocumentDto>>.Success(documents);
    }



    public async Task<Result<FileDownloadDto>> DownloadDocumentAsync(
    Guid documentId,
    CancellationToken cancellationToken = default)
    {
        // 1. Authenticate
        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<FileDownloadDto>.Failure(401, "User is not authenticated.");
        }

        // 2. Fetch the document metadata from the database
        var document = await _dbContext.ApplicantDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

        if (document == null)
        {
            return Result<FileDownloadDto>.Failure(404, "Document not found.");
        }

        // 3. Prevent IDOR: Check if the current user owns the parent application
        bool hasAccess = await _applicationAccessReader.CanAccessDocumentsAsync(
            document.ApplicationId,
            userId,
            cancellationToken);

        if (!hasAccess)
        {
            _logger.LogWarning(
                "Security IDOR attempt: User {UserId} tried to download document {DocumentId} belonging to application {ApplicationId}.",
                userId, documentId, document.ApplicationId);

            return Result<FileDownloadDto>.Failure(403, "You do not have permission to download this document.");
        }

        // 4. Retrieve the physical file stream
        var stream = await _fileStorageService.OpenReadAsync(document.StorageKey, cancellationToken);

        // Note: We don't check for null stream here because if it fails, 
        // IFileStorageService will throw an exception which your global ExceptionHandler will catch (500).

        var dto = new FileDownloadDto(stream, document.OriginalFileName);
        return Result<FileDownloadDto>.Success(dto);
    }






    public async Task<Result<IEnumerable<ApplicantDocumentDto>>> GetDocumentsForApplicantAsync(string userId, CancellationToken cancellationToken)
    {
        // 1. Ask Halim's module for this user's current ApplicationId
        var applicationId = await _applicationAccessReader.GetApplicationIdForUserAsync(userId, cancellationToken);

        // Khorshid explicitly requested: "returns their documents, or an empty list if they have none - please don't throw"
        if (applicationId == Guid.Empty)
        {
            return Result<IEnumerable<ApplicantDocumentDto>>.Success(Enumerable.Empty<ApplicantDocumentDto>());
        }

        // 2. Fetch and project directly to the DTO in one highly optimized query
        var documents = await _dbContext.ApplicantDocuments
            .AsNoTracking()
            .Where(d => d.ApplicationId == applicationId)
            .Select(d => new ApplicantDocumentDto(
                d.Id,
                d.DocumentType,
                d.OriginalFileName,
                d.Status,
                d.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result<IEnumerable<ApplicantDocumentDto>>.Success(documents);
    }


    public async Task<Result<RequiredDocumentsDto>> GetRequiredDocumentTypesAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        // 1. Authenticate
        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<RequiredDocumentsDto>.Failure(401, "User is not authenticated.");
        }

        // 2. Authorize
        var hasAccess = await _applicationAccessReader.CanAccessDocumentsAsync(
            applicationId,
            userId,
            cancellationToken);

        if (!hasAccess)
        {
            _logger.LogWarning(
                "Security: User {UserId} attempted to view required documents for application {ApplicationId} without authorization.",
                userId,
                applicationId);

            return Result<RequiredDocumentsDto>.Failure(403, "You do not have permission to view requirements for this application.");
        }

        // 3. Resolve program context (same lookup UploadDocumentAsync already does)
        var programId = await _applicationAccessReader.GetProgramIdAsync(applicationId, cancellationToken);
        if (programId == Guid.Empty)
        {
            return Result<RequiredDocumentsDto>.Failure(404, "Application program context could not be found.");
        }

        // 4. Resolve applicant gender
        var applicant = await _profileService.GetAsync(userId, cancellationToken);
        var applicantGender = GenderMapper.ToGender(applicant?.Value?.Gender) ?? Gender.None;

        // 5. Resolve the effective requirement set for this program + gender
        var requirementSet = await _programRequirementReader.ResolveAsync(programId, applicantGender, cancellationToken);
        if (requirementSet is null)
        {
            return Result<RequiredDocumentsDto>.Failure(400, "Could not determine document requirements for this program. Please contact support.");
        }

        return Result<RequiredDocumentsDto>.Success(new RequiredDocumentsDto(requirementSet.DocumentTypes));
    }

}