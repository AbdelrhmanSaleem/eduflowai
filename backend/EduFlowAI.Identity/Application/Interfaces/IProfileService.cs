using EduFlowAI.Identity.Application.DTOs;

namespace EduFlowAI.Identity.Application.Interfaces;

public interface IProfileService    
{
    Task<IdentityOperationResult<ProfileResponse>> GetAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IdentityOperationResult<ProfileResponse>> UpdateAsync(
        string userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default);
}
