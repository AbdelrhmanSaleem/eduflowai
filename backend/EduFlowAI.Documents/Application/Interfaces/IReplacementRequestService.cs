using EduFlowAI.Documents.Application.DTOs;
using EduFlowAI.Shared.Kernel.Common;
using EduFlowAI.Shared.Kernel.Common.Pagination;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents.Application.Interfaces
{
    public interface IReplacementRequestService
    {
        Task<Result<string>> SendReplacementRequest(ReplacementRequestDto request, string requestedById, CancellationToken cancellationToken);
        Task<Result<PaginatedResult<ReplacementDto>>> GetAllReplacemntRequestsAsync(string ApplicantId, QueryParameters queryParameters);
        Task<Result<ReplacementDto>> GetReplacemntRequestAsync(Guid requestId);
        Task<Result<Guid>> UploadReplacementAsync(Guid requestId, IFormFile file, 
            string applicantUserId, CancellationToken cancellationToken);
    }
}
