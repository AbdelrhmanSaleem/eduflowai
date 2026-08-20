using System;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.Shared.Kernel.Common;

namespace EduFlowAI.Documents.Application.Services;

public interface IDocumentSubmissionService
{
    Task<Result<int>> SubmitPackageAsync(
        Guid applicationId,
        CancellationToken cancellationToken);
}