using EduFlowAI.Documents.Application.DTOs;
using EduFlowAI.Documents.Application.Interfaces;
using EduFlowAI.Shared.Kernel.Common.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduFlowAI.Admission.Presentation.Controllers
{
    [ApiController]
    [Route("api/operations/[controller]")]
    public class HumanReviewController : ControllerBase
    {
        private readonly IDocumentHumanReviewService _documentHumanReviewService;

        public HumanReviewController(IDocumentHumanReviewService documentHumanReviewService)
        {
            _documentHumanReviewService = documentHumanReviewService;
        }

        [HttpGet("document-reviews")]
        [Authorize(Roles = "SuperAdmin, OperationsManager")]
        public async Task<IActionResult> GetAllDocumentsReviews([FromQuery] QueryParameters queryParameters)
        {
            var result = await _documentHumanReviewService.GetAllDocumentsReviewsAsync(queryParameters);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Message);
            }

            return StatusCode(result.StatusCode, result.Data);
        }

        [HttpGet("document-review/{documentId}")]
        [Authorize(Roles = "SuperAdmin, OperationsManager")]
        public async Task<IActionResult> GetDocumentReview(Guid documentId)
        {
            var result = await _documentHumanReviewService.GetDocumentReviewAsync(documentId);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Message);
            }
            return StatusCode(result.StatusCode, result.Data);
        }

        [HttpGet("document-file/{documentId}")]
        [Authorize]
        public async Task<IActionResult> GetDocumentFile(Guid documentId)
        {
            var result = await _documentHumanReviewService.GetDocumentFileAsync(documentId);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Message);
            }
            return File(result.Data.Content, result.Data.ContentType, enableRangeProcessing: true);
        }

        [HttpPost("approve-review/{documentId}")]
        [Authorize(Roles ="SuperAdmin, OperationsManager")]
        public async Task<IActionResult> ApproveReview(Guid documentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var result = await _documentHumanReviewService.ApproveReviewAsync(documentId, userId, default);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Message);
            }
            return StatusCode(result.StatusCode, result.Message);
        }

        [HttpPost("reject-review/{documentId}")]
        [Authorize(Roles = "SuperAdmin, OperationsManager")]
        public async Task<IActionResult> RejectReview(Guid documentId, string reason)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var result = await _documentHumanReviewService.RejectReviewAsync(documentId, reason, default);
            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Message);
            }
            return StatusCode(result.StatusCode, result.Message);
        }
    }
}