using EduFlowAI.Admission.Application.Features.Programs;
using EduFlowAI.Shared.Kernel.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduFlowAI.Admission.Presentation.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/admin/institutions")]
public sealed class AdminInstitutionsController : ControllerBase
{
    private readonly IProgramConfigurationService _programService;

    public AdminInstitutionsController(
        IProgramConfigurationService programService)
    {
        ArgumentNullException.ThrowIfNull(programService);
        _programService = programService;
    }

    [HttpGet]
    public async Task<ActionResult<Result<IReadOnlyList<InstitutionDto>>>> GetInstitutions(
        CancellationToken cancellationToken)
    {
        var institutions = await _programService.GetInstitutionsAsync(
            cancellationToken);

        return Ok(Result<IReadOnlyList<InstitutionDto>>.Success(institutions));
    }

    [HttpPost]
    public async Task<ActionResult<Result<InstitutionDto>>> CreateInstitution(
        [FromBody] CreateInstitutionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _programService.CreateInstitutionAsync(
            request,
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPut("{institutionId:guid}")]
    public async Task<ActionResult<Result<InstitutionDto>>> UpdateInstitution(
        Guid institutionId,
        [FromBody] UpdateInstitutionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _programService.UpdateInstitutionAsync(
            institutionId,
            request,
            cancellationToken);

        return this.ToActionResult(result);
    }
}
