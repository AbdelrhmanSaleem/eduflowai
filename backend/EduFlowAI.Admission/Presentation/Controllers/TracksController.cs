using EduFlowAI.Admission.Application.Features.Tracks;
using EduFlowAI.Shared.Kernel.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduFlowAI.Admission.Presentation.Controllers;

[ApiController]
[Route("api/tracks")]
public sealed class TracksController : ControllerBase
{
    private readonly ITrackService _trackService;

    public TracksController(ITrackService trackService)
    {
        ArgumentNullException.ThrowIfNull(trackService);
        _trackService = trackService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<Result<IReadOnlyList<TrackDto>>>> GetTracks(
        [FromQuery] Guid? cycleId,      // Optional parameter to filter tracks by admission cycle
        CancellationToken cancellationToken)
    {
        var tracks = await _trackService.GetPublicTracksAsync(cycleId, cancellationToken);
        return Ok(Result<IReadOnlyList<TrackDto>>.Success(tracks));
    }

    [HttpGet("{trackId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<TrackDto>>> GetTrack(
        Guid trackId,
        CancellationToken cancellationToken)
    {
        var track = await _trackService.GetPublicTrackAsync(
            trackId,
            cancellationToken);

        return track is null
            ? NotFound(Result<TrackDto>.Failure(
                404,
                "Track was not found in the public catalog."))
            : Ok(Result<TrackDto>.Success(track));
    }
}
