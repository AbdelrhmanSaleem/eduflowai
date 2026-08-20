using EduFlowAI.AI.Application.DTOs;
using EduFlowAI.AI.Application.Exceptions;
using EduFlowAI.AI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduFlowAI.Api.Controllers;

[ApiController]
//[Authorize(Roles = "SuperAdmin")]
[Route("api/admin/knowledge-base")]
public sealed class AdminKnowledgeBaseController(
    IKnowledgeIndexingService knowledgeIndexingService)
    : ControllerBase
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<Guid>> Upload(
    [FromForm] UploadKnowledgeBaseDocumentRequest request,
    CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length <= 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid knowledge base file.",
                Detail = "A non-empty file is required.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        var extension = Path
            .GetExtension(request.File.FileName)
            .ToLowerInvariant();

        var allowedExtensions = new HashSet<string>
    {
        ".pdf",
        ".md",
        ".txt",
    };

        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Unsupported knowledge base file.",
                Detail =
                    "Only PDF, Markdown (.md), and text (.txt) files are supported.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        try
        {
            await using var stream =
                request.File.OpenReadStream();

            var documentId =
                await knowledgeIndexingService.AddDocumentAsync(
                    stream,
                    request.File.FileName,
                    request.File.Length,
                    cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                documentId);
        }
        catch (KnowledgeBaseValidationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid knowledge base document.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest,
            });
        }
        catch (KnowledgeBaseTooLargeException exception)
        {
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                new ProblemDetails
                {
                    Title = "Knowledge base document is too large.",
                    Detail = exception.Message,
                    Status =
                        StatusCodes.Status413PayloadTooLarge,
                });
        }
    }

    [HttpPost("text")]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> AddText(
        [FromBody] AddKnowledgeBaseTextRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid knowledge base text.",
                Detail = "Content is required.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        try
        {
            var documentId =
                await knowledgeIndexingService.AddTextAsync(
                    request.Title,
                    request.Content,
                    cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                documentId);
        }
        catch (KnowledgeBaseValidationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid knowledge base text.",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest,
            });
        }
        catch (KnowledgeBaseTooLargeException exception)
        {
            return StatusCode(
                StatusCodes.Status413PayloadTooLarge,
                new ProblemDetails
                {
                    Title = "Knowledge base text is too large.",
                    Detail = exception.Message,
                    Status = StatusCodes.Status413PayloadTooLarge,
                });
        }
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<KnowledgeBaseDocumentDto>>(
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<IReadOnlyList<KnowledgeBaseDocumentDto>>> GetDocuments(
        CancellationToken cancellationToken)
    {
        var documents =
            await knowledgeIndexingService.GetDocumentsAsync(
                cancellationToken);

        return Ok(documents);
    }

    [HttpGet("{documentId:guid}/status")]
    [ProducesResponseType<KnowledgeBaseDocumentStatusDto>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KnowledgeBaseDocumentStatusDto>> GetStatus(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var status =
            await knowledgeIndexingService.GetDocumentStatusAsync(
                documentId,
                cancellationToken);

        if (status is null)
        {
            return NotFound();
        }

        return Ok(status);
    }

    [HttpDelete("{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var deleted =
                await knowledgeIndexingService.DeleteDocumentAsync(
                    documentId,
                    cancellationToken);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (KnowledgeBaseBusyException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Knowledge base is busy.",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict,
            });
        }
    }

    [HttpPost("sync")]
    [ProducesResponseType<KnowledgeBaseSyncResultDto>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<KnowledgeBaseSyncResultDto>> SyncAll(
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await knowledgeIndexingService.ResyncAllAsync(
                    cancellationToken);

            return Ok(result);
        }
        catch (KnowledgeBaseBusyException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Knowledge base is busy.",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict,
            });
        }
    }
}

public sealed class AddKnowledgeBaseTextRequest
{
    public string? Title { get; init; }
    public string Content { get; init; } = string.Empty;
}

public sealed class UploadKnowledgeBaseDocumentRequest
{
    public IFormFile File { get; init; } = null!;
}