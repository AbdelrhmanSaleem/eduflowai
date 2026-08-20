using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Application.Features.Configuration.Common;
using EduFlowAI.Admission.Application.Features.Cycles;
using EduFlowAI.Admission.Application.Features.Dashboard;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;
using EduFlowAI.Admission.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AdmissionApplication = EduFlowAI.Admission.Domain.Entities.Application;
using AdmissionProgram = EduFlowAI.Admission.Domain.Entities.Program;

namespace EduFlowAI.Admission.Tests;

public sealed class ActiveCyclePerProgramTests
{
    [Fact]
    public async Task Activation_allows_active_cycles_for_different_programs()
    {
        await using var dbContext = CreateDbContext();
        var firstProgram = await SeedProgramAsync(dbContext, "A");
        var secondProgram = await SeedProgramAsync(dbContext, "B");
        await AddConfiguredCycleAsync(
            dbContext,
            firstProgram,
            "Program A Active",
            CycleStatus.Active);
        Guid secondCycleId = await AddConfiguredCycleAsync(
            dbContext,
            secondProgram,
            "Program B Draft",
            CycleStatus.Draft);

        var result = await CreateCycleService(dbContext)
            .ActivateCycleAsync(secondCycleId);

        Assert.True(result.IsSuccess);
        Assert.Equal(CycleStatus.Active, result.Data.Status);
        Assert.Equal(secondProgram.ProgramId, result.Data.ProgramId);
        Assert.Equal(
            2,
            await dbContext.AdmissionCycles.CountAsync(
                cycle => cycle.Status == CycleStatus.Active));
    }

    [Fact]
    public async Task Activation_rejects_a_second_active_cycle_for_the_same_program()
    {
        await using var dbContext = CreateDbContext();
        var program = await SeedProgramAsync(dbContext, "A");
        await AddConfiguredCycleAsync(
            dbContext,
            program,
            "First Active",
            CycleStatus.Active);
        Guid secondCycleId = await AddConfiguredCycleAsync(
            dbContext,
            program,
            "Second Draft",
            CycleStatus.Draft);

        var result = await CreateCycleService(dbContext)
            .ActivateCycleAsync(secondCycleId);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal(
            "Another admission cycle is already active for this program.",
            result.Message);
    }

    [Fact]
    public async Task Activation_rejects_legacy_noncanonical_9m_offering()
    {
        await using var dbContext = CreateDbContext();
        var program = await SeedOfficialNineMonthProgramAsync(dbContext);
        Guid cycleId = await AddConfiguredCycleAsync(
            dbContext,
            program,
            "Legacy invalid draft",
            CycleStatus.Draft);

        var result = await CreateCycleService(dbContext)
            .ActivateCycleAsync(cycleId);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Contains("official Intake-47 locations", result.Message);
        Assert.Equal(
            CycleStatus.Draft,
            (await dbContext.AdmissionCycles.FindAsync(cycleId))!.Status);
    }

    [Fact]
    public async Task Dashboard_returns_the_active_cycle_for_the_selected_program()
    {
        await using var dbContext = CreateDbContext();
        var firstProgram = await SeedProgramAsync(dbContext, "A");
        var secondProgram = await SeedProgramAsync(dbContext, "B");
        await AddConfiguredCycleAsync(
            dbContext,
            firstProgram,
            "Program A Active",
            CycleStatus.Active);
        Guid selectedCycleId = await AddConfiguredCycleAsync(
            dbContext,
            secondProgram,
            "Program B Active",
            CycleStatus.Active,
            capacity: 40);

        var dashboard = await CreateDashboardService(dbContext)
            .GetDashboardAsync(secondProgram.ProgramId);

        Assert.NotNull(dashboard.ActiveCycle);
        Assert.Equal(selectedCycleId, dashboard.ActiveCycle.Id);
        Assert.Equal(secondProgram.ProgramId, dashboard.ActiveCycle.ProgramId);
        Assert.Equal(1, dashboard.ActiveCycleOfferingCount);
        Assert.Equal(40, dashboard.ActiveCycleCapacity);
    }

    private static AdmissionCycleService CreateCycleService(
        TestAdmissionDbContext dbContext)
    {
        var applicationReader = new EmptyApplicationReader();
        return new AdmissionCycleService(
            dbContext,
            TimeProvider.System,
            new AdmissionWriteExecutor(dbContext),
            new CycleConfigurationGuard(applicationReader));
    }

    private static AdmissionDashboardService CreateDashboardService(
        TestAdmissionDbContext dbContext)
    {
        return new AdmissionDashboardService(
            dbContext,
            new EmptyApplicationReader());
    }

    private static TestAdmissionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestAdmissionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new TestAdmissionDbContext(options);
    }

    private static async Task<SeededProgram> SeedProgramAsync(
        TestAdmissionDbContext dbContext,
        string suffix)
    {
        Guid institutionId = Guid.NewGuid();
        Guid programId = Guid.NewGuid();
        Guid trackId = Guid.NewGuid();
        Guid branchId = Guid.NewGuid();

        dbContext.Institutions.Add(new Institution
        {
            Id = institutionId,
            Name = $"Institution {suffix}",
            Code = $"INST-{suffix}"
        });

        dbContext.Programs.Add(new AdmissionProgram
        {
            Id = programId,
            InstitutionId = institutionId,
            Name = $"Program {suffix}",
            Code = $"PROGRAM-{suffix}",
            Description = string.Empty,
            DurationMonths = 9
        });

        dbContext.Tracks.Add(new Track
        {
            Id = trackId,
            ProgramId = programId,
            Name = $"Track {suffix}",
            Description = $"Track {suffix} description",
            PrerequisiteTopics = ["C#"],
            IsActive = true
        });

        dbContext.Branches.Add(new Branch
        {
            Id = branchId,
            Name = $"Branch {suffix}",
            Governorate = $"Governorate {suffix}",
            IsActive = true
        });

        dbContext.ProgramDocumentRequirements.Add(
            new ProgramDocumentRequirement
            {
                Id = Guid.NewGuid(),
                ProgramId = programId,
                DocumentType = DocumentType.NationalId
            });

        await dbContext.SaveChangesAsync();
        return new SeededProgram(programId, trackId, branchId);
    }

    private static async Task<Guid> AddConfiguredCycleAsync(
        TestAdmissionDbContext dbContext,
        SeededProgram program,
        string label,
        CycleStatus status,
        int capacity = 25)
    {
        Guid cycleId = Guid.NewGuid();

        dbContext.AdmissionCycles.Add(new AdmissionCycle
        {
            Id = cycleId,
            ProgramId = program.ProgramId,
            Label = label,
            StartDate = new DateOnly(2099, 1, 1),
            DeadlineUtc = new DateTimeOffset(
                2099,
                12,
                31,
                23,
                59,
                0,
                TimeSpan.Zero),
            Status = status
        });

        dbContext.CycleEligibilityRules.Add(new CycleEligibilityRule
        {
            Id = Guid.NewGuid(),
            CycleId = cycleId,
            RequiredNationality = "EG",
            RequiredDegreeLevel = "Bachelor",
            MaxYearsSinceGraduation = 5,
            MinGrade = CumulativeGrade.Good
        });

        dbContext.TrackBranchOfferings.Add(new TrackBranchOffering
        {
            Id = Guid.NewGuid(),
            CycleId = cycleId,
            TrackId = program.TrackId,
            BranchId = program.BranchId,
            Capacity = capacity
        });

        await dbContext.SaveChangesAsync();
        return cycleId;
    }

    private static async Task<SeededProgram> SeedOfficialNineMonthProgramAsync(
        TestAdmissionDbContext dbContext)
    {
        var definition = AdmissionTrackSeedCatalog.All.Single(track =>
            track.Name == "Digital IC Design");
        Guid institutionId = Guid.NewGuid();
        Guid programId = Guid.NewGuid();
        var institution = new Institution
        {
            Id = institutionId,
            Name = "Information Technology Institute",
            Code = "ITI-TEST"
        };
        var program = new AdmissionProgram
        {
            Id = programId,
            InstitutionId = institutionId,
            Institution = institution,
            Name = "9-Month Professional Training Program",
            Code = "9M",
            Description = string.Empty,
            DurationMonths = 9
        };
        var track = new Track
        {
            Id = definition.Id,
            ProgramId = programId,
            Program = program,
            Name = definition.Name,
            Description = definition.Description,
            PrerequisiteTopics = [.. definition.PrerequisiteTopics],
            IsActive = true
        };
        var invalidBranch = new Branch
        {
            Id = AdmissionSeedIds.AlexandriaBranchId,
            Name = "Alexandria",
            Governorate = "Alexandria",
            IsActive = true
        };

        dbContext.Institutions.Add(institution);
        dbContext.Programs.Add(program);
        dbContext.Tracks.Add(track);
        dbContext.Branches.Add(invalidBranch);
        dbContext.ProgramDocumentRequirements.Add(
            new ProgramDocumentRequirement
            {
                Id = Guid.NewGuid(),
                ProgramId = programId,
                DocumentType = DocumentType.NationalId
            });

        await dbContext.SaveChangesAsync();
        return new SeededProgram(programId, track.Id, invalidBranch.Id);
    }

    private readonly record struct SeededProgram(
        Guid ProgramId,
        Guid TrackId,
        Guid BranchId);

    private sealed class EmptyApplicationReader : IAdmissionApplicationReader
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
        public DbSet<CycleEligibilityRule> CycleEligibilityRules =>
            Set<CycleEligibilityRule>();
        public DbSet<Track> Tracks => Set<Track>();
        public DbSet<Branch> Branches => Set<Branch>();
        public DbSet<TrackBranchOffering> TrackBranchOfferings =>
            Set<TrackBranchOffering>();
        public DbSet<AdmissionApplication> Applications =>
            Set<AdmissionApplication>();
        public DbSet<ApplicationPreference> ApplicationPreferences =>
            Set<ApplicationPreference>();
        public DbSet<EligibilityResult> EligibilityResults =>
            Set<EligibilityResult>();
        public DbSet<SimulatedStageResult> SimulatedStageResults =>
            Set<SimulatedStageResult>();
        public DbSet<ProgramDocumentRequirement> ProgramDocumentRequirements =>
            Set<ProgramDocumentRequirement>();
        public DbSet<EnrollmentTaskLookup> EnrollmentTaskLookups =>
            Set<EnrollmentTaskLookup>();
        public DbSet<ApplicationEnrollmentTask> ApplicationEnrollmentTasks =>
            Set<ApplicationEnrollmentTask>();

        DatabaseFacade IAdmissionDbContext.Database => Database;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(EduFlowAI.Admission.Infrastructure.Configurations.ProgramConfiguration)
                    .Assembly);
        }
    }
}
