using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace EduFlowAI.Admission.Tests;

public sealed class AdmissionSeedingGuardTests
{
    [Fact]
    public void AddAdmissionDataSeeding_registers_startup_hosted_service()
    {
        var services = new ServiceCollection();

        services.AddAdmissionDataSeeding();

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(IHostedService));
        Assert.Equal(
            typeof(AdmissionDataSeedHostedService),
            descriptor.ImplementationType);
    }

    [Fact]
    public async Task Startup_hosted_service_reaches_admission_data_seeder_flow()
    {
        var options = new DbContextOptionsBuilder<StartupProbeAdmissionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new StartupProbeAdmissionDbContext(options);

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment
        {
            ApplicationName = "EduFlowAI.Api"
        });
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IAdmissionDbContext>(context);
        services.AddAdmissionDataSeeding();

        await using var provider = services.BuildServiceProvider();
        var hostedService = Assert.Single(provider.GetServices<IHostedService>());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => hostedService.StartAsync(CancellationToken.None));

        Assert.Contains("PostgreSQL EF Core provider", exception.Message);
    }

    [Fact]
    public async Task Explicit_seed_preflight_allows_postgresql_context_without_compiled_migrations()
    {
        var options = new DbContextOptionsBuilder<PreflightDbContext>()
            .UseNpgsql(
                "Host=127.0.0.1;Database=unused;Username=unused;Password=unused")
            .Options;
        await using var context = new PreflightDbContext(options);

        Assert.Empty(context.Database.GetMigrations());

        await AdmissionDataSeeder.ValidateDatabasePreconditionsAsync(
            context.Database,
            CancellationToken.None);
    }

    [Fact]
    public async Task Worker_host_cannot_run_admission_seeding()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment
        {
            ApplicationName = "EduFlowAI.Worker"
        });
        await using var provider = services.BuildServiceProvider();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.SeedAdmissionDataAsync());

        Assert.Contains("EduFlowAI.Api", exception.Message);
        Assert.Contains("EduFlowAI.Worker", exception.Message);
    }

    private sealed class PreflightDbContext : DbContext
    {
        public PreflightDbContext(DbContextOptions<PreflightDbContext> options)
            : base(options)
        {
        }
    }

    private sealed class StartupProbeAdmissionDbContext : DbContext, IAdmissionDbContext
    {
        public StartupProbeAdmissionDbContext(
            DbContextOptions<StartupProbeAdmissionDbContext> options)
            : base(options)
        {
        }

        public DbSet<Institution> Institutions => Set<Institution>();
        public DbSet<EduFlowAI.Admission.Domain.Entities.Program> Programs =>
            Set<EduFlowAI.Admission.Domain.Entities.Program>();
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

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = string.Empty;
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}
