using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.Admission.Application.Services;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;
using EduFlowAI.Admission.Infrastructure.Seeding;

namespace EduFlowAI.Admission.Tests;

public sealed class OfferedTrackReaderTests
{
    [Fact]
    public void Eligibility_requires_active_cycle_track_and_branch()
    {
        var predicate =
            OfferedTrackRecommendationProjection.IsActiveOffering.Compile();

        Assert.True(predicate(CreateOffering(
            CycleStatus.Active,
            trackIsActive: true,
            branchIsActive: true)));

        Assert.False(predicate(CreateOffering(
            CycleStatus.Draft,
            trackIsActive: true,
            branchIsActive: true)));

        Assert.False(predicate(CreateOffering(
            CycleStatus.Active,
            trackIsActive: false,
            branchIsActive: true)));

        Assert.False(predicate(CreateOffering(
            CycleStatus.Active,
            trackIsActive: true,
            branchIsActive: false)));
    }

    [Fact]
    public void Projection_removes_duplicates_and_returns_read_only_dtos()
    {
        var aiTrack = CreateTrack(
            "AI and Machine Learning",
            ["Python", "Linear Algebra"]);
        var cloudTrack = CreateTrack(
            "Cloud Architecture",
            ["Linux", "Networking"]);

        var offerings = new[]
        {
            CreateOffering(
                CycleStatus.Active,
                trackIsActive: true,
                branchIsActive: true,
                track: cloudTrack),
            CreateOffering(
                CycleStatus.Active,
                trackIsActive: true,
                branchIsActive: true,
                track: aiTrack),
            CreateOffering(
                CycleStatus.Active,
                trackIsActive: true,
                branchIsActive: true,
                track: aiTrack)
        };

        var result = OfferedTrackRecommendationProjection.Project(offerings);

        Assert.Equal(2, result.Count);
        Assert.Equal(aiTrack.Id, result[0].TrackId);
        Assert.Equal(cloudTrack.Id, result[1].TrackId);
        Assert.Equal(
            new[] { "Python", "Linear Algebra" },
            result[0].PrerequisiteTopics);
        Assert.Equal(new[] { "Test Branch" }, result[0].Locations);

        var outerCollection = Assert.IsAssignableFrom<
            IList<OfferedTrackForRecommendationDto>>(result);
        Assert.Throws<NotSupportedException>(
            () => outerCollection.Add(result[0]));

        var topics = Assert.IsAssignableFrom<ICollection<string>>(
            result[0].PrerequisiteTopics);
        Assert.Throws<NotSupportedException>(() => topics.Add("Statistics"));
    }

    [Fact]
    public void Projection_hides_invalid_9m_location_and_returns_contract_fields()
    {
        var definition = AdmissionTrackSeedCatalog.All
            .Single(track => track.Name == "Digital IC Design");
        var track = CreateTrack(definition.Name, ["Legacy topic"]);
        track.Id = definition.Id;
        track.Program = new EduFlowAI.Admission.Domain.Entities.Program
        {
            Id = track.ProgramId,
            InstitutionId = Guid.NewGuid(),
            Name = "9-Month Professional Training Program",
            Code = "9M",
            DurationMonths = 9
        };

        var offering = CreateOffering(
            CycleStatus.Active,
            trackIsActive: true,
            branchIsActive: true,
            track: track);
        offering.Branch.Id = AdmissionSeedIds.AlexandriaBranchId;
        offering.BranchId = offering.Branch.Id;

        Assert.Empty(OfferedTrackRecommendationProjection.Project([offering]));

        offering.Branch.Id = AdmissionSeedIds.SmartVillageBranchId;
        offering.BranchId = offering.Branch.Id;
        var projected = Assert.Single(
            OfferedTrackRecommendationProjection.Project([offering]));

        Assert.Equal(track.Id, projected.TrackId);
        Assert.Equal(track.Name, projected.Name);
        Assert.Equal(track.Description, projected.Description);
        Assert.Equal(new[] { "Legacy topic" }, projected.PrerequisiteTopics);
        Assert.Equal(new[] { "Test Branch" }, projected.Locations);
    }

    [Fact]
    public void Projection_preserves_current_admin_description_and_topics()
    {
        var definition = AdmissionTrackSeedCatalog.All.Single(track =>
            track.Name == "3D Generalist");
        var track = CreateTrack(definition.Name, ["Stale topic"]);
        track.Id = definition.Id;
        track.Description = "Stale inferred description";
        track.Program = new EduFlowAI.Admission.Domain.Entities.Program
        {
            Id = track.ProgramId,
            InstitutionId = Guid.NewGuid(),
            Name = "9-Month Professional Training Program",
            Code = "9M",
            DurationMonths = 9
        };
        var offering = CreateOffering(
            CycleStatus.Active,
            trackIsActive: true,
            branchIsActive: true,
            track: track);
        offering.Branch.Id = AdmissionSeedIds.SmartVillageBranchId;
        offering.BranchId = offering.Branch.Id;

        var projected = Assert.Single(
            OfferedTrackRecommendationProjection.Project([offering]));

        Assert.Equal("Stale inferred description", projected.Description);
        Assert.Equal(new[] { "Stale topic" }, projected.PrerequisiteTopics);
    }

    private static Track CreateTrack(
        string name,
        List<string> prerequisiteTopics)
    {
        return new Track
        {
            Id = Guid.NewGuid(),
            ProgramId = Guid.NewGuid(),
            Name = name,
            Description = $"{name} description",
            PrerequisiteTopics = prerequisiteTopics,
            IsActive = true
        };
    }

    private static TrackBranchOffering CreateOffering(
        CycleStatus cycleStatus,
        bool trackIsActive,
        bool branchIsActive,
        Track? track = null)
    {
        track ??= CreateTrack(
            "Test Track",
            ["Programming fundamentals"]);
        track.IsActive = trackIsActive;

        var cycle = new AdmissionCycle
        {
            Id = Guid.NewGuid(),
            ProgramId = track.ProgramId,
            Label = "Test Cycle",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DeadlineUtc = DateTimeOffset.UtcNow.AddMonths(1),
            Status = cycleStatus
        };

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            Name = "Test Branch",
            IsActive = branchIsActive
        };

        return new TrackBranchOffering
        {
            Id = Guid.NewGuid(),
            CycleId = cycle.Id,
            TrackId = track.Id,
            BranchId = branch.Id,
            Capacity = 25,
            Cycle = cycle,
            Track = track,
            Branch = branch
        };
    }
}
