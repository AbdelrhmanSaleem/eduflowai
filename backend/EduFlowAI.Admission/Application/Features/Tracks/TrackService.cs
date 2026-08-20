using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Application.Features.Configuration.Common;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;
using EduFlowAI.Admission.Infrastructure.Seeding;
using EduFlowAI.Shared.Kernel.Common;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EduFlowAI.Admission.Application.Features.Tracks;

public sealed class BranchOfferingDto
{
    public Guid OfferingId { get; init; }
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public string? Governorate { get; init; }
    public int Capacity { get; init; }
}

public sealed class TrackLocationDto
{
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public string? Governorate { get; init; }
}

public sealed class TrackDto
{
    public Guid Id { get; init; }
    public Guid ProgramId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public IReadOnlyList<string> PrerequisiteTopics { get; init; } = Array.Empty<string>();
    public bool IsActive { get; init; }
    public bool IsOfficialIntake47 { get; init; }
    public Guid? OfficialTrackId { get; init; }
    public string? OfficialTrackUrl { get; init; }
    public int? Intake { get; init; }
    public int? Year { get; init; }
    public string? Category { get; init; }
    public int? TotalHours { get; init; }
    public string? MinimumGrade { get; init; }
    public string? EligibilitySummary { get; init; }
    public int? GraduationYearLimitYears { get; init; }
    public IReadOnlyList<TrackLocationDto> Locations { get; init; } = Array.Empty<TrackLocationDto>();
    public IReadOnlyList<BranchOfferingDto> Offerings { get; init; } = Array.Empty<BranchOfferingDto>();
}

public sealed class CreateTrackRequest
{
    public Guid ProgramId { get; init; }

    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; init; }

    [MaxLength(50)]
    public string[] PrerequisiteTopics { get; init; } = [];

    public bool IsActive { get; init; } = true;
}

public sealed class UpdateTrackRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; init; }

    [MaxLength(50)]
    public string[] PrerequisiteTopics { get; init; } = [];

    public bool IsActive { get; init; } = true;
}

public interface ITrackService
{
    Task<IReadOnlyList<TrackDto>> GetPublicTracksAsync(
        Guid? cycleId = null,   // optional cycleId parameter
        CancellationToken cancellationToken = default);

    Task<TrackDto?> GetPublicTrackAsync(
        Guid trackId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrackDto>> GetAdminTracksAsync(
        CancellationToken cancellationToken = default);

    Task<Result<TrackDto>> CreateTrackAsync(
        CreateTrackRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<TrackDto>> UpdateTrackAsync(
        Guid trackId,
        UpdateTrackRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed class TrackService : ITrackService
{
    private const string NineMonthProgramCode = "9M";

    private readonly IAdmissionDbContext _dbContext;
    private readonly IAdmissionApplicationReader _applicationReader;
    private readonly TimeProvider _timeProvider;
    private readonly AdmissionWriteExecutor _writeExecutor;

    public TrackService(
        IAdmissionDbContext dbContext,
        IAdmissionApplicationReader applicationReader,
        TimeProvider timeProvider,
        AdmissionWriteExecutor writeExecutor)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(applicationReader);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(writeExecutor);

        _dbContext = dbContext;
        _applicationReader = applicationReader;
        _timeProvider = timeProvider;
        _writeExecutor = writeExecutor;
    }

    public async Task<IReadOnlyList<TrackDto>> GetPublicTracksAsync(
        Guid? cycleId = null,
        CancellationToken cancellationToken = default)
    {
        var offeringsQuery = _dbContext.TrackBranchOfferings
            .AsNoTracking()
            .Include(offering => offering.Track)
                .ThenInclude(track => track.Program)
            .Include(offering => offering.Branch)
            .Where(offering =>
                offering.Cycle.Status == CycleStatus.Active &&
                offering.Track.IsActive &&
                offering.Branch.IsActive);

        if (cycleId is not null)
        {
            offeringsQuery = offeringsQuery.Where(
                offering => offering.CycleId == cycleId.Value);
        }

        var offerings = await offeringsQuery
            .OrderBy(offering => offering.Track.Name)
            .ThenBy(offering => offering.Branch.Name)
            .ToListAsync(cancellationToken);

        offerings = offerings
            .Where(offering => IsPublicOfferingAllowed(
                offering.Track,
                offering.Branch,
                offering.Track.ProgramId,
                offering.Track.Program.Code))
            .ToList();

        var locationsById = await LoadLocationLookupAsync(
            cancellationToken);

        return offerings
            .GroupBy(offering => offering.TrackId)
            .Select(group => MapTrack(
                group.First().Track,
                group.Select(MapBranchOffering).ToList(),
                IsNineMonthProgram(
                    group.First().Track.ProgramId,
                    group.First().Track.Program.Code),
                locationsById))
            .OrderBy(track => track.Name)
            .ToList();
    }

    public async Task<TrackDto?> GetPublicTrackAsync(
        Guid trackId,
        CancellationToken cancellationToken = default)
    {
        var track = await _dbContext.Tracks
            .AsNoTracking()
            .Include(candidate => candidate.Program)
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == trackId &&
                    candidate.IsActive,
                cancellationToken);

        if (track is null)
        {
            return null;
        }

        bool isNineMonthProgram = IsNineMonthProgram(
            track.ProgramId,
            track.Program.Code);

        if (isNineMonthProgram &&
            AdmissionLegacyTrackCatalog.IsHistorical(track.Id, track.Name))
        {
            return null;
        }

        var offerings = await _dbContext.TrackBranchOfferings
            .AsNoTracking()
            .Include(offering => offering.Branch)
            .Where(offering =>
                offering.TrackId == trackId &&
                offering.Cycle.Status == CycleStatus.Active &&
                offering.Track.IsActive &&
                offering.Branch.IsActive)
            .OrderBy(offering => offering.Branch.Name)
            .ToListAsync(cancellationToken);

        offerings = offerings
            .Where(offering => IsPublicOfferingAllowed(
                track,
                offering.Branch,
                track.ProgramId,
                track.Program.Code))
            .ToList();

        if (offerings.Count == 0)
        {
            return null;
        }

        var locationsById = await LoadLocationLookupAsync(
            cancellationToken);

        return MapTrack(
            track,
            offerings.Select(MapBranchOffering).ToList(),
            isNineMonthProgram,
            locationsById);
    }

    public async Task<IReadOnlyList<TrackDto>> GetAdminTracksAsync(
        CancellationToken cancellationToken = default)
    {
        var tracks = await _dbContext.Tracks
            .AsNoTracking()
            .Include(track => track.Program)
            .OrderBy(track => track.Name)
            .ToListAsync(cancellationToken);

        var locationsById = await LoadLocationLookupAsync(
            cancellationToken);

        return tracks
            .Select(track => MapTrack(
                track,
                Array.Empty<BranchOfferingDto>(),
                IsNineMonthProgram(track.ProgramId, track.Program.Code),
                locationsById))
            .OrderBy(track => track.Name)
            .ToList();
    }

    public async Task<Result<TrackDto>> CreateTrackAsync(
        CreateTrackRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Result<TrackDto>.Failure(400, "Request body is required.");
        }

        if (request.ProgramId == Guid.Empty)
        {
            return Result<TrackDto>.Failure(
                400,
                "A valid program ID is required.");
        }

        string name = AdmissionConfigurationText.NormalizeRequired(request.Name);
        string? description = AdmissionConfigurationText.NormalizeOptional(
            request.Description);

        if (!AdmissionConfigurationText.IsRequiredValid(name, 200) ||
            description?.Length > 4000)
        {
            return Result<TrackDto>.Failure(
                400,
                "Track name is required and must not exceed 200 characters; description must not exceed 4000 characters.");
        }

        var topicValidation = TrackTopicValidator.Validate(
            request.PrerequisiteTopics);

        if (!topicValidation.IsValid)
        {
            return Result<TrackDto>.Failure(
                400,
                topicValidation.ErrorMessage);
        }

        return await _writeExecutor.ExecuteAsync(
            cancellationToken,
            async ct =>
            {
                var programCode = await _dbContext.Programs
                    .Where(program => program.Id == request.ProgramId)
                    .Select(program => program.Code)
                    .SingleOrDefaultAsync(ct);

                if (programCode is null)
                {
                    return Result<TrackDto>.Failure(
                        404,
                        "Program was not found.");
                }

                if (await _dbContext.Tracks.AnyAsync(
                        x =>
                            x.ProgramId == request.ProgramId &&
                            x.Name.ToUpper() == name.ToUpper(),
                        ct))
                {
                    return Result<TrackDto>.Failure(
                        409,
                        "A track with the same name already exists in this program.");
                }

                var track = new Track
                {
                    Id = Guid.NewGuid(),
                    ProgramId = request.ProgramId,
                    Name = name,
                    Description = description,
                    PrerequisiteTopics = topicValidation.Topics,
                    IsActive = request.IsActive,
                    CreatedAt = UtcNow,
                    UpdatedAt = UtcNow
                };

                _dbContext.Tracks.Add(track);
                await _dbContext.SaveChangesAsync(ct);

                return Result<TrackDto>.Success(
                    MapTrack(
                        track,
                        Array.Empty<BranchOfferingDto>(),
                        IsNineMonthProgram(request.ProgramId, programCode)),
                    statusCode: 201,
                    message: "Track created successfully.");
            },
            (
                "UX_Tracks_Program_Name",
                "A track with the same name already exists in this program."));
    }

    public async Task<Result<TrackDto>> UpdateTrackAsync(
        Guid trackId,
        UpdateTrackRequest request,
        CancellationToken cancellationToken = default)
    {
        if (trackId == Guid.Empty)
        {
            return Result<TrackDto>.Failure(
                400,
                "A valid track ID is required.");
        }

        if (request is null)
        {
            return Result<TrackDto>.Failure(400, "Request body is required.");
        }

        string name = AdmissionConfigurationText.NormalizeRequired(request.Name);
        string? description = AdmissionConfigurationText.NormalizeOptional(
            request.Description);

        if (!AdmissionConfigurationText.IsRequiredValid(name, 200) ||
            description?.Length > 4000)
        {
            return Result<TrackDto>.Failure(
                400,
                "Track name is required and must not exceed 200 characters; description must not exceed 4000 characters.");
        }

        var topicValidation = TrackTopicValidator.Validate(
            request.PrerequisiteTopics);

        if (!topicValidation.IsValid)
        {
            return Result<TrackDto>.Failure(
                400,
                topicValidation.ErrorMessage);
        }

        return await _writeExecutor.ExecuteSerializableAsync(
            cancellationToken,
            async ct =>
            {
                var track = await _dbContext.Tracks
                    .Include(candidate => candidate.Program)
                    .SingleOrDefaultAsync(
                        candidate => candidate.Id == trackId,
                        ct);

                if (track is null)
                {
                    return Result<TrackDto>.Failure(
                        404,
                        "Track was not found.");
                }

                if (await _applicationReader.IsTrackConfigurationLockedAsync(
                        trackId,
                        ct))
                {
                    return Result<TrackDto>.Failure(
                        409,
                        "Track configuration is locked because it belongs to an Active cycle or a cycle that already has applications.");
                }

                if (await _dbContext.Tracks.AnyAsync(
                        x =>
                            x.Id != trackId &&
                            x.ProgramId == track.ProgramId &&
                            x.Name.ToUpper() == name.ToUpper(),
                        ct))
                {
                    return Result<TrackDto>.Failure(
                        409,
                        "A track with the same name already exists in this program.");
                }

                track.Name = name;
                track.Description = description;
                track.PrerequisiteTopics = topicValidation.Topics;
                track.IsActive = request.IsActive;
                track.UpdatedAt = UtcNow;

                await _dbContext.SaveChangesAsync(ct);

                return Result<TrackDto>.Success(
                    MapTrack(
                        track,
                        Array.Empty<BranchOfferingDto>(),
                        IsNineMonthProgram(track.ProgramId, track.Program.Code)),
                    message: "Track updated successfully.");
            },
            (
                "UX_Tracks_Program_Name",
                "A track with the same name already exists in this program."));
    }

    private DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    private static TrackDto MapTrack(
        Track track,
        IReadOnlyList<BranchOfferingDto> offerings,
        bool isNineMonthProgram,
        IReadOnlyDictionary<Guid, TrackLocationDto>? locationsById = null)
    {
        var definition = isNineMonthProgram
            ? AdmissionTrackSeedCatalog.Find(track.Id, track.Name)
            : null;

        var locations = definition is not null
            ? definition.Locations
                .Select(location => ResolveLocation(
                    location,
                    locationsById))
                .ToList()
            : offerings
                .DistinctBy(offering => offering.BranchId)
                .Select(offering => new TrackLocationDto
                {
                    BranchId = offering.BranchId,
                    BranchName = offering.BranchName,
                    Governorate = offering.Governorate
                })
                .ToList();

        return new TrackDto
        {
            Id = track.Id,
            ProgramId = track.ProgramId,
            Name = track.Name,
            Description = track.Description,
            PrerequisiteTopics = track.PrerequisiteTopics.ToList(),
            IsActive = track.IsActive,
            IsOfficialIntake47 = definition is not null,
            OfficialTrackId = definition?.OfficialTrackId,
            OfficialTrackUrl = definition?.OfficialTrackUrl,
            Intake = definition is null ? null : AdmissionTrackSeedCatalog.Intake,
            Year = definition is null ? null : AdmissionTrackSeedCatalog.Year,
            Category = definition?.Category,
            TotalHours = definition?.TotalHours,
            MinimumGrade = definition?.MinimumGrade,
            EligibilitySummary = definition?.EligibilitySummary,
            GraduationYearLimitYears = definition?.MaxYearsSinceGraduation,
            Locations = locations,
            Offerings = offerings
        };
    }

    private async Task<IReadOnlyDictionary<Guid, TrackLocationDto>>
        LoadLocationLookupAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Branches
            .AsNoTracking()
            .ToDictionaryAsync(
                branch => branch.Id,
                branch => new TrackLocationDto
                {
                    BranchId = branch.Id,
                    BranchName = branch.Name,
                    Governorate = branch.Governorate
                },
                cancellationToken);
    }

    private static TrackLocationDto ResolveLocation(
        string canonicalBranchName,
        IReadOnlyDictionary<Guid, TrackLocationDto>? locationsById)
    {
        var seed = AdmissionBranchSeedCatalog.FindByName(canonicalBranchName)
            ?? throw new InvalidOperationException(
                $"Official Intake-47 location '{canonicalBranchName}' has no branch definition.");

        if (locationsById is not null &&
            locationsById.TryGetValue(seed.Id, out var currentLocation))
        {
            return currentLocation;
        }

        return new TrackLocationDto
        {
            BranchId = seed.Id,
            BranchName = seed.Name,
            Governorate = seed.Governorate
        };
    }

    private static bool IsNineMonthProgram(Guid programId, string? programCode) =>
        programId == AdmissionSeedIds.NineMonthProgramId ||
        StringComparer.OrdinalIgnoreCase.Equals(
            programCode,
            NineMonthProgramCode);

    internal static bool IsPublicOfferingAllowed(
        Track track,
        Branch branch,
        Guid programId,
        string? programCode)
    {
        if (!IsNineMonthProgram(programId, programCode))
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

    private static BranchOfferingDto MapBranchOffering(
        TrackBranchOffering offering)
    {
        return new BranchOfferingDto
        {
            OfferingId = offering.Id,
            BranchId = offering.BranchId,
            BranchName = offering.Branch.Name,
            Governorate = offering.Branch.Governorate,
            Capacity = offering.Capacity
        };
    }
}
