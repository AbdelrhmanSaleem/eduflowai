using EduFlowAI.Documents.Application.DTOs;
using EduFlowAI.Shared.Kernel.Common;
using EduFlowAI.Shared.Kernel.Common.Pagination;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents.Application.Interfaces
{
    public interface IDocumentHumanReviewService
    {
        Task<Result<PaginatedResult<HumanReviewDto>>> GetAllDocumentsReviewsAsync(QueryParameters queryParameters);
        Task<Result<DocumentReviewDto>> GetDocumentReviewAsync(Guid documentId);
        Task<Result<DocumentFileDto>> GetDocumentFileAsync(Guid documentId);
        Task<Result<string>> ApproveReviewAsync(Guid documentId, string approvedById, CancellationToken cancellationToken);
        Task<Result<string>> RejectReviewAsync(Guid documentId, string reason, CancellationToken cancellationToken);
    }
}
