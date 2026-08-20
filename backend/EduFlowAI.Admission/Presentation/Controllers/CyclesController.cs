using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Shared.Kernel.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduFlowAI.Admission.Presentation.Controllers
{
    [ApiController]
    [Route("api/modules/admission/[controller]")]
    public class CyclesController : ControllerBase
    {
        private readonly ICycleQueryService _cycleQueryService;

        public CyclesController(ICycleQueryService cycleQueryService)
        {
            _cycleQueryService = cycleQueryService;
        }

        /// <summary>
        /// Retrieves a list of currently active admission cycles along with their associated program details.
        /// Used primarily by the applicant portal for program selection.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A result containing a collection of active cycles and programs.</returns>
        [HttpGet("active")]
        [AllowAnonymous] // Ensures this endpoint is accessible to applicants without admin authorization
        public async Task<ActionResult<Result<IEnumerable<ActiveAdmissionCycleDto>>>> GetActiveCycles(CancellationToken cancellationToken)
        {
            var result = await _cycleQueryService.GetActiveCyclesAsync(cancellationToken);

            // Return the structured Result object with the appropriate HTTP Status Code
            return StatusCode(result.StatusCode, result);
        }
    }
}
