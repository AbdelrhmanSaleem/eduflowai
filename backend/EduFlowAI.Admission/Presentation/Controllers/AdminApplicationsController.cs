using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Enums;
using EduFlowAI.Shared.Kernel.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduFlowAI.Admission.Presentation.Controllers
{
    [ApiController]
    [Route("api/modules/admission/admin/applications")]
    [Authorize(Roles = "SuperAdmin")] // Uncomment this later when Identity is implemented
    public class AdminApplicationsController : ControllerBase
    {
        private readonly IAdminOperationsService _adminOperationsService;
        private readonly IAssessmentSimulationService _simulationService;
        private readonly IFinalRankingService _finalRankingService;

        public AdminApplicationsController(IAdminOperationsService adminOperationsService,
            IAssessmentSimulationService simulationService,
            IFinalRankingService finalRankingService)
        {
            _adminOperationsService = adminOperationsService;
            _simulationService = simulationService;
            _finalRankingService = finalRankingService;
        }

        [HttpPost("{applicationId}/eligibility-override")]
        public async Task<ActionResult<Result<string>>> OverrideEligibility(Guid applicationId, [FromBody] string overrideReason)
        {
            // Extract Admin ID from token
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "dummy-admin-id";

            var (isSuccess, errorMessage) = await _adminOperationsService.OverrideEligibilityAsync(applicationId, adminId, overrideReason);
            if (!isSuccess)
            {
                int statusCode = errorMessage!.Contains("not found") ? 404 : 400;
                var failureResult = Result<string>.Failure(statusCode, errorMessage);
                return StatusCode(statusCode, failureResult);
            }

            var successResult = Result<string>.Success(overrideReason, message: "Application overridden successfully. Status updated.");
            return Ok(successResult);
        }

        [HttpPost("{applicationId}/simulate-stage")]
        public async Task<ActionResult<Result<SimulatedStageDto>>> SimulateStage(Guid applicationId, [FromQuery] SelectionStage stage)
        {
            var (isSuccess, errorMessage, data) = await _simulationService.SimulateStageAsync(applicationId, stage);

            if (!isSuccess)
            {
                int statusCode = errorMessage!.Contains("not found") ? 404 : 400;
                var failureResult = Result<SimulatedStageDto>.Failure(statusCode, errorMessage);
                return StatusCode(statusCode, failureResult);
            }

            var successResult = Result<SimulatedStageDto>.Success(data!, message: "Assessment stage simulated successfully.");
            return Ok(successResult);
        }

        [HttpPost("cycles/{cycleId}/bulk-simulate-stage")]
        public async Task<ActionResult<Result<string>>> BulkSimulateStage(Guid cycleId, [FromQuery] SelectionStage stage)
        {
            var (isSuccess, errorMessage, processedCount) = await _simulationService.BulkSimulateStageAsync(cycleId, stage);

            if (!isSuccess)
            {
                var failureResult = Result<string>.Failure(400, errorMessage!);
                return BadRequest(failureResult);
            }

            string resultMessage = $"Bulk simulation completed. Successfully processed {processedCount} applications for stage: {stage}.";
            var successResult = Result<string>.Success(resultMessage, message: "Bulk simulation executed successfully.");

            return Ok(successResult);
        }

        [HttpPost("execute-ranking")]
        public async Task<ActionResult<Result<string>>> ExecuteRanking([FromQuery] int admittedCapacity = 20, [FromQuery] int waitlistCapacity = 20)
        {
            var (isSuccess, errorMessage, admitted, waitlisted) = await _finalRankingService.ExecuteRankingAsync(admittedCapacity, waitlistCapacity);

            if (!isSuccess)
            {
                var failureResult = Result<string>.Failure(400, errorMessage!);
                return BadRequest(failureResult);
            }

            string resultMessage = $"Ranking executed successfully. {admitted} admitted, {waitlisted} waitlisted.";
            var successResult = Result<string>.Success(resultMessage, message: "Workflow completed.");

            return Ok(successResult);
        }
    }
}
