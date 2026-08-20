
using EduFlowAI.Communication.Application.Interfaces;
using EduFlowAI.Shared.Kernel.Common.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace EduFlowAI.Communication.Presentation.Controllers;

[Authorize]
[ApiController]
[Route("api/communication/notifications")]
public sealed class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllNotificationsByUserId([FromQuery] QueryParameters queryParameters, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var result = await _notificationService.GetAllNotificationsByUserIdAsync(userId!, queryParameters, cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, result.Message);
        }
        return StatusCode(result.StatusCode, result.Data);
    }

    [HttpPatch("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid notificationId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var result = await _notificationService.MarkAsReadAsync(
            userId,
            notificationId,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, result.Message);
        }
        return NoContent();
    }

    [HttpPatch("read-all")]
    [ProducesResponseType<MarkAllReadResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var result = await _notificationService.MarkAllAsReadAsync(
            userId,
            cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, result.Message);
        }
        return Ok(new MarkAllReadResponse(result.Data));
    }
}

public sealed record MarkAllReadResponse(int UpdatedCount);
