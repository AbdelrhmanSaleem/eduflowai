using System.Security.Claims;
using EduFlowAI.Identity.Application.DTOs;
using EduFlowAI.Identity.Application.Interfaces;
using EduFlowAI.Identity.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduFlowAI.Identity.Presentation.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Applicant)]
[Route("api/profile")]
public sealed class ProfileController(
    IProfileService profileService) : IdentityControllerBase
{
    [HttpGet]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileResponse>> Get(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await profileService.GetAsync(
            userId,
            cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : ToErrorResult(result.Failure!);
    }

    [HttpPut]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProfileResponse>> Put(
        [FromBody] UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await profileService.UpdateAsync(
            userId,
            request,
            cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : ToErrorResult(result.Failure!);
    }

    private bool TryGetUserId(out string userId)
    {
        userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        return !string.IsNullOrWhiteSpace(userId);
    }
}
