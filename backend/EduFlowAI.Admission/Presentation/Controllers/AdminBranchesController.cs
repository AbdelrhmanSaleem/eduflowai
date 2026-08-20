using EduFlowAI.Admission.Application.Features.Branches;
using EduFlowAI.Shared.Kernel.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduFlowAI.Admission.Presentation.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/admin/branches")]
public sealed class AdminBranchesController : ControllerBase
{
    private readonly IBranchService _branchService;

    public AdminBranchesController(IBranchService branchService)
    {
        ArgumentNullException.ThrowIfNull(branchService);
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<ActionResult<Result<IReadOnlyList<BranchDto>>>> GetBranches(
        CancellationToken cancellationToken)
    {
        var branches = await _branchService.GetBranchesAsync(cancellationToken);
        return Ok(Result<IReadOnlyList<BranchDto>>.Success(branches));
    }

    [HttpPost]
    public async Task<ActionResult<Result<BranchDto>>> CreateBranch(
        [FromBody] CreateBranchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _branchService.CreateBranchAsync(
            request,
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPut("{branchId:guid}")]
    public async Task<ActionResult<Result<BranchDto>>> UpdateBranch(
        Guid branchId,
        [FromBody] UpdateBranchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _branchService.UpdateBranchAsync(
            branchId,
            request,
            cancellationToken);

        return this.ToActionResult(result);
    }
}
