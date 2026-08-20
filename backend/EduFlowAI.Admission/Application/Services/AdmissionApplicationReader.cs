using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EduFlowAI.Admission.Application.Services;

public sealed class AdmissionApplicationReader : IAdmissionApplicationReader
{
    private readonly IAdmissionDbContext _dbContext;

    public AdmissionApplicationReader(IAdmissionDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<bool> IsInstitutionConfigurationLockedAsync(
        Guid institutionId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.AdmissionCycles
            .AsNoTracking()
            .AnyAsync(
                cycle =>
                    cycle.Program.InstitutionId == institutionId &&
                    (cycle.Status == CycleStatus.Active ||
                     _dbContext.Applications.Any(application => application.CycleId == cycle.Id)),
                cancellationToken);
    }

    public Task<bool> IsProgramConfigurationLockedAsync(
        Guid programId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.AdmissionCycles
            .AsNoTracking()
            .AnyAsync(
                cycle =>
                    cycle.ProgramId == programId &&
                    (cycle.Status == CycleStatus.Active ||
                     _dbContext.Applications.Any(application => application.CycleId == cycle.Id)),
                cancellationToken);
    }

    public Task<bool> IsTrackConfigurationLockedAsync(
        Guid trackId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TrackBranchOfferings
            .AsNoTracking()
            .AnyAsync(
                offering =>
                    offering.TrackId == trackId &&
                    (offering.Cycle.Status == CycleStatus.Active ||
                     _dbContext.Applications.Any(application => application.CycleId == offering.CycleId)),
                cancellationToken);
    }

    public Task<bool> IsBranchConfigurationLockedAsync(
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TrackBranchOfferings
            .AsNoTracking()
            .AnyAsync(
                offering =>
                    offering.BranchId == branchId &&
                    (offering.Cycle.Status == CycleStatus.Active ||
                     _dbContext.Applications.Any(application => application.CycleId == offering.CycleId)),
                cancellationToken);
    }

    public Task<bool> HasApplicationsForCycleAsync(
        Guid cycleId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Applications
            .AsNoTracking()
            .AnyAsync(application => application.CycleId == cycleId, cancellationToken);
    }

    public Task<bool> HasPreferencesForOfferingsAsync(
        IReadOnlyCollection<Guid> offeringIds,
        CancellationToken cancellationToken = default)
    {
        if (offeringIds.Count == 0)
        {
            return Task.FromResult(false);
        }

        return _dbContext.ApplicationPreferences
            .AsNoTracking()
            .AnyAsync(
                preference => offeringIds.Contains(preference.TrackBranchOfferingId),
                cancellationToken);
    }

    public Task<int> CountApplicationsAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Applications
            .AsNoTracking()
            .CountAsync(cancellationToken);
    }
}
