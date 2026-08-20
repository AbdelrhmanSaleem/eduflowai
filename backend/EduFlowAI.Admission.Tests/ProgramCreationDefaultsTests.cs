using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Application.Features.Configuration.Common;
using EduFlowAI.Admission.Application.Features.Programs;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AdmissionApplication = EduFlowAI.Admission.Domain.Entities.Application;
using AdmissionProgram = EduFlowAI.Admission.Domain.Entities.Program;

namespace EduFlowAI.Admission.Tests;

public sealed class ProgramCreationDefaultsTests
{
    [Fact]
    public async Task Create_without_description_persists_an_empty_string()
    {
        await using var dbContext = CreateDbContext();
        Guid institutionId = Guid.NewGuid();

        dbContext.Institutions.Add(new Institution
        {
            Id = institutionId,
            Name = "Information Technology Institute",
            Code = "ITI"
        });
        await dbContext.SaveChangesAsync();

        var request = new CreateProgramRequest
        {
            InstitutionId = institutionId,
            Name = "9-Month Professional Training Program",
            Code = "9M",
            DurationMonths = 9
        };

        var result = await CreateService(dbContext).CreateProgramAsync(request);

        Assert.True(result.IsSuccess);
        var program = await dbContext.Programs.SingleAsync();
        Assert.Equal(string.Empty, program.Description);
    }

    [Fact]
    public async Task Create_normalizes_an_explicit_null_description_to_empty()
    {
        await using var dbContext = CreateDbContext();
        Guid institutionId = Guid.NewGuid();

        dbContext.Institutions.Add(new Institution
        {
            Id = institutionId,
            Name = "Information Technology Institute",
            Code = "ITI"
        });
        await dbContext.SaveChangesAsync();

        var request = new CreateProgramRequest
        {
            InstitutionId = institutionId,
            Name = "Intensive Code Camp",
            Code = "ICC",
            DurationMonths = 4,
            Description = null!
        };

        var result = await CreateService(dbContext).CreateProgramAsync(request);

        Assert.True(result.IsSuccess);
        var program = await dbContext.Programs.SingleAsync();
        Assert.Equal(string.Empty, program.Description);
    }

    private static ProgramConfigurationService CreateService(
        TestAdmissionDbContext dbContext)
    {
        return new ProgramConfigurationService(
            dbContext,
            new EmptyApplicationReader(),
            TimeProvider.System,
            new AdmissionWriteExecutor(dbContext));
    }

    private static TestAdmissionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestAdmissionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestAdmissionDbContext(options);
    }

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
    }
}
