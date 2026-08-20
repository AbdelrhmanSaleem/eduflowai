using EduFlowAI.Documents.Application.DTOs;
using EduFlowAI.Documents.Application.Interfaces;
using EduFlowAI.Documents.Application.Services;
using EduFlowAI.Documents.Presentation.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EduFlowAI.Documents.Presentation.Controllers;

[ApiController]
//[Authorize] // Enforces that the user must be authenticated
[Route("api/applications/{applicationId:guid}/documents")]
public sealed class ApplicantDocumentsController : ControllerBase
{
    private readonly IApplicantDocumentService _documentService;
    private readonly IDocumentSubmissionService _submissionService;


    public ApplicantDocumentsController(IApplicantDocumentService documentService, IDocumentSubmissionService submissionService)
    {
        _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        _submissionService = submissionService ?? throw new ArgumentNullException(nameof(submissionService));
    }

    [HttpPost]
    public async Task<IActionResult> UploadDocument(
        [FromRoute] Guid applicationId,
        [FromForm] UploadDocumentRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Map the HTTP request to your application DTO
        var dto = new UploadDocumentDto(
            applicationId,
            request.DocumentType,
            request.File);

        // 2. Execute the business logic
        var result = await _documentService.UploadDocumentAsync(dto, cancellationToken);

        // 3. Translate the custom Result<T> into a standard HTTP response
        if (result.IsSuccess)
        {
            // Returns the Document ID so the frontend has a reference to what was just created
            return StatusCode(result.StatusCode, new
            {
                documentId = result.Data,
                message = "Document uploaded successfully."
            });
        }

        // Returns the exact failure code and the error message
        return StatusCode(result.StatusCode, new
        {
            error = result.Message
        });
    }


    [HttpGet]
    public async Task<IActionResult> GetDocuments(
    [FromRoute] Guid applicationId,
    CancellationToken cancellationToken)
    {
        var result = await _documentService.GetDocumentsByApplicationIdAsync(
            applicationId,
            cancellationToken);

        if (result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new
            {
                documents = result.Data
            });
        }

        return StatusCode(result.StatusCode, new
        {
            error = result.Message
        });
    }



    [HttpGet("/api/documents/{documentId:guid}/file")]
    public async Task<IActionResult> DownloadDocument(
        [FromRoute] Guid documentId,
        CancellationToken cancellationToken)
    {
        var result = await _documentService.DownloadDocumentAsync(documentId, cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { error = result.Message });
        }

        // 1. Get the stream and filename from the unwrapped result
        var stream = result.Data.Stream;
        var fileName = result.Data.OriginalFileName;

        // 2. Safely resolve the MIME type using ASP.NET Core's built-in provider
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fileName, out var contentType))
        {
            // Fallback for unknown file types
            contentType = "application/octet-stream";
        }

        // 3. Return the physical file to the browser
        return File(stream, contentType, fileName);
    }


    [HttpPost("submit")]
    public async Task<IActionResult> SubmitDocumentPackage(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await _submissionService.SubmitPackageAsync(applicationId, cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { Message = result.Message });
        }

        return StatusCode(result.StatusCode, new
        {
            Message = result.Message,
            SubmittedCount = result.Data
        });
    }




    [HttpGet("required")]
    public async Task<IActionResult> GetRequiredDocuments(
    [FromRoute] Guid applicationId,
    CancellationToken cancellationToken)
    {
        var result = await _documentService.GetRequiredDocumentTypesAsync(applicationId, cancellationToken);

        if (result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new
            {
                documentTypes = result.Data.DocumentTypes
            });
        }

        return StatusCode(result.StatusCode, new
        {
            error = result.Message
        });
    }
}