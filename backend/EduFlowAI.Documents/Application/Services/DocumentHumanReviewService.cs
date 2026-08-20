using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Enums;
using EduFlowAI.Communication.Application.Interfaces;
using EduFlowAI.Communication.Domain.Enums;
using EduFlowAI.Documents.Application.DbContextAbstraction;
using EduFlowAI.Documents.Application.DTOs;
using EduFlowAI.Documents.Application.Interfaces;
using EduFlowAI.Documents.Domain.Enums;
using EduFlowAI.Identity.Application.DbContextAbstraction;
using EduFlowAI.Identity.Domain.Entities;
using EduFlowAI.Shared.Kernel.Common;
using EduFlowAI.Shared.Kernel.Common.Pagination;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EduFlowAI.Documents.Application.Services
{
    public class DocumentHumanReviewService(IDocumentDbContext dbContext, IIdentityDbContext identityDbContext,
        IAdmissionDbContext admissionDbContext, IFileStorageService fileStorageService, 
        INotificationService notificationService, IStatusTransitionCoordinator statusTransition)
        : IDocumentHumanReviewService
    {
        public async Task<Result<PaginatedResult<HumanReviewDto>>> GetAllDocumentsReviewsAsync(QueryParameters queryParameters)
        {
            try
            {
                var query = dbContext.ApplicantDocuments.AsNoTracking()
                    .Where(d => d.Status == DocumentStatus.NeedsHumanReview || d.Status == DocumentStatus.ReplacementRequested)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(queryParameters.Search)) {
                    query = query.Where(d => d.OriginalFileName.Contains(queryParameters.Search));
                }
                if (!string.IsNullOrEmpty(queryParameters.Status))
                {
                    query = query.Where(d => d.Status.ToString().Contains(queryParameters.Status));
                }
                if (!string.IsNullOrEmpty(queryParameters.Type))
                {
                    query = query.Where(d => d.DocumentType.ToString().Contains(queryParameters.Type!));
                }
                else
                {
                    query = query.OrderBy(d => d.CreatedAt);
                }

                var totalCount = await query.CountAsync();

                var projectedDocs = await query.Skip((queryParameters.Page - 1) * queryParameters.PageSize)
                    .Take(queryParameters.PageSize)
                    .Select(d => new {
                        d.Id,
                        d.DocumentType,
                        d.StorageKey,
                        d.OriginalFileName,
                        d.Status,
                        d.VerificationDetailsJson,
                        d.ApplicationId
                    })
                    .ToListAsync();

                var applicationIds = projectedDocs.Select(d => d.ApplicationId).Distinct().ToList();
                var appUserMap = await admissionDbContext.Applications
                    .Where(a => applicationIds.Contains(a.Id))
                    .Select(a => new { a.Id, a.ApplicantUserId })
                    .ToListAsync();

                var userIds = appUserMap.Select(a => a.ApplicantUserId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
                var profileMap = await identityDbContext.ApplicantProfiles
                    .Where(p => userIds.Contains(p.UserId))
                    .Select(p => new { p.UserId, p.FullNameEn })
                    .ToListAsync();

                var appIdToUserId = appUserMap.ToDictionary(x => x.Id, x => x.ApplicantUserId);
                var userIdToName = profileMap.ToDictionary(x => x.UserId, x => x.FullNameEn);

                var docReviews = projectedDocs.Select(d => {
                    var applicantName = string.Empty;
                    if (appIdToUserId.TryGetValue(d.ApplicationId, out var userId) && !string.IsNullOrEmpty(userId))
                    {
                        userIdToName.TryGetValue(userId, out applicantName);
                    }

                    return new HumanReviewDto(
                        DocumentId: d.Id,
                        DocumentType: d.DocumentType,
                        ApplicantName: applicantName ?? string.Empty,
                        StorageKey: d.StorageKey,
                        OriginalFileName: d.OriginalFileName,
                        Status: d.Status,
                        VerificationDetailsJson: d.VerificationDetailsJson
                    );
                }).ToList();

                var data = new PaginatedResult<HumanReviewDto>
                {
                    Data = docReviews,
                    CurrentPage = queryParameters.Page,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)queryParameters.PageSize),
                    PageSize = queryParameters.PageSize,
                    TotalCount = totalCount
                };
                return Result<PaginatedResult<HumanReviewDto>>.Success(data);
            }
            catch (Exception ex)
            {
                return Result<PaginatedResult<HumanReviewDto>>.Failure(500, $"An error occurred while retrieving document reviews: {ex.Message}");
            }
        }

        public async Task<Result<DocumentReviewDto>> GetDocumentReviewAsync(Guid documentId)
        {
            try
            {
                var doc = await dbContext.ApplicantDocuments.AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == documentId);
                if (doc == null)
                {
                    return Result<DocumentReviewDto>.Failure(404, "Document not found");
                }

                var dto = new DocumentReviewDto
                (
                    DocumentId: doc.Id,
                    ApplicationId: doc.ApplicationId,
                    ApplicantId: await GetApplicantId(doc.ApplicationId),
                    DocumentType: doc.DocumentType,
                    StorageKey: doc.StorageKey,
                    OriginalFileName: doc.OriginalFileName,
                    Status: doc.Status,
                    VerificationDetailsJson: doc.VerificationDetailsJson
                );

                return Result<DocumentReviewDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<DocumentReviewDto>.Failure(500, $"An error occurred while retrieving the document review: {ex.Message}");
            }
        }

        public async Task<Result<string>> ApproveReviewAsync(Guid documentId, string approvedById, CancellationToken cancellationToken)
        {
            try
            {
                var doc = await dbContext.ApplicantDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
                if (doc == null)
                {
                    return Result<string>.Failure(404, "Document not found");
                }

                doc.Status = DocumentStatus.Approved;
                doc.UpdatedAt = DateTime.UtcNow;

                var reviewResult = await DocumentPackageReviewResolver.ResolveAsync(
                    dbContext,
                    doc.ApplicationId,
                    doc.Id,
                    DocumentStatus.Approved,
                    cancellationToken);

                await dbContext.SaveChangesAsync(cancellationToken);

                if (reviewResult is not null)
                {
                    var transition = await statusTransition.ProcessDocumentReviewAsync(doc.ApplicationId, reviewResult);

                    if (!transition.IsSuccess)
                    {
                        throw new InvalidOperationException(transition.ErrorMessage);
                    }
                }

                var applicantId = await GetApplicantId(doc.ApplicationId);
                // send notification
                await notificationService.NotifyAsync(new CreateNotificationRequest(
                    UserId: applicantId,
                    ApplicationId: doc.ApplicationId,
                    Type: NotificationType.DocumentApproved,
                    Message: $"Your {doc.DocumentType.ToString()} approved successfully."),
                    cancellationToken);

                return Result<string>.Success("Document approved successfully");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(500, $"An error occurred while approving the document review: {ex.Message}");
            }
        }

        public async Task<Result<string>> RejectReviewAsync(Guid documentId, string reason, CancellationToken cancellationToken)
        {
            try
            {
                var doc = await dbContext.ApplicantDocuments.FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);
                if (doc == null)
                {
                    return Result<string>.Failure(404, "Document not found");
                }

                doc.Status = DocumentStatus.Rejected;
                doc.RejectionReason = reason;
                doc.UpdatedAt = DateTimeOffset.UtcNow;

                await dbContext.SaveChangesAsync(cancellationToken);

                var reviewResult = new DocumentReviewResultDto
                {
                    ReviewerType = ReviewerType.Human,
                    IsAccepted = false,
                    IsAgentUncertain = false,
                    RejectionReason = reason
                };
                var transition = await statusTransition.ProcessDocumentReviewAsync(doc.ApplicationId, reviewResult);
                if (!transition.IsSuccess)
                {
                    throw new InvalidOperationException(transition.ErrorMessage);
                }

                var applicantId = await GetApplicantId(doc.ApplicationId);
                // send notification
                await notificationService.NotifyAsync(new CreateNotificationRequest(
                    UserId: applicantId,
                    ApplicationId: doc.ApplicationId,
                    Type: NotificationType.DocumentRejected,
                    Message: $"Unfortunately, your {doc.DocumentType.ToString()} rejected, see details."),
                    cancellationToken);

                return Result<string>.Success("Document rejected successfully");
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(500, $"An error occurred while rejecting the document review: {ex.Message}");
            }
        }

        public async Task<Result<DocumentFileDto>> GetDocumentFileAsync(Guid documentId)
        {
            try
            {
                var document = await dbContext.ApplicantDocuments.AsNoTracking()
                    .SingleOrDefaultAsync(d => d.Id == documentId);

                if (document is null)
                {
                    return Result<DocumentFileDto>.Failure(404, "Document not found");
                }

                if (string.IsNullOrWhiteSpace(document.StorageKey))
                {
                    return Result<DocumentFileDto>.Failure(400, "The document does not have a stored file.");
                }

                var stream = await fileStorageService.OpenReadAsync(document.StorageKey);

                var contentType = GetContentType(document.StorageKey);

                var fileName = string.IsNullOrWhiteSpace(
                    document.OriginalFileName)
                        ? Path.GetFileName(document.StorageKey)
                        : document.OriginalFileName;

                return Result<DocumentFileDto>.Success(new DocumentFileDto
                (
                    Content: stream,
                    ContentType: contentType,
                    FileName: fileName
                ));
            }
            catch (Exception ex)
            {
                return Result<DocumentFileDto>.Failure(500, $"An error occurred while retrieving the document file: {ex.Message}");
            }
        }

        // helpers
        #region Helpers
        private async Task<string> GetApplicantId(Guid applicationId)
        {
            var id = await admissionDbContext.Applications
                .Where(a => a.Id == applicationId)
                .Select(a => a.ApplicantUserId)
                .FirstOrDefaultAsync();
            return id ?? string.Empty;
        }

        private static string GetContentType(string storageKey)
        {
            var extension = Path
                .GetExtension(storageKey)
                .ToLowerInvariant();

            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
        #endregion
    }
}
