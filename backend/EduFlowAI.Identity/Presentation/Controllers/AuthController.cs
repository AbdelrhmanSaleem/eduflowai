using EduFlowAI.Identity.Application.DTOs;
using EduFlowAI.Identity.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EduFlowAI.Identity.Presentation.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("Authentication")]
[Route("api/auth")]
public sealed class AuthController(
    IAuthService authService) : IdentityControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(
            request,
            cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : ToErrorResult(result.Failure!);
    }

    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status423Locked)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(
            request,
            cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : ToErrorResult(result.Failure!);
    }

    [HttpPost("confirm-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request)
    {
        var result = await authService.ConfirmEmailAsync(request);
        return result.IsSuccess
            ? NoContent()
            : ToErrorResult(result.Failure!);
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType<ForgotPasswordResponse>(
        StatusCodes.Status202Accepted)]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.ForgotPasswordAsync(
            request,
            cancellationToken);

        return result.IsSuccess
            ? Accepted(result.Value)
            : ToErrorResult(result.Failure!);
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request)
    {
        var result = await authService.ResetPasswordAsync(request);
        return result.IsSuccess
            ? NoContent()
            : ToErrorResult(result.Failure!);
    }
}
