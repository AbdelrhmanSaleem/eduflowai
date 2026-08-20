using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.Admission.Domain.Enums;
using EduFlowAI.Documents.Application.DbContextAbstraction;
using EduFlowAI.Documents.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents.Application.Services
{
    internal static class DocumentPackageReviewResolver
    {
        public static async Task<DocumentReviewResultDto?> ResolveAsync(
            IDocumentDbContext dbContext,
            Guid applicationId,
            Guid changedDocumentId,
            DocumentStatus changedStatus,
            CancellationToken cancellationToken)
        {

            var statuses = await dbContext.ApplicantDocuments
                .AsNoTracking()
                .Where(d => d.ApplicationId == applicationId && d.Id != changedDocumentId)
                .Select(d => d.Status)
                .ToListAsync(cancellationToken);

            statuses.Add(changedStatus);

            // wait until every document finishes verification
            if (statuses.Any(status =>
                    status is DocumentStatus.Uploaded or DocumentStatus.Verifying))
            {
                return null;
            }

            if (statuses.Any(status => status == DocumentStatus.NeedsHumanReview))
            {
                return new DocumentReviewResultDto
                {
                    ReviewerType = ReviewerType.AI,
                    IsAccepted = false,
                    IsAgentUncertain = true
                };
            }

            if (statuses.All(status => status == DocumentStatus.Approved))
            {
                return new DocumentReviewResultDto
                {
                    ReviewerType = ReviewerType.AI,
                    IsAccepted = true,
                    IsAgentUncertain = false
                };
            }

            return null;
        }
    }
}
