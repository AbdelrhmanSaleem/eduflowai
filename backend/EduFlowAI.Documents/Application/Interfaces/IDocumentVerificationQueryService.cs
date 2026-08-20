using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.Documents.Application.DTOs;
using EduFlowAI.Shared.Kernel.Common;

namespace EduFlowAI.Documents.Application.Interfaces;

public interface IDocumentVerificationQueryService
{
    Task<Result<DocumentVerificationResponseDto>> GetVerificationAsync(
        Guid documentId,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyCollection<DocumentVerificationResponseDto>>> GetVerificationAttemptsAsync(
        Guid documentId,
        CancellationToken cancellationToken);
}
