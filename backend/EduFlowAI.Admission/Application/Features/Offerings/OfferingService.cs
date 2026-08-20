using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Application.Features.Configuration.Common;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Infrastructure.Seeding;
using EduFlowAI.Shared.Kernel.Common;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EduFlowAI.Admission.Application.Features.Offerings;

public sealed class OfferingDto
{
    public Guid Id { get; init; }
    public Guid CycleId { get; init; }
    public Guid TrackId { get; init; }
    public string TrackName { get; init; } = string.Empty;
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public int Capacity { get; init; }
}

public sealed class CreateOfferingRequest
{
    public Guid TrackId { get; init; }
    public Guid BranchId { get; init; }

    [Range(1, int.MaxValue)]
    public int Capacity { get; init; }
}

public sealed class UpdateOfferingRequest
{
    [Range(1, int.MaxValue)]
    public int Capacity { get; init; }
}

public interface IOfferingService
{
    Task<Result<OfferingDto>> CreateOfferingAsync(
        Guid cycleId,
        CreateOfferingRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<OfferingDto>> UpdateOfferingAsync(
        Guid cycleId,
        Guid offeringId,
        UpdateOfferingRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteOfferingAsync(
        Guid cycleId,
        Guid offeringId,
        CancellationToken cancellationToken = default);
}

internal sealed class OfferingService : IOfferingService
{
    private const int MaximumOfferingsPerCycle = 500;

    private readonly IAdmissionDbContext _dbContext;
    private readonly IAdmissionApplicationReader _applicationReader;
    private readonly TimeProvider _timeProvider;
    private readonly AdmissionWriteExecutor _writeExecutor;
    private readonly CycleConfigurationGuard _cycleGuard;

    public OfferingService(
        IAdmissionDbContext dbContext,
        IAdmissionApplicationReader applicationReader,
        TimeProvider timeProvider,
        AdmissionWriteExecutor writeExecutor,
        CycleConfigurationGuard cycleGuard)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(applicationReader);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(writeExecutor);
        ArgumentNullException.ThrowIfNull(cycleGuard);

        _dbContext = dbContext;
        _applicationReader = applicationReader;
        _timeProvider = timeProvider;
        _writeExecutor = writeExecutor;
        _cycleGuard = cycleGuard;
    }

    public async Task<Result<OfferingDto>> CreateOfferingAsync(
        Guid cycleId,
        CreateOfferingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (cycleId == Guid.Empty)
        {
            return Result<OfferingDto>.Failure(
                400,
                "A valid cycle ID is required.");
        }

        if (request is null)
        {
            return Result<OfferingDto>.Failure(
                400,
                "Request body is required.");
        }

        if (request.TrackId == Guid.Empty ||
            request.BranchId == Guid.Empty ||
            request.Capacity <= 0)
        {
            return Result<OfferingDto>.Failure(
                400,
                "A valid track, branch, and positive capacity are required.");
        }

        return await _writeExecutor.ExecuteSerializableAsync(
            cancellationToken,
            async ct =>
            {
                var cycle = await _dbContext.AdmissionCycles
                    .Include(x => x.Program)
                    .SingleOrDefaultAsync(x => x.Id == cycleId, ct);

                if (cycle is null)
                {
                    return Result<OfferingDto>.Failure(
                        404,
                        "Admission cycle was not found.");
                }

                var configurationError = await _cycleGuard.ValidateAsync(cycle, ct);
                if (configurationError is not null)
                {
                    return Result<OfferingDto>.Failure(409, configurationError);
                }

                int offeringCount = await _dbContext.TrackBranchOfferings.CountAsync(
                    x => x.CycleId == cycleId,
                    ct);
                if (offeringCount >= MaximumOfferingsPerCycle)
                {
                    return Result<OfferingDto>.Failure(
                        409,
                        $"A cycle can contain at most {MaximumOfferingsPerCycle} track/branch offerings.");
                }

                var track = await _dbContext.Tracks
                    .SingleOrDefaultAsync(x => x.Id == request.TrackId, ct);

                var branch = await _dbContext.Branches
                    .SingleOrDefaultAsync(x => x.Id == request.BranchId, ct);

                if (track is null || branch is null)
                {
                    return Result<OfferingDto>.Failure(
                        400,
                        "The selected track or branch does not exist.");
                }

                if (track.ProgramId != cycle.ProgramId)
                {
                    return Result<OfferingDto>.Failure(
                        400,
                        "The offered track must belong to the cycle's program.");
                }

                if (!track.IsActive || !branch.IsActive)
                {
                    return Result<OfferingDto>.Failure(
                        400,
                        "Inactive tracks or branches cannot be offered.");
                }

                if (!IsAllowedByOfficialCatalog(
                        track,
                        branch,
                        cycle.ProgramId,
                        cycle.Program.Code))
                {
                    return Result<OfferingDto>.Failure(
                        400,
                        "The selected branch is not an official Intake-47 location for this track.");
                }

                if (await _dbContext.TrackBranchOfferings.AnyAsync(
                        x => x.CycleId == cycleId &&
                             x.TrackId == request.TrackId &&
                             x.BranchId == request.BranchId,
                        ct))
                {
                    return Result<OfferingDto>.Failure(
                        409,
                        "That track and branch offering already exists for this cycle.");
                }

                var now = UtcNow;
                var offering = new TrackBranchOffering
                {
                    Id = Guid.NewGuid(),
                    CycleId = cycleId,
                    TrackId = request.TrackId,
                    BranchId = request.BranchId,
                    Capacity = request.Capacity,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _dbContext.TrackBranchOfferings.Add(offering);
                cycle.UpdatedAt = now;
                await _dbContext.SaveChangesAsync(ct);

                return Result<OfferingDto>.Success(
                    OfferingMapper.Map(offering, track.Name, branch.Name),
                    201,
                    "Cycle offering created successfully.");
            },
            (
                "UX_TrackBranchOfferings_Cycle_Track_Branch",
                "That track and branch offering already exists for this cycle."));
    }

    public async Task<Result<OfferingDto>> UpdateOfferingAsync(
        Guid cycleId,
        Guid offeringId,
        UpdateOfferingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (cycleId == Guid.Empty || offeringId == Guid.Empty)
        {
            return Result<OfferingDto>.Failure(
                400,
                "Valid cycle and offering IDs are required.");
        }

        if (request is null)
        {
            return Result<OfferingDto>.Failure(
                400,
                "Request body is required.");
        }

        if (request.Capacity <= 0)
        {
            return Result<OfferingDto>.Failure(
                400,
                "Capacity must be greater than zero.");
        }

        return await _writeExecutor.ExecuteSerializableAsync(
            cancellationToken,
            async ct =>
            {
                var offering = await _dbContext.TrackBranchOfferings
                    .Include(x => x.Cycle)
                    .Include(x => x.Track)
                    .Include(x => x.Branch)
                    .SingleOrDefaultAsync(
                        x => x.Id == offeringId && x.CycleId == cycleId,
                        ct);

                if (offering is null)
                {
                    return Result<OfferingDto>.Failure(
                        404,
                        "Cycle offering was not found.");
                }

                var configurationError = await _cycleGuard.ValidateAsync(
                    offering.Cycle,
                    ct);
                if (configurationError is not null)
                {
                    return Result<OfferingDto>.Failure(409, configurationError);
                }

                if (offering.Capacity != request.Capacity)
                {
                    var now = UtcNow;
                    offering.Capacity = request.Capacity;
                    offering.UpdatedAt = now;
                    offering.Cycle.UpdatedAt = now;
                    await _dbContext.SaveChangesAsync(ct);
                }

                return Result<OfferingDto>.Success(
                    OfferingMapper.Map(
                        offering,
                        offering.Track.Name,
                        offering.Branch.Name),
                    message: "Cycle offering updated successfully.");
            });
    }

    public async Task<Result<bool>> DeleteOfferingAsync(
        Guid cycleId,
        Guid offeringId,
        CancellationToken cancellationToken = default)
    {
        if (cycleId == Guid.Empty || offeringId == Guid.Empty)
        {
            return Result<bool>.Failure(
                400,
                "Valid cycle and offering IDs are required.");
        }

        return await _writeExecutor.ExecuteSerializableAsync(
            cancellationToken,
            async ct =>
            {
                var offering = await _dbContext.TrackBranchOfferings
                    .Include(x => x.Cycle)
                    .SingleOrDefaultAsync(
                        x => x.Id == offeringId && x.CycleId == cycleId,
                        ct);

                if (offering is null)
                {
                    return Result<bool>.Failure(
                        404,
                        "Cycle offering was not found.");
                }

                var configurationError = await _cycleGuard.ValidateAsync(
                    offering.Cycle,
                    ct);
                if (configurationError is not null)
                {
                    return Result<bool>.Failure(409, configurationError);
                }

                if (await _applicationReader.HasPreferencesForOfferingsAsync(
                        [offering.Id],
                        ct))
                {
                    return Result<bool>.Failure(
                        409,
                        "This offering is referenced by an application preference and cannot be deleted.");
                }

                _dbContext.TrackBranchOfferings.Remove(offering);
                offering.Cycle.UpdatedAt = UtcNow;
                await _dbContext.SaveChangesAsync(ct);

                return Result<bool>.Success(
                    true,
                    message: "Cycle offering deleted successfully.");
            });
    }

    private DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    // Backward-compatible integration overload for application-owned code.
    // The Admission module still owns the catalog rule; callers that only know
    // the program code can delegate using the track's stable ProgramId.
    internal static bool IsAllowedByOfficialCatalog(
        Track track,
        Branch branch,
        string? programCode)
    {
        ArgumentNullException.ThrowIfNull(track);
        return IsAllowedByOfficialCatalog(
            track,
            branch,
            track.ProgramId,
            programCode);
    }

    internal static bool IsAllowedByOfficialCatalog(
        Track track,
        Branch branch,
        Guid programId,
        string? programCode)
    {
        ArgumentNullException.ThrowIfNull(track);
        ArgumentNullException.ThrowIfNull(branch);

        bool isNineMonthProgram =
            programId == AdmissionSeedIds.NineMonthProgramId ||
            StringComparer.OrdinalIgnoreCase.Equals(programCode, "9M");

        if (!isNineMonthProgram)
        {
            return true;
        }

        if (AdmissionLegacyTrackCatalog.IsHistorical(track.Id, track.Name))
        {
            return false;
        }

        var definition = AdmissionTrackSeedCatalog.Find(track.Id, track.Name);

        return definition is null ||
            AdmissionTrackSeedCatalog.IsCanonicalLocation(
                definition,
                branch.Id);
    }
}

internal static class OfferingMapper
{
    public static OfferingDto Map(
        TrackBranchOffering offering,
        string trackName,
        string branchName)
    {
        return new OfferingDto
        {
            Id = offering.Id,
            CycleId = offering.CycleId,
            TrackId = offering.TrackId,
            TrackName = trackName,
            BranchId = offering.BranchId,
            BranchName = branchName,
            Capacity = offering.Capacity
        };
    }
}
