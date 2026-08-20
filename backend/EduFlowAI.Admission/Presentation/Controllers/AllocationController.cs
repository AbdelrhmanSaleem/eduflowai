using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Shared.Kernel.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Presentation.Controllers
{
    [ApiController]
    [Route("api/cycles")]
    // Extremely important: Restrict this endpoint to administrators only
    [Authorize(Roles = "SuperAdmin")]
    public class AllocationController : ControllerBase
    {
        private readonly IAllocationService _allocationService;

        public AllocationController(IAllocationService allocationService)
        {
            _allocationService = allocationService;
        }

        [HttpPost("{cycleId}/run-allocation")]
        public async Task<ActionResult<Result<string>>> RunAllocation(Guid cycleId, CancellationToken cancellationToken = default)
        {
            // Execute the allocation engine
            var (isSuccess, errorMessage) =
                await _allocationService.RunAllocationAsync(
                    cycleId,
                    cancellationToken);

            if (!isSuccess)
            {
                // Wrap the error message using the Result<T>.Failure method
                var failureResult = Result<string>.Failure(400, errorMessage);
                return StatusCode(failureResult.StatusCode, failureResult);
            }

            // Wrap the success statistics using the Result<T>.Success method
            // We pass the errorMessage (which contains the statistics) as the Data, and a unified success message
            var successResult = Result<string>.Success(errorMessage, 200, "Allocation engine executed successfully.");

            return Ok(successResult);
        }
    }
}
