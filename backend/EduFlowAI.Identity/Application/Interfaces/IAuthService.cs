using EduFlowAI.Identity.Application.DTOs;

namespace EduFlowAI.Identity.Application.Interfaces;

public interface IAuthService
{
    Task<IdentityOperationResult<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<IdentityOperationResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<IdentityOperationResult<bool>> ConfirmEmailAsync(
        ConfirmEmailRequest request);

    Task<IdentityOperationResult<ForgotPasswordResponse>> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<IdentityOperationResult<bool>> ResetPasswordAsync(
        ResetPasswordRequest request);
}
