using System.Linq.Expressions;
using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;
using EduFlowAI.Admission.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;

namespace EduFlowAI.Admission.Application.Services;

public sealed class OfferedTrackReader : IOfferedTrackReader
{
    private readonly IAdmissionDbContext _dbContext;

    public OfferedTrackReader(IAdmissionDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OfferedTrackForRecommendationDto>>
        GetActiveOfferedTracksAsync(
            CancellationToken cancellationToken = default)
    {
        var offerings = await _dbContext.TrackBranchOfferings
            .AsNoTracking()
            .Include(offering => offering.Track)
                .ThenInclude(track => track.Program)
            .Include(offering => offering.Branch)
            .Where(OfferedTrackRecommendationProjection.IsActiveOffering)
            .OrderBy(offering => offering.Track.Name)
            .ThenBy(offering => offering.TrackId)
            .ThenBy(offering => offering.BranchId)
            .ToListAsync(cancellationToken);

        return OfferedTrackRecommendationProjection.Project(offerings);
    }
}

internal static class OfferedTrackRecommendationProjection
{
    // TrackBranchOffering has no IsActive flag. An offering is available when its
    // cycle, track, and branch are all active/available.
    internal static readonly Expression<Func<TrackBranchOffering, bool>>
        IsActiveOffering = offering =>
            offering.Cycle.Status == CycleStatus.Active &&
            offering.Track.IsActive &&
            offering.Branch.IsActive;

    internal static IReadOnlyList<OfferedTrackForRecommendationDto> Project(
        IEnumerable<TrackBranchOffering> offerings)
    {
        ArgumentNullException.ThrowIfNull(offerings);

        var tracks = offerings
            .Where(IsAllowedOffering)
            .GroupBy(offering => offering.TrackId)
            .Select(group =>
            {
                var track = group.First().Track;
                var locations = group
                    .Select(offering => offering.Branch.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return new OfferedTrackForRecommendationDto
                {
                    TrackId = track.Id,
                    Name = track.Name,
                    Description = track.Description,
                    PrerequisiteTopics = Array.AsReadOnly(
                        track.PrerequisiteTopics.ToArray()),
                    Locations = Array.AsReadOnly(locations)
                };
            })
            .OrderBy(track => track.Name)
            .ThenBy(track => track.TrackId)
            .ToArray();

        return Array.AsReadOnly(tracks);
    }

    internal static bool IsAllowedOffering(TrackBranchOffering offering)
    {
        bool isNineMonthProgram =
            offering.Track.ProgramId == AdmissionSeedIds.NineMonthProgramId ||
            StringComparer.OrdinalIgnoreCase.Equals(
                offering.Track.Program?.Code,
                "9M");

        if (!isNineMonthProgram)
        {
            return true;
        }

        if (AdmissionLegacyTrackCatalog.IsHistorical(
                offering.Track.Id,
                offering.Track.Name))
        {
            return false;
        }

        var definition = AdmissionTrackSeedCatalog.Find(
            offering.Track.Id,
            offering.Track.Name);

        return definition is null ||
            AdmissionTrackSeedCatalog.IsCanonicalLocation(
                definition,
                offering.Branch.Id);
    }
}
