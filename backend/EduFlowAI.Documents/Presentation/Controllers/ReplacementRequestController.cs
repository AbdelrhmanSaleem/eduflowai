using EduFlowAI.Documents.Application.DTOs;
using EduFlowAI.Documents.Application.Interfaces;
using EduFlowAI.Shared.Kernel.Common.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduFlowAI.Admission.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReplacementRequestController : ControllerBase
    {
        private readonly IReplacementRequestService _replacementRequestService;

        public ReplacementRequestController(IReplacementRequestService replacementRequestService)
        {
            _replacementRequestService = replacementRequestService;
        }

        [HttpPost("send-replacement-request")]
        [Authorize(Roles = "SuperAdmin, OperationsManager")]
        public async Task<IActionResult> SendReplacementRequest([FromBody] ReplacementRequestDto request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var result = await _replacementRequestService.SendReplacementRequest(request, userId!, default);
            if (!result.IsSuccess)
            {
                return BadRequest(result.Message);
            }
            return StatusCode(result.StatusCode, result.Message);
        }

        [HttpGet("applicant/replacement-requests")]
        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> GetReplacementRequests([FromQuery] QueryParameters queryParameters)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var result = await _replacementRequestService.GetAllReplacemntRequestsAsync(userId!,queryParameters);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode,result.Message);
            }
            return StatusCode(result.StatusCode, result.Data);
        }

        [HttpGet("applicant/replacement-requests/{requestId}")]
        public async Task<IActionResult> GetReplacementRequest(Guid requestId)
        {
            var result = await _replacementRequestService.GetReplacemntRequestAsync(requestId);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Message);
            }

            return StatusCode(result.StatusCode, result.Data);
        }

        [HttpPost("/api/replacement-requests/{requestId:guid}/upload")]
        [Consumes("multipart/form-data")]
        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> UploadReplacement(Guid requestId, IFormFile file, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var result = await _replacementRequestService.UploadReplacementAsync(requestId, file, userId, cancellationToken);

            if (!result.IsSuccess)
            {
                return StatusCode(result.StatusCode, result.Message);
            }

            return Ok(new
            {
                documentId = result.Data,
                message = result.Message,
                replacementStatus = "Fulfilled",
                documentStatus = "Verifying"
            });
        }

    }
}