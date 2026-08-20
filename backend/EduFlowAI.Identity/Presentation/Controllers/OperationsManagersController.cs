using EduFlowAI.Identity.Application.DTOs;
using EduFlowAI.Identity.Application.Interfaces;
using EduFlowAI.Identity.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduFlowAI.Identity.Presentation.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.SuperAdmin)]
[Route("api/admin/operations-managers")]
public sealed class OperationsManagersController(
    IOperationsManagerService operationsManagerService)
    : IdentityControllerBase
{
    [HttpPost]
    [ProducesResponseType<OperationsManagerResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OperationsManagerResponse>> Create(
        [FromBody] CreateOperationsManagerRequest request)
    {
        var result = await operationsManagerService.CreateAsync(request);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : ToErrorResult(result.Failure!);
    }

    [HttpPatch("{userId}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(string userId)
    {
        var result = await operationsManagerService.DeactivateAsync(userId);
        return result.IsSuccess
            ? NoContent()
            : ToErrorResult(result.Failure!);
    }
}
