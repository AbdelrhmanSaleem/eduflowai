using EduFlowAI.Identity.Application.DTOs;

namespace EduFlowAI.Identity.Application.Interfaces;

public interface IOperationsManagerService
{
    Task<IdentityOperationResult<OperationsManagerResponse>> CreateAsync(
        CreateOperationsManagerRequest request);

    Task<IdentityOperationResult<bool>> DeactivateAsync(string userId);
}
