using EduFlowAI.Admission.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EduFlowAI.Admission.Application.DbContextAbstraction;

public interface IAdmissionDbContext
{
    DbSet<Institution> Institutions { get; }
    DbSet<Program> Programs { get; }
    DbSet<AdmissionCycle> AdmissionCycles { get; }
    DbSet<CycleEligibilityRule> CycleEligibilityRules { get; }
    DbSet<Track> Tracks { get; }
    DbSet<Branch> Branches { get; }
    DbSet<TrackBranchOffering> TrackBranchOfferings { get; }
    DbSet<Domain.Entities.Application> Applications { get; }
    DbSet<ApplicationPreference> ApplicationPreferences { get; }
    DbSet<EligibilityResult> EligibilityResults { get; }
    DbSet<SimulatedStageResult> SimulatedStageResults { get; }
    DbSet<ProgramDocumentRequirement> ProgramDocumentRequirements { get; }
    DbSet<EnrollmentTaskLookup> EnrollmentTaskLookups { get; }
    DbSet<ApplicationEnrollmentTask> ApplicationEnrollmentTasks { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
