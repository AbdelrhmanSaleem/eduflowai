using System;
using System.Threading;
using System.Threading.Tasks;
using EduFlowAI.Documents.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EduFlowAI.Documents.Presentation.Controllers;

[ApiController]
[Route("api/documents/{documentId:guid}/verification")]
public sealed class DocumentVerificationController : ControllerBase
{
    private readonly IDocumentVerificationQueryService _queryService;

    public DocumentVerificationController(IDocumentVerificationQueryService queryService)
    {
        _queryService = queryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetVerification(
        [FromRoute] Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _queryService.GetVerificationAsync(documentId, cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { error = result.Message });
        }

        return StatusCode(result.StatusCode, result.Data);
    }

    [HttpGet("attempts")]
    public async Task<IActionResult> GetVerificationAttempts(
        [FromRoute] Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _queryService.GetVerificationAttemptsAsync(documentId, cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { error = result.Message });
        }

        return StatusCode(result.StatusCode, result.Data);
    }
}
