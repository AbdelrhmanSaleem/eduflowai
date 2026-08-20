namespace EduFlowAI.Admission.Application.Interfaces.Services;

/// <summary>
/// Read-only application-side facts needed by Admission configuration.
/// This contract prevents configuration services from owning application workflow.
/// </summary>
public interface IAdmissionApplicationReader
{
    Task<bool> IsInstitutionConfigurationLockedAsync(
        Guid institutionId,
        CancellationToken cancellationToken = default);

    Task<bool> IsProgramConfigurationLockedAsync(
        Guid programId,
        CancellationToken cancellationToken = default);

    Task<bool> IsTrackConfigurationLockedAsync(
        Guid trackId,
        CancellationToken cancellationToken = default);

    Task<bool> IsBranchConfigurationLockedAsync(
        Guid branchId,
        CancellationToken cancellationToken = default);

    Task<bool> HasApplicationsForCycleAsync(
        Guid cycleId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPreferencesForOfferingsAsync(
        IReadOnlyCollection<Guid> offeringIds,
        CancellationToken cancellationToken = default);

    Task<int> CountApplicationsAsync(CancellationToken cancellationToken = default);
}
