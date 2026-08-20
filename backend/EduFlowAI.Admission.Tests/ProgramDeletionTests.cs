using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Application.Features.Configuration.Common;
using EduFlowAI.Admission.Application.Features.Programs;
using EduFlowAI.Admission.Application.Services;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AdmissionApplication = EduFlowAI.Admission.Domain.Entities.Application;
using AdmissionProgram = EduFlowAI.Admission.Domain.Entities.Program;

namespace EduFlowAI.Admission.Tests;

public sealed class ProgramDeletionTests
{
    [Fact]
    public async Task Delete_removes_program_and_all_unlocked_configuration_data()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedProgramAsync(
            dbContext,
            CycleStatus.Draft,
            withApplication: false);
        var service = CreateService(dbContext);

        var result = await service.DeleteProgramAsync(seeded.ProgramId);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);
        Assert.Empty(await dbContext.Programs.ToListAsync());
        Assert.Empty(await dbContext.Tracks.ToListAsync());
        Assert.Empty(await dbContext.AdmissionCycles.ToListAsync());
        Assert.Empty(await dbContext.CycleEligibilityRules.ToListAsync());
        Assert.Empty(await dbContext.TrackBranchOfferings.ToListAsync());
        Assert.Empty(await dbContext.ProgramDocumentRequirements.ToListAsync());
        Assert.Single(await dbContext.Institutions.ToListAsync());
        Assert.Single(await dbContext.Branches.ToListAsync());
    }

    [Fact]
    public async Task Delete_rejects_a_program_with_an_active_cycle()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedProgramAsync(
            dbContext,
            CycleStatus.Active,
            withApplication: false);
        var service = CreateService(dbContext);

        var result = await service.DeleteProgramAsync(seeded.ProgramId);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
        Assert.Single(await dbContext.Programs.ToListAsync());
        Assert.Single(await dbContext.AdmissionCycles.ToListAsync());
    }

    [Fact]
    public async Task Delete_rejects_a_program_with_applications()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedProgramAsync(
            dbContext,
            CycleStatus.Draft,
            withApplication: true);
        var service = CreateService(dbContext);

        var result = await service.DeleteProgramAsync(seeded.ProgramId);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
        Assert.Single(await dbContext.Programs.ToListAsync());
        Assert.Single(await dbContext.Applications.ToListAsync());
    }

    private static ProgramConfigurationService CreateService(
        TestAdmissionDbContext dbContext)
    {
        var applicationReader = new AdmissionApplicationReader(dbContext);
        return new ProgramConfigurationService(
            dbContext,
            applicationReader,
            TimeProvider.System,
            new AdmissionWriteExecutor(dbContext));
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
        CycleStatus cycleStatus,
        bool withApplication)
    {
        Guid institutionId = Guid.NewGuid();
        Guid programId = Guid.NewGuid();
        Guid trackId = Guid.NewGuid();
        Guid branchId = Guid.NewGuid();
        Guid cycleId = Guid.NewGuid();

        dbContext.Institutions.Add(new Institution
        {
            Id = institutionId,
            Name = "Test Institution",
            Code = "TEST"
        });

        dbContext.Programs.Add(new AdmissionProgram
        {
            Id = programId,
            InstitutionId = institutionId,
            Name = "Disposable Program",
            Code = "DELETE-ME",
            Description = string.Empty,
            DurationMonths = 4
        });

        dbContext.Tracks.Add(new Track
        {
            Id = trackId,
            ProgramId = programId,
            Name = "Disposable Track",
            Description = "Test track",
            PrerequisiteTopics = ["C#"],
            IsActive = true
        });

        dbContext.Branches.Add(new Branch
        {
            Id = branchId,
            Name = "Alexandria",
            Governorate = "Alexandria",
            IsActive = true
        });

        dbContext.AdmissionCycles.Add(new AdmissionCycle
        {
            Id = cycleId,
            ProgramId = programId,
            Label = "Disposable Intake",
            StartDate = new DateOnly(2026, 8, 1),
            DeadlineUtc = new DateTimeOffset(2026, 8, 31, 21, 0, 0, TimeSpan.Zero),
            Status = cycleStatus
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
            TrackId = trackId,
            BranchId = branchId,
            Capacity = 25
        });

        dbContext.ProgramDocumentRequirements.Add(
            new ProgramDocumentRequirement
            {
                Id = Guid.NewGuid(),
                ProgramId = programId,
                DocumentType = DocumentType.NationalId
            });

        if (withApplication)
        {
            dbContext.Applications.Add(new AdmissionApplication
            {
                Id = Guid.NewGuid(),
                ApplicantUserId = "applicant-1",
                CycleId = cycleId,
                Status = ApplicationStatus.Draft,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();
        return new SeededProgram(programId);
    }

    private readonly record struct SeededProgram(Guid ProgramId);

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
        public DbSet<AdmissionApplication> Applications => Set<AdmissionApplication>();
        public DbSet<ApplicationPreference> ApplicationPreferences => Set<ApplicationPreference>();
        public DbSet<EligibilityResult> EligibilityResults => Set<EligibilityResult>();
        public DbSet<SimulatedStageResult> SimulatedStageResults => Set<SimulatedStageResult>();
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
