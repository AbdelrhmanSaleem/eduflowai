using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Application.Features.Branches;
using EduFlowAI.Admission.Application.Features.Configuration.Common;
using EduFlowAI.Admission.Application.Features.Programs;
using EduFlowAI.Admission.Application.Features.Tracks;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;
using EduFlowAI.Admission.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using AdmissionProgram = EduFlowAI.Admission.Domain.Entities.Program;

namespace EduFlowAI.Admission.Tests;

public sealed class TrackServiceCatalogTests
{
    [Fact]
    public async Task Public_catalog_returns_only_active_offerings_and_enriches_official_metadata()
    {
        await using var scenario = await CreateScenarioAsync();
        var service = CreateService(scenario.Context);

        var tracks = await service.GetPublicTracksAsync();

        Assert.Equal(2, tracks.Count);
        Assert.DoesNotContain(
            tracks,
            track => track.Name == "Industrial Automation");

        var digital = Assert.Single(
            tracks,
            track => track.Name == "Digital IC Design");
        Assert.True(digital.IsOfficialIntake47);
        Assert.Equal("Industrial Systems", digital.Category);
        Assert.Equal("Good", digital.MinimumGrade);
        var validOffering = Assert.Single(digital.Offerings);
        Assert.Equal(AdmissionSeedIds.SmartVillageBranchId, validOffering.BranchId);

        var custom = Assert.Single(
            tracks,
            track => track.Id == scenario.CustomTrackId);
        Assert.False(custom.IsOfficialIntake47);
        Assert.Null(custom.Category);
        var customOffering = Assert.Single(custom.Offerings);
        var customLocation = Assert.Single(custom.Locations);
        Assert.Equal(customOffering.BranchId, customLocation.BranchId);
    }

    [Fact]
    public async Task Non_9m_cycle_and_detail_locations_are_derived_from_current_offerings()
    {
        await using var scenario = await CreateScenarioAsync();
        var service = CreateService(scenario.Context);

        var cycleTrack = Assert.Single(await service.GetPublicTracksAsync(
            scenario.CustomCycleId));
        var cycleOffering = Assert.Single(cycleTrack.Offerings);
        var cycleLocation = Assert.Single(cycleTrack.Locations);
        Assert.Equal(cycleOffering.BranchId, cycleLocation.BranchId);
        Assert.Equal(cycleOffering.BranchName, cycleLocation.BranchName);
        Assert.Equal(cycleOffering.Governorate, cycleLocation.Governorate);

        var detailTrack = await service.GetPublicTrackAsync(
            scenario.CustomTrackId);
        Assert.NotNull(detailTrack);
        var detailOffering = Assert.Single(detailTrack.Offerings);
        var detailLocation = Assert.Single(detailTrack.Locations);
        Assert.Equal(detailOffering.BranchId, detailLocation.BranchId);
        Assert.Equal(detailOffering.BranchName, detailLocation.BranchName);
        Assert.Equal(detailOffering.Governorate, detailLocation.Governorate);
    }

    [Fact]
    public async Task Cycle_and_detail_queries_filter_invalid_official_edges_and_require_active_offerings()
    {
        await using var scenario = await CreateScenarioAsync();
        var service = CreateService(scenario.Context);

        var nineMonthTracks = await service.GetPublicTracksAsync(
            scenario.NineMonthCycleId);
        var digital = Assert.Single(nineMonthTracks);
        Assert.Equal("Digital IC Design", digital.Name);
        Assert.Equal("Smart Village", Assert.Single(digital.Offerings).BranchName);

        var customTracks = await service.GetPublicTracksAsync(
            scenario.CustomCycleId);
        Assert.Equal(
            scenario.CustomTrackId,
            Assert.Single(customTracks).Id);

        var industrialDefinition = AdmissionTrackSeedCatalog.All.Single(
            definition => definition.Name == "Industrial Automation");
        Assert.Null(await service.GetPublicTrackAsync(industrialDefinition.Id));

        var customDetail = await service.GetPublicTrackAsync(
            scenario.CustomTrackId);
        Assert.NotNull(customDetail);
        Assert.False(customDetail.IsOfficialIntake47);

        var adminTracks = await service.GetAdminTracksAsync();
        Assert.Contains(
            adminTracks,
            track => track.Id == AdmissionSeedIds.ProfessionalDevelopmentCrmTrackId);
        Assert.Contains(
            adminTracks,
            track => track.Id == AdmissionSeedIds.IntegratedSoftwareArchitectureTrackId);
        Assert.Contains(
            adminTracks,
            track => track.Id == scenario.UnknownNineMonthTrackId);
    }

    [Fact]
    public async Task Official_9m_tracks_remain_admin_editable_and_keep_reference_metadata()
    {
        await using var scenario = await CreateScenarioAsync();
        var service = CreateService(scenario.Context);
        var digital = AdmissionTrackSeedCatalog.All.Single(
            definition => definition.Name == "Digital IC Design");

        var officialResult = await service.UpdateTrackAsync(
            digital.Id,
            new UpdateTrackRequest
            {
                Name = "Renamed official track",
                Description = "Administrator description",
                PrerequisiteTopics = ["Logic"],
                IsActive = true
            });

        Assert.True(officialResult.IsSuccess);
        Assert.Equal("Renamed official track", officialResult.Data.Name);
        Assert.Equal("Administrator description", officialResult.Data.Description);
        Assert.Equal(new[] { "Logic" }, officialResult.Data.PrerequisiteTopics);
        Assert.True(officialResult.Data.IsOfficialIntake47);
        Assert.Equal(digital.OfficialTrackId, officialResult.Data.OfficialTrackId);

        var createNineMonthResult = await service.CreateTrackAsync(
            new CreateTrackRequest
            {
                ProgramId = AdmissionSeedIds.NineMonthProgramId,
                Name = "Admin configured 9M track",
                Description = "Custom track in the same program.",
                PrerequisiteTopics = [],
                IsActive = true
            });

        Assert.True(createNineMonthResult.IsSuccess);
        Assert.False(createNineMonthResult.Data.IsOfficialIntake47);
    }

    [Fact]
    public async Task Official_branch_identity_is_reference_metadata_not_an_edit_lock()
    {
        await using var scenario = await CreateScenarioAsync();
        var service = new BranchService(
            scenario.Context,
            new UnlockedApplicationReader(),
            TimeProvider.System,
            new AdmissionWriteExecutor(scenario.Context));

        var officialResult = await service.UpdateBranchAsync(
            AdmissionSeedIds.SmartVillageBranchId,
            new UpdateBranchRequest
            {
                Name = "Renamed Smart Village",
                Governorate = "Changed",
                IsActive = false
            });

        Assert.True(officialResult.IsSuccess);
        Assert.Equal("Renamed Smart Village", officialResult.Data.Name);
        Assert.False(officialResult.Data.IsActive);
        Assert.True(officialResult.Data.IsOfficialIntake47Location);

        var customResult = await service.UpdateBranchAsync(
            scenario.CustomBranchId,
            new UpdateBranchRequest
            {
                Name = "Renamed custom branch",
                Governorate = "Custom governorate",
                IsActive = false
            });

        Assert.True(customResult.IsSuccess);
        Assert.False(customResult.Data.IsOfficialIntake47Location);
    }

    [Fact]
    public async Task Seeded_parent_rows_follow_normal_admin_configuration_rules()
    {
        await using var scenario = await CreateScenarioAsync();
        var service = new ProgramConfigurationService(
            scenario.Context,
            new UnlockedApplicationReader(),
            TimeProvider.System,
            new AdmissionWriteExecutor(scenario.Context));

        var programs = await service.GetProgramsAsync();
        var nineMonth = programs.Single(program =>
            program.Id == AdmissionSeedIds.NineMonthProgramId);
        Assert.Equal(34, nineMonth.TrackCount);

        var officialUpdate = await service.UpdateProgramAsync(
            nineMonth.Id,
            new UpdateProgramRequest
            {
                Name = "Updated 9-Month Program",
                Code = "9M-UPDATED",
                DurationMonths = 9
            });
        Assert.True(officialUpdate.IsSuccess);

        var institutions = await service.GetInstitutionsAsync();
        var iti = Assert.Single(institutions);

        var institutionUpdate = await service.UpdateInstitutionAsync(
            iti.Id,
            new UpdateInstitutionRequest
            {
                Name = "Updated ITI",
                Code = "ITI-UPDATED"
            });
        Assert.True(institutionUpdate.IsSuccess);
    }

    private static TrackService CreateService(IAdmissionDbContext context) =>
        new(
            context,
            new UnlockedApplicationReader(),
            TimeProvider.System,
            new AdmissionWriteExecutor(context));

    private static async Task<TestScenario> CreateScenarioAsync()
    {
        var options = new DbContextOptionsBuilder<TestAdmissionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(
                InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new TestAdmissionDbContext(options);

        var institution = new Institution
        {
            Id = AdmissionSeedIds.ItiInstitutionId,
            Name = "Information Technology Institute",
            Code = "ITI"
        };
        var nineMonthProgram = new AdmissionProgram
        {
            Id = AdmissionSeedIds.NineMonthProgramId,
            InstitutionId = institution.Id,
            Institution = institution,
            Name = "9-Month Professional Training Program",
            Code = "9M",
            DurationMonths = 9
        };
        var customProgram = new AdmissionProgram
        {
            Id = Guid.NewGuid(),
            InstitutionId = institution.Id,
            Institution = institution,
            Name = "Custom Program",
            Code = "CUSTOM",
            DurationMonths = 4
        };

        context.Institutions.Add(institution);
        context.Programs.AddRange(nineMonthProgram, customProgram);

        var officialTracks = AdmissionTrackSeedCatalog.All
            .Select(definition => new Track
            {
                Id = definition.Id,
                ProgramId = nineMonthProgram.Id,
                Program = nineMonthProgram,
                Name = definition.Name,
                Description = definition.Name == "3D Generalist"
                    ? "Stale inferred description"
                    : definition.Description,
                PrerequisiteTopics = [.. definition.PrerequisiteTopics],
                IsActive = true
            })
            .ToList();
        context.Tracks.AddRange(officialTracks);

        Guid unknownNineMonthTrackId = Guid.NewGuid();
        context.Tracks.AddRange(
            new Track
            {
                Id = AdmissionSeedIds.ProfessionalDevelopmentCrmTrackId,
                ProgramId = nineMonthProgram.Id,
                Program = nineMonthProgram,
                Name = AdmissionLegacyTrackCatalog.ProfessionalDevelopmentCrmName,
                IsActive = true
            },
            new Track
            {
                Id = AdmissionSeedIds.IntegratedSoftwareArchitectureTrackId,
                ProgramId = nineMonthProgram.Id,
                Program = nineMonthProgram,
                Name = AdmissionLegacyTrackCatalog.IntegratedSoftwareArchitectureName,
                IsActive = true
            },
            new Track
            {
                Id = unknownNineMonthTrackId,
                ProgramId = nineMonthProgram.Id,
                Program = nineMonthProgram,
                Name = "Unrecognized historical track",
                IsActive = true
            });

        var customTrack = new Track
        {
            Id = Guid.NewGuid(),
            ProgramId = customProgram.Id,
            Program = customProgram,
            Name = AdmissionLegacyTrackCatalog.ProfessionalDevelopmentCrmName,
            Description = "A non-9M custom track.",
            PrerequisiteTopics = ["Custom topic"],
            IsActive = true
        };
        context.Tracks.Add(customTrack);

        var branches = AdmissionBranchSeedCatalog.All
            .Select(definition => new Branch
            {
                Id = definition.Id,
                Name = definition.Name,
                Governorate = definition.Governorate,
                IsActive = true
            })
            .ToDictionary(branch => branch.Name);
        context.Branches.AddRange(branches.Values);
        var customBranch = new Branch
        {
            Id = Guid.NewGuid(),
            Name = "Custom Branch",
            Governorate = "Custom",
            IsActive = true
        };
        context.Branches.Add(customBranch);

        var nineMonthCycle = new AdmissionCycle
        {
            Id = Guid.NewGuid(),
            ProgramId = nineMonthProgram.Id,
            Program = nineMonthProgram,
            Label = "Intake 47",
            StartDate = new DateOnly(2026, 9, 1),
            DeadlineUtc = new DateTimeOffset(2026, 8, 31, 21, 59, 59, TimeSpan.Zero),
            Status = CycleStatus.Active
        };
        var customCycle = new AdmissionCycle
        {
            Id = Guid.NewGuid(),
            ProgramId = customProgram.Id,
            Program = customProgram,
            Label = "Custom active cycle",
            StartDate = new DateOnly(2026, 9, 1),
            DeadlineUtc = new DateTimeOffset(2026, 8, 31, 21, 59, 59, TimeSpan.Zero),
            Status = CycleStatus.Active
        };
        context.AdmissionCycles.AddRange(nineMonthCycle, customCycle);

        var digital = officialTracks.Single(track =>
            track.Name == "Digital IC Design");
        context.TrackBranchOfferings.AddRange(
            CreateOffering(nineMonthCycle, digital, branches["Smart Village"]),
            CreateOffering(nineMonthCycle, digital, branches["Alexandria"]),
            CreateOffering(customCycle, customTrack, branches["Alexandria"]));

        await context.SaveChangesAsync();

        return new TestScenario(
            context,
            nineMonthCycle.Id,
            customCycle.Id,
            customProgram.Id,
            customTrack.Id,
            unknownNineMonthTrackId,
            customBranch.Id);
    }

    private static TrackBranchOffering CreateOffering(
        AdmissionCycle cycle,
        Track track,
        Branch branch) =>
        new()
        {
            Id = Guid.NewGuid(),
            CycleId = cycle.Id,
            Cycle = cycle,
            TrackId = track.Id,
            Track = track,
            BranchId = branch.Id,
            Branch = branch,
            Capacity = 25
        };

    private sealed record TestScenario(
        TestAdmissionDbContext Context,
        Guid NineMonthCycleId,
        Guid CustomCycleId,
        Guid CustomProgramId,
        Guid CustomTrackId,
        Guid UnknownNineMonthTrackId,
        Guid CustomBranchId) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }

    private sealed class UnlockedApplicationReader : IAdmissionApplicationReader
    {
        public Task<bool> IsInstitutionConfigurationLockedAsync(
            Guid institutionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> IsProgramConfigurationLockedAsync(
            Guid programId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> IsTrackConfigurationLockedAsync(
            Guid trackId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> IsBranchConfigurationLockedAsync(
            Guid branchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasApplicationsForCycleAsync(
            Guid cycleId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> HasPreferencesForOfferingsAsync(
            IReadOnlyCollection<Guid> offeringIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<int> CountApplicationsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    private sealed class TestAdmissionDbContext : DbContext, IAdmissionDbContext
    {
        public TestAdmissionDbContext(
            DbContextOptions<TestAdmissionDbContext> options)
            : base(options)
        {
        }

        public DbSet<Institution> Institutions => Set<Institution>();
        public DbSet<AdmissionProgram> Programs => Set<AdmissionProgram>();
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
