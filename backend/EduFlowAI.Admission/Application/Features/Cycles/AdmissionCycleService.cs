using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Application.Features.Configuration.Common;
using EduFlowAI.Admission.Application.Features.Offerings;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;
using EduFlowAI.Shared.Kernel.Common;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EduFlowAI.Admission.Application.Features.Cycles;

public sealed class CycleEligibilityRuleDto
{
    public Guid Id { get; init; }
    public Guid CycleId { get; init; }
    public string RequiredNationality { get; init; } = string.Empty;
    public string RequiredDegreeLevel { get; init; } = string.Empty;
    public int MaxYearsSinceGraduation { get; init; }
    public CumulativeGrade MinGrade { get; init; }
}

public sealed class AdmissionCycleDto
{
    public Guid Id { get; init; }
    public Guid ProgramId { get; init; }
    public string ProgramName { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateTimeOffset DeadlineUtc { get; init; }
    public CycleStatus Status { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public uint RowVersion { get; init; }
    public CycleEligibilityRuleDto? EligibilityRule { get; init; }
    public IReadOnlyList<OfferingDto> Offerings { get; init; } = Array.Empty<OfferingDto>();
}

public sealed class CreateAdmissionCycleRequest
{
    public Guid ProgramId { get; init; }

    [Required, MaxLength(200)]
    public string Label { get; init; } = string.Empty;

    public DateOnly StartDate { get; init; }
    public DateTimeOffset DeadlineUtc { get; init; }
}

public sealed class UpdateCycleEligibilityRuleRequest
{
    [Required, MaxLength(5)]
    public string RequiredNationality { get; init; } = "EGY";

    [Required, MaxLength(30)]
    public string RequiredDegreeLevel { get; init; } = "Bachelor";

    [Range(0, 100)]
    public int MaxYearsSinceGraduation { get; init; } = 5;

    public CumulativeGrade MinGrade { get; init; } = CumulativeGrade.Good;
}

public interface IAdmissionCycleService
{
    Task<IReadOnlyList<AdmissionCycleDto>> GetCyclesAsync(
        CancellationToken cancellationToken = default);

    Task<Result<AdmissionCycleDto>> CreateCycleAsync(
        CreateAdmissionCycleRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CycleEligibilityRuleDto>> UpsertEligibilityRuleAsync(
        Guid cycleId,
        UpdateCycleEligibilityRuleRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AdmissionCycleDto>> ActivateCycleAsync(
        Guid cycleId,
        CancellationToken cancellationToken = default);

    Task<Result<AdmissionCycleDto>> CloseCycleAsync(
        Guid cycleId,
        CancellationToken cancellationToken = default);
}

internal sealed class AdmissionCycleService : IAdmissionCycleService
{
    private readonly IAdmissionDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly AdmissionWriteExecutor _writeExecutor;
    private readonly CycleConfigurationGuard _cycleGuard;

    public AdmissionCycleService(
        IAdmissionDbContext dbContext,
        TimeProvider timeProvider,
        AdmissionWriteExecutor writeExecutor,
        CycleConfigurationGuard cycleGuard)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(writeExecutor);
        ArgumentNullException.ThrowIfNull(cycleGuard);

        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _writeExecutor = writeExecutor;
        _cycleGuard = cycleGuard;
    }

    public async Task<IReadOnlyList<AdmissionCycleDto>> GetCyclesAsync(
        CancellationToken cancellationToken = default)
    {
        var cycles = await _dbContext.AdmissionCycles
            .AsNoTracking()
            .Include(x => x.Program)
            .Include(x => x.EligibilityRule)
            .Include(x => x.Offerings)
                .ThenInclude(x => x.Track)
            .Include(x => x.Offerings)
                .ThenInclude(x => x.Branch)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return cycles
            .Select(cycle => AdmissionCycleMapper.Map(cycle))
            .ToList();
    }

    public async Task<Result<AdmissionCycleDto>> CreateCycleAsync(
        CreateAdmissionCycleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Result<AdmissionCycleDto>.Failure(
                400,
                "Request body is required.");
        }

        if (request.ProgramId == Guid.Empty)
        {
            return Result<AdmissionCycleDto>.Failure(
                400,
                "A valid program ID is required.");
        }

        if (request.StartDate == default)
        {
            return Result<AdmissionCycleDto>.Failure(
                400,
                "StartDate is required.");
        }

        string label = AdmissionConfigurationText.NormalizeRequired(request.Label);
        if (!AdmissionConfigurationText.IsRequiredValid(label, 200))
        {
            return Result<AdmissionCycleDto>.Failure(
                400,
                "Cycle label is required and must not exceed 200 characters.");
        }

        DateTimeOffset deadlineUtc = request.DeadlineUtc.ToUniversalTime();

        if (deadlineUtc <= UtcNow)
        {
            return Result<AdmissionCycleDto>.Failure(
                400,
                "The application deadline must be in the future.");
        }

        if (request.StartDate >
            DateOnly.FromDateTime(deadlineUtc.UtcDateTime))
        {
            return Result<AdmissionCycleDto>.Failure(
                400,
                "StartDate cannot be after the application deadline.");
        }

        return await _writeExecutor.ExecuteAsync(
            cancellationToken,
            async ct =>
            {
                var program = await _dbContext.Programs
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        x => x.Id == request.ProgramId,
                        ct);

                if (program is null)
                {
                    return Result<AdmissionCycleDto>.Failure(
                        404,
                        "Program was not found.");
                }

                if (await _dbContext.AdmissionCycles.AnyAsync(
                        x =>
                            x.ProgramId == request.ProgramId &&
                            x.Label == label,
                        ct))
                {
                    return Result<AdmissionCycleDto>.Failure(
                        409,
                        "A cycle with the same label already exists for this program.");
                }

                var cycle = new AdmissionCycle
                {
                    Id = Guid.NewGuid(),
                    ProgramId = request.ProgramId,
                    Label = label,
                    StartDate = request.StartDate,
                    DeadlineUtc = deadlineUtc,
                    Status = CycleStatus.Draft,
                    CreatedAt = UtcNow,
                    UpdatedAt = UtcNow
                };

                _dbContext.AdmissionCycles.Add(cycle);
                await _dbContext.SaveChangesAsync(ct);

                return Result<AdmissionCycleDto>.Success(
                    AdmissionCycleMapper.Map(cycle, program.Name),
                    statusCode: 201,
                    message: "Admission cycle created as Draft.");
            },
            (
                "UX_AdmissionCycles_Program_Label",
                "A cycle with the same label already exists for this program."));
    }

    public async Task<Result<CycleEligibilityRuleDto>> UpsertEligibilityRuleAsync(
        Guid cycleId,
        UpdateCycleEligibilityRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (cycleId == Guid.Empty)
        {
            return Result<CycleEligibilityRuleDto>.Failure(
                400,
                "A valid cycle ID is required.");
        }

        if (request is null)
        {
            return Result<CycleEligibilityRuleDto>.Failure(
                400,
                "Request body is required.");
        }

        if (!Enum.IsDefined(request.MinGrade) ||
            request.MinGrade == CumulativeGrade.None)
        {
            return Result<CycleEligibilityRuleDto>.Failure(
                400,
                "Minimum grade must be Acceptable, Good, VeryGood, or Excellent.");
        }

        string nationality = AdmissionConfigurationText.NormalizeCode(
            request.RequiredNationality);

        string degreeLevel = AdmissionConfigurationText.NormalizeRequired(
            request.RequiredDegreeLevel);

        if (!AdmissionConfigurationText.IsRequiredValid(nationality, 5) ||
            !AdmissionConfigurationText.IsRequiredValid(degreeLevel, 30) ||
            request.MaxYearsSinceGraduation is < 0 or > 100)
        {
            return Result<CycleEligibilityRuleDto>.Failure(
                400,
                "Nationality and degree level are required, and graduation recency must be between 0 and 100 years.");
        }

        return await _writeExecutor.ExecuteSerializableAsync(
            cancellationToken,
            async ct =>
            {
                var cycle = await _dbContext.AdmissionCycles.SingleOrDefaultAsync(
                    x => x.Id == cycleId,
                    ct);

                if (cycle is null)
                {
                    return Result<CycleEligibilityRuleDto>.Failure(
                        404,
                        "Admission cycle was not found.");
                }

                var configurationError = await _cycleGuard.ValidateAsync(cycle, ct);
                if (configurationError is not null)
                {
                    return Result<CycleEligibilityRuleDto>.Failure(
                        409,
                        configurationError);
                }

                var rule = await _dbContext.CycleEligibilityRules
                    .SingleOrDefaultAsync(x => x.CycleId == cycleId, ct);

                var now = UtcNow;

                if (rule is null)
                {
                    rule = new CycleEligibilityRule
                    {
                        Id = Guid.NewGuid(),
                        CycleId = cycleId,
                        CreatedAt = now
                    };

                    _dbContext.CycleEligibilityRules.Add(rule);
                }

                rule.RequiredNationality = nationality;
                rule.RequiredDegreeLevel = degreeLevel;
                rule.MaxYearsSinceGraduation =
                    request.MaxYearsSinceGraduation;
                rule.MinGrade = request.MinGrade;
                rule.UpdatedAt = now;
                cycle.UpdatedAt = now;

                await _dbContext.SaveChangesAsync(ct);

                return Result<CycleEligibilityRuleDto>.Success(
                    AdmissionCycleMapper.MapEligibilityRule(rule),
                    message: "Cycle eligibility rule saved successfully.");
            },
            (
                "UX_CycleEligibilityRules_Cycle",
                "An eligibility rule already exists for this cycle. Reload and try again."));
    }

    public async Task<Result<AdmissionCycleDto>> ActivateCycleAsync(
        Guid cycleId,
        CancellationToken cancellationToken = default)
    {
        if (cycleId == Guid.Empty)
        {
            return Result<AdmissionCycleDto>.Failure(
                400,
                "A valid cycle ID is required.");
        }

        return await _writeExecutor.ExecuteSerializableAsync(
            cancellationToken,
            async ct =>
            {
                var cycle = await LoadCycleGraphAsync(cycleId, ct);

                if (cycle is null)
                {
                    return Result<AdmissionCycleDto>.Failure(
                        404,
                        "Admission cycle was not found.");
                }

                if (cycle.Status != CycleStatus.Draft)
                {
                    return Result<AdmissionCycleDto>.Failure(
                        409,
                        "Only a Draft cycle can be activated.");
                }

                if (cycle.DeadlineUtc <= UtcNow)
                {
                    return Result<AdmissionCycleDto>.Failure(
                        400,
                        "The cycle cannot be activated because its deadline has passed.");
                }

                if (cycle.EligibilityRule is null)
                {
                    return Result<AdmissionCycleDto>.Failure(
                        400,
                        "Configure the cycle eligibility rule before activation.");
                }

                if (!await _dbContext.ProgramDocumentRequirements.AnyAsync(
                        x => x.ProgramId == cycle.ProgramId,
                        ct))
                {
                    return Result<AdmissionCycleDto>.Failure(
                        400,
                        "Configure program document requirements before activation.");
                }

                if (cycle.Offerings.Count == 0)
                {
                    return Result<AdmissionCycleDto>.Failure(
                        400,
                        "Configure at least one track/branch offering before activation.");
                }

                if (cycle.Offerings.Any(x =>
                        x.Capacity <= 0 ||
                        !x.Track.IsActive ||
                        !x.Branch.IsActive ||
                        x.Track.ProgramId != cycle.ProgramId ||
                        !OfferingService.IsAllowedByOfficialCatalog(
                            x.Track,
                            x.Branch,
                            cycle.ProgramId,
                            cycle.Program.Code)))
                {
                    return Result<AdmissionCycleDto>.Failure(
                        400,
                        "All offerings must use active tracks and branches, positive capacity, tracks belonging to the cycle's program, and official Intake-47 locations for 9M tracks.");
                }

                if (await _dbContext.AdmissionCycles.AnyAsync(
                        x =>
                            x.Id != cycleId &&
                            x.ProgramId == cycle.ProgramId &&
                            x.Status == CycleStatus.Active,
                        ct))
                {
                    return Result<AdmissionCycleDto>.Failure(
                        409,
                        "Another admission cycle is already active for this program.");
                }

                cycle.Status = CycleStatus.Active;
                cycle.ClosedAt = null;
                cycle.UpdatedAt = UtcNow;

                await _dbContext.SaveChangesAsync(ct);

                return Result<AdmissionCycleDto>.Success(
                    AdmissionCycleMapper.Map(cycle),
                    message: "Admission cycle activated successfully.");
            },
            (
                "uq_cycle_active_per_program",
                "The cycle could not be activated because another cycle for this program became active at the same time."));
    }

    public async Task<Result<AdmissionCycleDto>> CloseCycleAsync(
        Guid cycleId,
        CancellationToken cancellationToken = default)
    {
        if (cycleId == Guid.Empty)
        {
            return Result<AdmissionCycleDto>.Failure(
                400,
                "A valid cycle ID is required.");
        }

        return await _writeExecutor.ExecuteSerializableAsync(
            cancellationToken,
            async ct =>
            {
                var cycle = await LoadCycleGraphAsync(cycleId, ct);

                if (cycle is null)
                {
                    return Result<AdmissionCycleDto>.Failure(
                        404,
                        "Admission cycle was not found.");
                }

                if (cycle.Status != CycleStatus.Active)
                {
                    return Result<AdmissionCycleDto>.Failure(
                        409,
                        "Only an Active cycle can be closed.");
                }

                var now = UtcNow;
                cycle.Status = CycleStatus.Closed;
                cycle.ClosedAt = now;
                cycle.UpdatedAt = now;

                await _dbContext.SaveChangesAsync(ct);

                return Result<AdmissionCycleDto>.Success(
                    AdmissionCycleMapper.Map(cycle),
                    message: "Admission cycle closed successfully.");
            });
    }

    private Task<AdmissionCycle?> LoadCycleGraphAsync(
        Guid cycleId,
        CancellationToken cancellationToken)
    {
        return _dbContext.AdmissionCycles
            .Include(x => x.Program)
            .Include(x => x.EligibilityRule)
            .Include(x => x.Offerings)
                .ThenInclude(x => x.Track)
            .Include(x => x.Offerings)
                .ThenInclude(x => x.Branch)
            .SingleOrDefaultAsync(
                x => x.Id == cycleId,
                cancellationToken);
    }

    private DateTimeOffset UtcNow => _timeProvider.GetUtcNow();
}

internal static class AdmissionCycleMapper
{
    public static AdmissionCycleDto Map(
        AdmissionCycle cycle,
        string? programName = null)
    {
        return new AdmissionCycleDto
        {
            Id = cycle.Id,
            ProgramId = cycle.ProgramId,
            ProgramName = programName ?? cycle.Program?.Name ?? string.Empty,
            Label = cycle.Label,
            StartDate = cycle.StartDate,
            DeadlineUtc = cycle.DeadlineUtc,
            Status = cycle.Status,
            ClosedAt = cycle.ClosedAt,
            RowVersion = cycle.RowVersion,
            EligibilityRule = cycle.EligibilityRule is null
                ? null
                : MapEligibilityRule(cycle.EligibilityRule),
            Offerings = cycle.Offerings
                .Select(x => OfferingMapper.Map(
                    x,
                    x.Track?.Name ?? string.Empty,
                    x.Branch?.Name ?? string.Empty))
                .OrderBy(x => x.TrackName)
                .ThenBy(x => x.BranchName)
                .ToList()
        };
    }

    public static CycleEligibilityRuleDto MapEligibilityRule(
        CycleEligibilityRule rule)
    {
        return new CycleEligibilityRuleDto
        {
            Id = rule.Id,
            CycleId = rule.CycleId,
            RequiredNationality = rule.RequiredNationality,
            RequiredDegreeLevel = rule.RequiredDegreeLevel,
            MaxYearsSinceGraduation = rule.MaxYearsSinceGraduation,
            MinGrade = rule.MinGrade
        };
    }
}
