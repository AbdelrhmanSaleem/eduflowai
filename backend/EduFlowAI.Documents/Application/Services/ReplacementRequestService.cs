using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Communication.Application.Interfaces;
using EduFlowAI.Communication.Application.Interfaces.Emails;
using EduFlowAI.Communication.Domain.Enums;
using EduFlowAI.Documents.Application.DbContextAbstraction;
using EduFlowAI.Documents.Application.DTOs;
using EduFlowAI.Documents.Application.Emails;
using EduFlowAI.Documents.Application.Interfaces;
using EduFlowAI.Documents.Application.Validators;
using EduFlowAI.Documents.Domain.Entities;
using EduFlowAI.Documents.Domain.Enums;
using EduFlowAI.Identity.Application.Interfaces;
using EduFlowAI.Identity.Domain.Entities;
using EduFlowAI.Shared.Kernel.Common;
using EduFlowAI.Shared.Kernel.Common.Pagination;
using EduFlowAI.Shared.Kernel.Messaging;
using EduFlowAI.Shared.Messaging.Contracts.Documents.V1;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents.Application.Services
{
    public class ReplacementRequestService(IDocumentDbContext dbContext, IUserContactInfoReader userContactInfoReader, 
        IAdmissionDbContext admissionDbContext, INotificationService notificationService, IEmailDispatcher emailDispatcher,
        IOutboxPublisher outboxPublisher, IFileStorageService fileStorageService, IValidator<UploadDocumentDto> uploadValidator,
        IApplicationAcessReader applicationAccessReader, IStatusTransitionCoordinator statusTransitionCoordinator) 
        : IReplacementRequestService
    {
        public async Task<Result<string>> SendReplacementRequest(ReplacementRequestDto request, string requestedById, CancellationToken cancellationToken)
        {
            try
            {
                var doc = await dbContext.ApplicantDocuments.FirstOrDefaultAsync(d => d.Id == request.DocumentId);
                if (doc == null)
                {
                    return Result<string>.Failure(404, "Document not found");
                }

                var replacementRequest = new Domain.Entities.DocumentReplacementRequest
                {
                    Id = Guid.NewGuid(),
                    DocumentId = request.DocumentId,
                    RequestedByUserId = requestedById,
                    Reason = request.Reason,
                    Status = ReplacementRequestStatus.Open,
                    RequestedAt = DateTimeOffset.UtcNow
                };

                await dbContext.DocumentReplacementRequests.AddAsync(replacementRequest);
                doc.Status = DocumentStatus.ReplacementRequested;

                var applicant = await GetApplicant(doc.ApplicationId);
                // send notification
                await notificationService.NotifyAsync(new CreateNotificationRequest(
                    UserId: applicant.UserId,
                    ApplicationId: doc.ApplicationId,
                    Type: NotificationType.DocumentReplacementRequested,
                    Message: $"Your {doc.DocumentType.ToString()} needs to be replaced!"),
                    cancellationToken);
                // send email
                await emailDispatcher.RequestEmailAsync(
                    EmailType.DocumentReplacementRequested,
                    applicant.Email!,
                    new DocumentReplacementRequestEmailData(doc.DocumentType.ToString(), request.Reason),
                    cancellationToken);

                // save transaction
                await outboxPublisher.SaveChangesAndFlushMessagesAsync(cancellationToken);

                return Result<string>.Success("Replacement request sent successfully");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(500, $"An error occurred while sending the replacement request: {ex.Message}");
            }
        }

        public async Task<Result<PaginatedResult<ReplacementDto>>> GetAllReplacemntRequestsAsync(string ApplicantId, QueryParameters queryParameters)
        {
            try
            {
                var apps = await admissionDbContext.Applications.AsNoTracking()
                    .Where(a => a.ApplicantUserId == ApplicantId)
                    .Select(a => a.Id).ToListAsync();

                if (apps.Count == 0)
                {
                    return Result<PaginatedResult<ReplacementDto>>.Failure(404, "User does not have any applications");
                }

                var query = dbContext.DocumentReplacementRequests.AsNoTracking()
                    .Include(r => r.Document)
                    .Where(r=>apps.Contains(r.Document.ApplicationId))
                    .Select(r => new
                    {
                        r.Id,
                        r.DocumentId,
                        r.Reason,
                        r.Status,
                        r.RequestedAt,
                        DocumentType = r.Document.DocumentType.ToString()
                    });

                if(!string.IsNullOrEmpty(queryParameters.Search))
                {
                    query = query.Where(r => r.Reason.Contains(queryParameters.Search));
                }
                if(!string.IsNullOrEmpty(queryParameters.Status)) {
                    query = query.Where(r => r.Status.ToString().Contains(queryParameters.Status));
                }
                else
                {
                    query = query.OrderBy(r => r.RequestedAt);
                }

                var totalCount = await query.CountAsync();
                var items = await query
                    .Skip((queryParameters.Page - 1) * queryParameters.PageSize)
                    .Take(queryParameters.PageSize)
                    .Select(r => new ReplacementDto(
                        r.Id,
                        r.DocumentId,
                        r.DocumentType,
                        r.Reason,
                        r.Status,
                        r.RequestedAt
                    ))
                    .ToListAsync();
                var paginatedResult = new PaginatedResult<ReplacementDto>
                {
                    Data = items,
                    PageSize = queryParameters.PageSize,
                    CurrentPage = queryParameters.Page,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)queryParameters.PageSize),
                    TotalCount = totalCount
                };

                return Result<PaginatedResult<ReplacementDto>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                return Result<PaginatedResult<ReplacementDto>>.Failure(500, $"An error occurred while retrieving replacement requests: {ex.Message}");
            }
        }

        public async Task<Result<ReplacementDto>> GetReplacemntRequestAsync(Guid requestId)
        {
            try
            {
                var req = await dbContext.DocumentReplacementRequests.AsNoTracking()
                    .Include(r => r.Document)
                    .Where(r => r.Id == requestId)
                    .Select(r => new ReplacementDto(
                        r.Id,
                        r.DocumentId,
                        r.Document.DocumentType.ToString(),
                        r.Reason,
                        r.Status,
                        r.RequestedAt
                    )).FirstOrDefaultAsync();

                if (req is null)
                {
                    return Result<ReplacementDto>.Failure(404, "Request not found");
                }

                return Result<ReplacementDto>.Success(req);
            }
            catch (Exception ex)
            {
                return Result<ReplacementDto>.Failure(500, $"An error occurred while retrieving the replacement request: {ex.Message}");
            }
        }

        public async Task<Result<Guid>> UploadReplacementAsync(Guid requestId, IFormFile file,
            string applicantUserId, CancellationToken cancellationToken)
        {
            var replacementRequest = await dbContext.DocumentReplacementRequests
                .Include(request => request.Document)
                .SingleOrDefaultAsync(request => request.Id == requestId, cancellationToken);

            if (replacementRequest is null)
            {
                return Result<Guid>.Failure(404, "Replacement request not found.");
            }

            if (replacementRequest.Status != ReplacementRequestStatus.Open)
            {
                return Result<Guid>.Failure(409, "This replacement request has already been fulfilled.");
            }

            var document = replacementRequest.Document;

            var hasAccess = await applicationAccessReader.CanAccessDocumentsAsync(document.ApplicationId,
                    applicantUserId, cancellationToken);

            if (!hasAccess)
            {
                return Result<Guid>.Failure(403, "You do not have permission to replace this document.");
            }

            var uploadDto = new UploadDocumentDto(document.ApplicationId, document.DocumentType, file);

            var validationResult =
                await uploadValidator.ValidateAsync(uploadDto, cancellationToken);

            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(error => error.ErrorMessage));
                return Result<Guid>.Failure(400, errors);
            }

            string newStorageKey;

            try
            {
                newStorageKey = await fileStorageService.SaveFileAsync(file, cancellationToken);
            }
            catch (ArgumentException exception)
            {
                return Result<Guid>.Failure(400, exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                return Result<Guid>.Failure(400, exception.Message);
            }

            var now = DateTimeOffset.UtcNow;

            var previousVersionCount = await dbContext.ApplicantDocumentVersions
                    .CountAsync(version => version.DocumentId == document.Id, cancellationToken);

            var previousVersion = new ApplicantDocumentVersion
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                VersionNumber = previousVersionCount + 1,
                StorageKey = document.StorageKey,
                OriginalFileName = document.OriginalFileName,
                Status = document.Status,
                VerificationDetailsJson = document.VerificationDetailsJson,
                VerifiedAt = document.VerifiedAt,
                CreatedAt = now
            };

            dbContext.ApplicantDocumentVersions.Add(previousVersion);

            document.StorageKey = newStorageKey;
            document.OriginalFileName = file.FileName;
            document.Status = DocumentStatus.Verifying;
            document.VerificationDetailsJson = null;
            document.VerifiedAt = null;
            document.UpdatedAt = now;

            replacementRequest.Status = ReplacementRequestStatus.Fulfilled;

            replacementRequest.FulfilledAt = now;

            var transition = await statusTransitionCoordinator.MarkDocumentVerificationStartedAsync(document.ApplicationId);

            if (!transition.IsSuccess)
            {
                return Result<Guid>.Failure(409, transition.ErrorMessage);
            }

            var verificationMessage = new VerifyApplicantDocumentV1(
                    MessageId: Guid.NewGuid(),
                    CorrelationId: document.ApplicationId,
                    CausationId: null,
                    DocumentId: document.Id,
                    ApplicationId: document.ApplicationId,
                    DocumentType: document.DocumentType.ToString(),
                    SourceStorageKey: newStorageKey,
                    OriginalFileName: file.FileName,
                    OccurredAtUtc: now);

            await outboxPublisher.PublishAsync(verificationMessage);

            await outboxPublisher.SaveChangesAndFlushMessagesAsync(cancellationToken);

            return Result<Guid>.Success(document.Id, 200, "Replacement uploaded successfully and verification has started.");
        }

        // helpers
        private async Task<UserContactInfo> GetApplicant(Guid applicationId)
        {
            var id = await admissionDbContext.Applications
                .Where(a => a.Id == applicationId)
                .Select(a => a.ApplicantUserId)
                .FirstOrDefaultAsync();

            var user = await userContactInfoReader.GetContactInfoAsync(id!, default);

            return user;
        }
    }
}
