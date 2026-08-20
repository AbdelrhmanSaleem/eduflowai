using System.Security.Claims;
using EduFlowAI.AI.Presentation.DTOs;
using EduFlowAI.AI.Presentation.Interfaces;
using EduFlowAI.AI.Presentation.Validators;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduFlowAI.Api.Controllers;

[ApiController]
[Route("api/assistant/message")]
public sealed class AssistantController(
    IAssistantMessageService assistantMessageService)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<AssistantResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssistantResponse>> SendMessage(
        [FromBody] AssistantMessageRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors =
            AssistantMessageRequestValidator.Validate(request);

        if (validationErrors.Count > 0)
        {
            foreach (var error in validationErrors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return ValidationProblem(ModelState);
        }
        var userId = Guid.Empty;

        var isAuthenticated =
            User.Identity?.IsAuthenticated == true &&
            Guid.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out userId);

        var response = await assistantMessageService.HandleAsync(
            request,
            userId,
            isAuthenticated,
            cancellationToken);

        return Ok(response);
    }
}