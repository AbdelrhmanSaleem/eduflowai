using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Application.Features.Cycles;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EduFlowAI.Admission.Application.Features.Dashboard;

public sealed class AdminAdmissionDashboardDto
{
    public int InstitutionCount { get; init; }
    public int ProgramCount { get; init; }
    public int ActiveTrackCount { get; init; }
    public int ActiveBranchCount { get; init; }
    public int DraftCycleCount { get; init; }
    public int ClosedCycleCount { get; init; }
    public int ApplicationCount { get; init; }
    public AdmissionCycleDto? ActiveCycle { get; init; }
    public int ActiveCycleOfferingCount { get; init; }
    public int ActiveCycleCapacity { get; init; }
}

public interface IAdmissionDashboardService
{
    Task<AdminAdmissionDashboardDto> GetDashboardAsync(
        Guid? programId = null,
        CancellationToken cancellationToken = default);
}

internal sealed class AdmissionDashboardService : IAdmissionDashboardService
{
    private readonly IAdmissionDbContext _dbContext;
    private readonly IAdmissionApplicationReader _applicationReader;

    public AdmissionDashboardService(
        IAdmissionDbContext dbContext,
        IAdmissionApplicationReader applicationReader)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(applicationReader);

        _dbContext = dbContext;
        _applicationReader = applicationReader;
    }

    public async Task<AdminAdmissionDashboardDto> GetDashboardAsync(
        Guid? programId = null,
        CancellationToken cancellationToken = default)
    {
        AdmissionCycle? activeCycle = null;

        if (programId is { } selectedProgramId && selectedProgramId != Guid.Empty)
        {
            activeCycle = await _dbContext.AdmissionCycles
                .AsNoTracking()
                .Include(x => x.Program)
                .Include(x => x.EligibilityRule)
                .Include(x => x.Offerings)
                    .ThenInclude(x => x.Track)
                .Include(x => x.Offerings)
                    .ThenInclude(x => x.Branch)
                .SingleOrDefaultAsync(
                    x =>
                        x.ProgramId == selectedProgramId &&
                        x.Status == CycleStatus.Active,
                    cancellationToken);
        }

        return new AdminAdmissionDashboardDto
        {
            InstitutionCount =
                await _dbContext.Institutions.CountAsync(cancellationToken),
            ProgramCount =
                await _dbContext.Programs.CountAsync(cancellationToken),
            ActiveTrackCount =
                await _dbContext.Tracks.CountAsync(
                    x => x.IsActive,
                    cancellationToken),
            ActiveBranchCount =
                await _dbContext.Branches.CountAsync(
                    x => x.IsActive,
                    cancellationToken),
            DraftCycleCount =
                await _dbContext.AdmissionCycles.CountAsync(
                    x => x.Status == CycleStatus.Draft,
                    cancellationToken),
            ClosedCycleCount =
                await _dbContext.AdmissionCycles.CountAsync(
                    x => x.Status == CycleStatus.Closed,
                    cancellationToken),
            ApplicationCount =
                await _applicationReader.CountApplicationsAsync(
                    cancellationToken),
            ActiveCycle = activeCycle is null
                ? null
                : AdmissionCycleMapper.Map(activeCycle),
            ActiveCycleOfferingCount = activeCycle?.Offerings.Count ?? 0,
            ActiveCycleCapacity =
                activeCycle?.Offerings.Sum(x => x.Capacity) ?? 0
        };
    }
}
