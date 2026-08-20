using EduFlowAI.Admission.Application.Features.Programs;
using EduFlowAI.Admission.Application.Features.Requirements;
using EduFlowAI.Shared.Kernel.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduFlowAI.Admission.Presentation.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/admin/programs")]
public sealed class AdminProgramsController : ControllerBase
{
    private readonly IProgramConfigurationService _programService;
    private readonly IProgramRequirementService _requirementService;

    public AdminProgramsController(
        IProgramConfigurationService programService,
        IProgramRequirementService requirementService)
    {
        ArgumentNullException.ThrowIfNull(programService);
        ArgumentNullException.ThrowIfNull(requirementService);

        _programService = programService;
        _requirementService = requirementService;
    }

    [HttpGet]
    public async Task<ActionResult<Result<IReadOnlyList<ProgramDto>>>> GetPrograms(
        CancellationToken cancellationToken)
    {
        var programs = await _programService.GetProgramsAsync(cancellationToken);
        return Ok(Result<IReadOnlyList<ProgramDto>>.Success(programs));
    }

    [HttpPost]
    public async Task<ActionResult<Result<ProgramDto>>> CreateProgram(
        [FromBody] CreateProgramRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _programService.CreateProgramAsync(
            request,
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPut("{programId:guid}")]
    public async Task<ActionResult<Result<ProgramDto>>> UpdateProgram(
        Guid programId,
        [FromBody] UpdateProgramRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _programService.UpdateProgramAsync(
            programId,
            request,
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpDelete("{programId:guid}")]
    public async Task<ActionResult<Result<bool>>> DeleteProgram(
        Guid programId,
        CancellationToken cancellationToken)
    {
        var result = await _programService.DeleteProgramAsync(
            programId,
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpGet("{programId:guid}/document-requirements")]
    public async Task<ActionResult<Result<IReadOnlyList<ProgramDocumentRequirementDto>>>> GetProgramDocumentRequirements(
        Guid programId,
        CancellationToken cancellationToken)
    {
        var requirements = await _requirementService.GetProgramRequirementsAsync(
            programId,
            cancellationToken);

        return requirements is null
            ? NotFound(Result<IReadOnlyList<ProgramDocumentRequirementDto>>.Failure(
                404,
                "Program was not found."))
            : Ok(Result<IReadOnlyList<ProgramDocumentRequirementDto>>.Success(
                requirements));
    }

    [HttpPut("{programId:guid}/document-requirements")]
    public async Task<ActionResult<Result<IReadOnlyList<ProgramDocumentRequirementDto>>>> UpdateProgramDocumentRequirements(
        Guid programId,
        [FromBody] UpdateProgramDocumentRequirementsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _requirementService.ReplaceProgramRequirementsAsync(
            programId,
            request,
            cancellationToken);

        return this.ToActionResult(result);
    }
}
