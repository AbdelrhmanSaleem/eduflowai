using EduFlowAI.Admission.Application.Features.Offerings;
using EduFlowAI.Admission.Application.Features.Tracks;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Infrastructure.Seeding;

namespace EduFlowAI.Admission.Tests;

public sealed class AdmissionCatalogOfferingTests
{
    [Fact]
    public void Official_track_accepts_only_its_canonical_locations_by_stable_branch_id()
    {
        var definition = AdmissionTrackSeedCatalog.All.Single(track =>
            track.Name.StartsWith(".NET Enterprise", StringComparison.Ordinal));
        var track = CreateTrack(definition);
        var validBranch = new Branch
        {
            Id = AdmissionSeedIds.TantaBranchId,
            Name = "Renamed Tanta"
        };
        var invalidBranch = new Branch
        {
            Id = AdmissionSeedIds.ZagazigBranchId,
            Name = "Tanta"
        };

        Assert.True(OfferingService.IsAllowedByOfficialCatalog(
            track,
            validBranch,
            AdmissionSeedIds.NineMonthProgramId,
            "9M"));
        Assert.False(OfferingService.IsAllowedByOfficialCatalog(
            track,
            invalidBranch,
            AdmissionSeedIds.NineMonthProgramId,
            "9M"));
    }

    [Fact]
    public void Application_integration_overload_keeps_the_same_catalog_rule()
    {
        var definition = AdmissionTrackSeedCatalog.All.Single(track =>
            track.Name == "Digital IC Design");
        var track = CreateTrack(definition);
        var validBranch = new Branch
        {
            Id = AdmissionSeedIds.SmartVillageBranchId,
            Name = "Smart Village"
        };
        var invalidBranch = new Branch
        {
            Id = AdmissionSeedIds.AlexandriaBranchId,
            Name = "Alexandria"
        };

        Assert.True(OfferingService.IsAllowedByOfficialCatalog(
            track,
            validBranch,
            "9M"));
        Assert.False(OfferingService.IsAllowedByOfficialCatalog(
            track,
            invalidBranch,
            "9M"));
    }

    [Fact]
    public void Invalid_existing_official_9m_offering_is_hidden_but_custom_tracks_are_preserved()
    {
        var definition = AdmissionTrackSeedCatalog.All.Single(track =>
            track.Name == "Digital IC Design");
        var track = CreateTrack(definition);
        var invalidBranch = new Branch
        {
            Id = AdmissionSeedIds.AlexandriaBranchId,
            Name = "Alexandria"
        };

        Assert.False(TrackService.IsPublicOfferingAllowed(
            track,
            invalidBranch,
            AdmissionSeedIds.NineMonthProgramId,
            "9M"));
        Assert.True(TrackService.IsPublicOfferingAllowed(
            track,
            invalidBranch,
            Guid.NewGuid(),
            "CUSTOM"));

        var customNineMonthTrack = new Track
        {
            Id = Guid.NewGuid(),
            ProgramId = AdmissionSeedIds.NineMonthProgramId,
            Name = "Admin configured 9M track",
            IsActive = true
        };

        Assert.True(OfferingService.IsAllowedByOfficialCatalog(
            customNineMonthTrack,
            invalidBranch,
            AdmissionSeedIds.NineMonthProgramId,
            "9M"));
    }

    private static Track CreateTrack(AdmissionTrackSeedDefinition definition)
    {
        return new Track
        {
            Id = definition.Id,
            ProgramId = AdmissionSeedIds.NineMonthProgramId,
            Name = definition.Name,
            Description = definition.Description,
            PrerequisiteTopics = [.. definition.PrerequisiteTopics],
            IsActive = true
        };
    }
}
