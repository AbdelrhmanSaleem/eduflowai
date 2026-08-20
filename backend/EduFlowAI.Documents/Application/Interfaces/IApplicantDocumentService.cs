using System;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.Shared.Kernel.Common;
using EduFlowAI.Documents.Application.DTOs;

namespace EduFlowAI.Documents.Application.Interfaces
{
    public interface IApplicantDocumentService
    {
        Task<Result<Guid>> UploadDocumentAsync(UploadDocumentDto dto, CancellationToken cancellationToken);

        Task<Result<IEnumerable<ApplicantDocumentDto>>> GetDocumentsByApplicationIdAsync(Guid applicationId, CancellationToken cancellationToken = default);

        Task<Result<FileDownloadDto>> DownloadDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

        Task<Result<IEnumerable<ApplicantDocumentDto>>> GetDocumentsForApplicantAsync(string userId, CancellationToken cancellationToken);


        Task<Result<RequiredDocumentsDto>> GetRequiredDocumentTypesAsync(Guid applicationId, CancellationToken cancellationToken = default);
    }
}