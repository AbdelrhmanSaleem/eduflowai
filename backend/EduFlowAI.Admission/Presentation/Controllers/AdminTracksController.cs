using EduFlowAI.Admission.Application.Features.Tracks;
using EduFlowAI.Shared.Kernel.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduFlowAI.Admission.Presentation.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/admin/tracks")]
public sealed class AdminTracksController : ControllerBase
{
    private readonly ITrackService _trackService;

    public AdminTracksController(ITrackService trackService)
    {
        ArgumentNullException.ThrowIfNull(trackService);
        _trackService = trackService;
    }

    [HttpGet]
    public async Task<ActionResult<Result<IReadOnlyList<TrackDto>>>> GetTracks(
        CancellationToken cancellationToken)
    {
        var tracks = await _trackService.GetAdminTracksAsync(cancellationToken);
        return Ok(Result<IReadOnlyList<TrackDto>>.Success(tracks));
    }

    [HttpPost]
    public async Task<ActionResult<Result<TrackDto>>> CreateTrack(
        [FromBody] CreateTrackRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _trackService.CreateTrackAsync(
            request,
            cancellationToken);

        return this.ToActionResult(result);
    }

    [HttpPut("{trackId:guid}")]
    public async Task<ActionResult<Result<TrackDto>>> UpdateTrack(
        Guid trackId,
        [FromBody] UpdateTrackRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _trackService.UpdateTrackAsync(
            trackId,
            request,
            cancellationToken);

        return this.ToActionResult(result);
    }
}
