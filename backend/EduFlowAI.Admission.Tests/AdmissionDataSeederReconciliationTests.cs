using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;
using EduFlowAI.Admission.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;

namespace EduFlowAI.Admission.Tests;

public sealed class AdmissionDataSeederReconciliationTests
{
    [Fact]
    public void Supported_legacy_name_is_renamed_without_restoring_admin_managed_metadata()
    {
        var definition = AdmissionTrackSeedCatalog.All.Single(track =>
            track.Id == AdmissionSeedIds.OpenSourceApplicationsDevelopmentTrackId);
        var now = new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);
        var track = new Track
        {
            Id = definition.Id,
            ProgramId = AdmissionSeedIds.NineMonthProgramId,
            Name = "Open Source Applications Development",
            Description = null,
            PrerequisiteTopics = [],
            IsActive = false,
            UpdatedAt = now.AddDays(-1)
        };

        bool changed = AdmissionDataSeeder.ReconcileOfficialTrack(
            track,
            AdmissionSeedIds.NineMonthProgramId,
            definition,
            now);

        Assert.True(changed);
        Assert.Equal(definition.Id, track.Id);
        Assert.Equal(definition.Name, track.Name);
        Assert.Null(track.Description);
        Assert.Empty(track.PrerequisiteTopics);
        Assert.False(track.IsActive);
        Assert.Equal(now, track.UpdatedAt);
    }

    [Fact]
    public void Administrator_edits_on_stable_official_id_are_not_overwritten()
    {
        var definition = AdmissionTrackSeedCatalog.All[0];
        var originalUpdatedAt = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var track = new Track
        {
            Id = definition.Id,
            ProgramId = AdmissionSeedIds.NineMonthProgramId,
            Name = "Admin renamed track",
            Description = "Admin description",
            PrerequisiteTopics = ["Admin topic"],
            IsActive = false,
            UpdatedAt = originalUpdatedAt
        };

        bool changed = AdmissionDataSeeder.ReconcileOfficialTrack(
            track,
            AdmissionSeedIds.NineMonthProgramId,
            definition,
            originalUpdatedAt.AddDays(1));

        Assert.False(changed);
        Assert.Equal("Admin renamed track", track.Name);
        Assert.Equal("Admin description", track.Description);
        Assert.Equal(new[] { "Admin topic" }, track.PrerequisiteTopics);
        Assert.False(track.IsActive);
        Assert.Equal(originalUpdatedAt, track.UpdatedAt);
    }

    [Fact]
    public void Ambiguous_legacy_track_cannot_be_reinterpreted_as_dotnet()
    {
        var definition = AdmissionTrackSeedCatalog.All.Single(track =>
            track.Id == AdmissionSeedIds.DotNetEnterpriseTrackId);
        var legacy = new Track
        {
            Id = AdmissionSeedIds.ProfessionalDevelopmentCrmTrackId,
            ProgramId = AdmissionSeedIds.NineMonthProgramId,
            Name = AdmissionLegacyTrackCatalog.ProfessionalDevelopmentCrmName,
            PrerequisiteTopics = [],
            IsActive = true
        };

        Assert.Throws<InvalidOperationException>(() =>
            AdmissionDataSeeder.ReconcileOfficialTrack(
                legacy,
                AdmissionSeedIds.NineMonthProgramId,
                definition,
                DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Only_known_ambiguous_legacy_tracks_are_deactivated(
        bool useKnownLegacyTrack,
        bool expectedChanged)
    {
        var now = new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);
        var originalUpdatedAt = now.AddDays(-1);
        var track = new Track
        {
            Id = useKnownLegacyTrack
                ? AdmissionSeedIds.IntegratedSoftwareArchitectureTrackId
                : Guid.NewGuid(),
            ProgramId = AdmissionSeedIds.NineMonthProgramId,
            Name = useKnownLegacyTrack
                ? AdmissionLegacyTrackCatalog.IntegratedSoftwareArchitectureName
                : "Admin configured 9M track",
            PrerequisiteTopics = [],
            IsActive = true,
            UpdatedAt = originalUpdatedAt
        };

        bool changed = AdmissionDataSeeder.DeactivateNonOfficialTrack(
            track,
            AdmissionSeedIds.NineMonthProgramId,
            now);

        Assert.Equal(expectedChanged, changed);
        Assert.Equal(!expectedChanged, track.IsActive);
        Assert.Equal(expectedChanged ? now : originalUpdatedAt, track.UpdatedAt);
    }

    [Fact]
    public void Official_branch_reconciliation_preserves_all_admin_managed_fields()
    {
        var definition = AdmissionBranchSeedCatalog.All.Single(branch =>
            branch.Name == "Smart Village");
        var branch = new Branch
        {
            Id = definition.Id,
            Name = "Renamed branch",
            Governorate = null,
            IsActive = false
        };

        bool changed = AdmissionDataSeeder.ReconcileOfficialBranch(
            branch,
            definition);

        Assert.False(changed);
        Assert.Equal(definition.Id, branch.Id);
        Assert.Equal("Renamed branch", branch.Name);
        Assert.Null(branch.Governorate);
        Assert.False(branch.IsActive);
    }

    [Fact]
    public async Task Explicit_reconciliation_rerun_preserves_admin_blanks_and_removed_requirements()
    {
        var options = new DbContextOptionsBuilder<TestAdmissionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new TestAdmissionDbContext(options);

        var firstRunAt = new DateTimeOffset(
            2026,
            8,
            12,
            10,
            0,
            0,
            TimeSpan.Zero);

        await AdmissionDataSeeder.ReconcileAsync(
            context,
            firstRunAt,
            CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.Single(context.Institutions);
        Assert.Single(context.Programs);
        Assert.Equal(4, context.ProgramDocumentRequirements.Count());
        Assert.Equal(31, context.Tracks.Count());
        Assert.Equal(12, context.Branches.Count());

        var trackDefinition = AdmissionTrackSeedCatalog.All.Single(definition =>
            definition.Name == "Digital IC Design");
        var track = await context.Tracks.SingleAsync(candidate =>
            candidate.Id == trackDefinition.Id);
        track.Description = null;
        track.PrerequisiteTopics = [];
        track.IsActive = false;

        var branch = await context.Branches.SingleAsync(candidate =>
            candidate.Id == AdmissionSeedIds.SmartVillageBranchId);
        branch.Name = "Admin renamed Smart Village";
        branch.Governorate = null;
        branch.IsActive = false;

        var removedRequirement = await context.ProgramDocumentRequirements
            .SingleAsync(requirement =>
                requirement.DocumentType == DocumentType.MilitaryCertificate &&
                requirement.RequiredForGender == Gender.Male);
        context.ProgramDocumentRequirements.Remove(removedRequirement);
        await context.SaveChangesAsync();

        var secondRunAt = firstRunAt.AddDays(1);
        await AdmissionDataSeeder.ReconcileAsync(
            context,
            secondRunAt,
            CancellationToken.None);
        await context.SaveChangesAsync();

        var trackAfterRerun = await context.Tracks.SingleAsync(candidate =>
            candidate.Id == trackDefinition.Id);
        Assert.Null(trackAfterRerun.Description);
        Assert.Empty(trackAfterRerun.PrerequisiteTopics);
        Assert.False(trackAfterRerun.IsActive);

        var branchAfterRerun = await context.Branches.SingleAsync(candidate =>
            candidate.Id == AdmissionSeedIds.SmartVillageBranchId);
        Assert.Equal("Admin renamed Smart Village", branchAfterRerun.Name);
        Assert.Null(branchAfterRerun.Governorate);
        Assert.False(branchAfterRerun.IsActive);

        Assert.Equal(3, context.ProgramDocumentRequirements.Count());
        Assert.DoesNotContain(
            context.ProgramDocumentRequirements,
            requirement =>
                requirement.DocumentType == DocumentType.MilitaryCertificate &&
                requirement.RequiredForGender == Gender.Male);

        Assert.Equal(31, context.Tracks.Count());
        Assert.Equal(12, context.Branches.Count());
    }

    [Fact]
    public async Task Explicit_reconciliation_rerun_preserves_intentionally_empty_requirement_set()
    {
        var options = new DbContextOptionsBuilder<TestAdmissionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new TestAdmissionDbContext(options);

        var firstRunAt = new DateTimeOffset(
            2026,
            8,
            12,
            10,
            0,
            0,
            TimeSpan.Zero);

        await AdmissionDataSeeder.ReconcileAsync(
            context,
            firstRunAt,
            CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.Equal(4, context.ProgramDocumentRequirements.Count());

        context.ProgramDocumentRequirements.RemoveRange(
            context.ProgramDocumentRequirements);
        await context.SaveChangesAsync();

        Assert.Empty(context.ProgramDocumentRequirements);
        Assert.Single(context.Programs);

        await AdmissionDataSeeder.ReconcileAsync(
            context,
            firstRunAt.AddDays(1),
            CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.Empty(context.ProgramDocumentRequirements);
        Assert.Single(context.Programs);
        Assert.Equal(31, context.Tracks.Count());
        Assert.Equal(12, context.Branches.Count());
    }

    private sealed class TestAdmissionDbContext : DbContext, IAdmissionDbContext
    {
        public TestAdmissionDbContext(
            DbContextOptions<TestAdmissionDbContext> options)
            : base(options)
        {
        }

        public DbSet<Institution> Institutions => Set<Institution>();
        public DbSet<Program> Programs => Set<Program>();
        public DbSet<AdmissionCycle> AdmissionCycles => Set<AdmissionCycle>();
        public DbSet<CycleEligibilityRule> CycleEligibilityRules => Set<CycleEligibilityRule>();
        public DbSet<Track> Tracks => Set<Track>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<TrackBranchOffering> TrackBranchOfferings => Set<TrackBranchOffering>();
        public DbSet<EduFlowAI.Admission.Domain.Entities.Application> Applications =>
            Set<EduFlowAI.Admission.Domain.Entities.Application>();
        public DbSet<ApplicationPreference> ApplicationPreferences =>
            Set<ApplicationPreference>();
        public DbSet<EligibilityResult> EligibilityResults => Set<EligibilityResult>();
        public DbSet<SimulatedStageResult> SimulatedStageResults =>
            Set<SimulatedStageResult>();
        public DbSet<ProgramDocumentRequirement> ProgramDocumentRequirements =>
            Set<ProgramDocumentRequirement>();
        public DbSet<EnrollmentTaskLookup> EnrollmentTaskLookups =>
            Set<EnrollmentTaskLookup>();
        public DbSet<ApplicationEnrollmentTask> ApplicationEnrollmentTasks =>
            Set<ApplicationEnrollmentTask>();
    }
}
