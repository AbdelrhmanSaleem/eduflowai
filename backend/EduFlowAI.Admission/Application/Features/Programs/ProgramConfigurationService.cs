using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Application.Features.Configuration.Common;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Shared.Kernel.Common;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using AdmissionProgram = EduFlowAI.Admission.Domain.Entities.Program;

namespace EduFlowAI.Admission.Application.Features.Programs;

public sealed class InstitutionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public int ProgramCount { get; init; }
}

public sealed class ProgramDto
{
    public Guid Id { get; init; }
    public Guid InstitutionId { get; init; }
    public string InstitutionName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public int DurationMonths { get; init; }
    public int TrackCount { get; init; }
    public int CycleCount { get; init; }
}

public sealed class CreateInstitutionRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required, MaxLength(20)]
    public string Code { get; init; } = string.Empty;
}

public sealed class UpdateInstitutionRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required, MaxLength(20)]
    public string Code { get; init; } = string.Empty;
}

public sealed class CreateProgramRequest
{
    public Guid InstitutionId { get; init; }

    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required, MaxLength(30)]
    public string Code { get; init; } = string.Empty;

    [Range(1, 60)]
    public int DurationMonths { get; init; }

    public string Description { get; init; } = string.Empty;
}

public sealed class UpdateProgramRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required, MaxLength(30)]
    public string Code { get; init; } = string.Empty;

    [Range(1, 60)]
    public int DurationMonths { get; init; }
}

public interface IProgramConfigurationService
{
    Task<IReadOnlyList<InstitutionDto>> GetInstitutionsAsync(
        CancellationToken cancellationToken = default);

    Task<Result<InstitutionDto>> CreateInstitutionAsync(
        CreateInstitutionRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<InstitutionDto>> UpdateInstitutionAsync(
        Guid institutionId,
        UpdateInstitutionRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProgramDto>> GetProgramsAsync(
        CancellationToken cancellationToken = default);

    Task<Result<ProgramDto>> CreateProgramAsync(
        CreateProgramRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ProgramDto>> UpdateProgramAsync(
        Guid programId,
        UpdateProgramRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> DeleteProgramAsync(
        Guid programId,
        CancellationToken cancellationToken = default);
}

internal sealed class ProgramConfigurationService : IProgramConfigurationService
{
    private readonly IAdmissionDbContext _dbContext;
    private readonly IAdmissionApplicationReader _applicationReader;
    private readonly TimeProvider _timeProvider;
    private readonly AdmissionWriteExecutor _writeExecutor;

    public ProgramConfigurationService(
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

    public async Task<IReadOnlyList<InstitutionDto>> GetInstitutionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Institutions
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new InstitutionDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                ProgramCount = x.Programs.Count,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<InstitutionDto>> CreateInstitutionAsync(
        CreateInstitutionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Result<InstitutionDto>.Failure(400, "Request body is required.");
        }

        string name = AdmissionConfigurationText.NormalizeRequired(request.Name);
        string code = AdmissionConfigurationText.NormalizeCode(request.Code);
        if (!AdmissionConfigurationText.IsRequiredValid(name, 200) ||
            !AdmissionConfigurationText.IsRequiredValid(code, 20))
        {
            return Result<InstitutionDto>.Failure(
                400,
                "Institution name is required and must not exceed 200 characters; code is required and must not exceed 20 characters.");
        }

        return await _writeExecutor.ExecuteAsync(
            cancellationToken,
            async ct =>
            {
                if (await _dbContext.Institutions.AnyAsync(
                        x => x.Code.ToUpper() == code,
                        ct))
                {
                    return Result<InstitutionDto>.Failure(
                        409,
                        $"Institution code '{code}' already exists.");
                }

                var institution = new Institution
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Code = code,
                    CreatedAt = UtcNow
                };

                _dbContext.Institutions.Add(institution);
                await _dbContext.SaveChangesAsync(ct);

                return Result<InstitutionDto>.Success(
                    MapInstitution(institution, 0),
                    201,
                    "Institution created successfully.");
            },
            ("UX_Institutions_Code", $"Institution code '{code}' already exists."));
    }

    public async Task<Result<InstitutionDto>> UpdateInstitutionAsync(
        Guid institutionId,
        UpdateInstitutionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (institutionId == Guid.Empty)
        {
            return Result<InstitutionDto>.Failure(
                400,
                "A valid institution ID is required.");
        }

        if (request is null)
        {
            return Result<InstitutionDto>.Failure(400, "Request body is required.");
        }

        string name = AdmissionConfigurationText.NormalizeRequired(request.Name);
        string code = AdmissionConfigurationText.NormalizeCode(request.Code);
        if (!AdmissionConfigurationText.IsRequiredValid(name, 200) ||
            !AdmissionConfigurationText.IsRequiredValid(code, 20))
        {
            return Result<InstitutionDto>.Failure(
                400,
                "Institution name is required and must not exceed 200 characters; code is required and must not exceed 20 characters.");
        }

        return await _writeExecutor.ExecuteSerializableAsync(
            cancellationToken,
            async ct =>
            {
                var institution = await _dbContext.Institutions
                    .SingleOrDefaultAsync(x => x.Id == institutionId, ct);

                if (institution is null)
                {
                    return Result<InstitutionDto>.Failure(
                        404,
                        "Institution was not found.");
                }

                if (await _applicationReader.IsInstitutionConfigurationLockedAsync(
                        institutionId,
                        ct))
                {
                    return Result<InstitutionDto>.Failure(
                        409,
                        "Institution configuration is locked because one of its admission cycles is Active or already has applications.");
                }

                if (await _dbContext.Institutions.AnyAsync(
                        x =>
                            x.Id != institutionId &&
                            x.Code.ToUpper() == code,
                        ct))
                {
                    return Result<InstitutionDto>.Failure(
                        409,
                        $"Institution code '{code}' already exists.");
                }

                institution.Name = name;
                institution.Code = code;
                await _dbContext.SaveChangesAsync(ct);

                int programCount = await _dbContext.Programs.CountAsync(
                    x => x.InstitutionId == institutionId,
                    ct);

                return Result<InstitutionDto>.Success(
                    MapInstitution(institution, programCount),
                    message: "Institution updated successfully.");
            },
            ("UX_Institutions_Code", $"Institution code '{code}' already exists."));
    }

    public async Task<IReadOnlyList<ProgramDto>> GetProgramsAsync(
        CancellationToken cancellationToken = default)
    {
        var programs = await _dbContext.Programs
            .AsNoTracking()
            .Include(program => program.Institution)
            .Include(program => program.Tracks)
            .Include(program => program.AdmissionCycles)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return programs
            .Select(program => MapProgram(
                program,
                program.Institution.Name,
                program.Tracks.Count,
                program.AdmissionCycles.Count))
            .ToList();
    }

    public async Task<Result<ProgramDto>> CreateProgramAsync(
        CreateProgramRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Result<ProgramDto>.Failure(400, "Request body is required.");
        }

        if (request.InstitutionId == Guid.Empty)
        {
            return Result<ProgramDto>.Failure(
                400,
                "A valid institution ID is required.");
        }

        string name = AdmissionConfigurationText.NormalizeRequired(request.Name);
        string code = AdmissionConfigurationText.NormalizeCode(request.Code);
        if (!AdmissionConfigurationText.IsRequiredValid(name, 200) ||
            !AdmissionConfigurationText.IsRequiredValid(code, 30) ||
            request.DurationMonths is < 1 or > 60)
        {
            return Result<ProgramDto>.Failure(
                400,
                "Program name and code are required, and duration must be between 1 and 60 months.");
        }

        var institution = await _dbContext.Institutions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == request.InstitutionId,
                cancellationToken);

        if (institution is null)
        {
            return Result<ProgramDto>.Failure(404, "Institution was not found.");
        }

        return await _writeExecutor.ExecuteAsync(
            cancellationToken,
            async ct =>
            {
                if (await _dbContext.Programs.AnyAsync(
                        x => x.Code.ToUpper() == code,
                        ct))
                {
                    return Result<ProgramDto>.Failure(
                        409,
                        $"Program code '{code}' already exists.");
                }

                var program = new AdmissionProgram
                {
                    Id = Guid.NewGuid(),
                    InstitutionId = request.InstitutionId,
                    Name = name,
                    Code = code,
                    DurationMonths = request.DurationMonths,
                    CreatedAt = UtcNow,
                    Description = request.Description?.Trim() ?? string.Empty
                };

                _dbContext.Programs.Add(program);
                await _dbContext.SaveChangesAsync(ct);

                return Result<ProgramDto>.Success(
                    MapProgram(program, institution.Name, 0, 0),
                    statusCode: 201,
                    message: "Program created successfully.");
            },
            ("UX_Programs_Code", $"Program code '{code}' already exists."));
    }

    public async Task<Result<ProgramDto>> UpdateProgramAsync(
        Guid programId,
        UpdateProgramRequest request,
        CancellationToken cancellationToken = default)
    {
        if (programId == Guid.Empty)
        {
            return Result<ProgramDto>.Failure(
                400,
                "A valid program ID is required.");
        }

        if (request is null)
        {
            return Result<ProgramDto>.Failure(400, "Request body is required.");
        }

        string name = AdmissionConfigurationText.NormalizeRequired(request.Name);
        string code = AdmissionConfigurationText.NormalizeCode(request.Code);
        if (!AdmissionConfigurationText.IsRequiredValid(name, 200) ||
            !AdmissionConfigurationText.IsRequiredValid(code, 30) ||
            request.DurationMonths is < 1 or > 60)
        {
            return Result<ProgramDto>.Failure(
                400,
                "Program name and code are required, and duration must be between 1 and 60 months.");
        }

        return await _writeExecutor.ExecuteSerializableAsync(
            cancellationToken,
            async ct =>
            {
                var program = await _dbContext.Programs
                    .Include(x => x.Institution)
                    .SingleOrDefaultAsync(x => x.Id == programId, ct);

                if (program is null)
                {
                    return Result<ProgramDto>.Failure(
                        404,
                        "Program was not found.");
                }

                if (await _applicationReader.IsProgramConfigurationLockedAsync(
                        programId,
                        ct))
                {
                    return Result<ProgramDto>.Failure(
                        409,
                        "Program configuration is locked because one of its cycles is Active or already has applications.");
                }

                if (await _dbContext.Programs.AnyAsync(
                        x =>
                            x.Id != programId &&
                            x.Code.ToUpper() == code,
                        ct))
                {
                    return Result<ProgramDto>.Failure(
                        409,
                        $"Program code '{code}' already exists.");
                }

                program.Name = name;
                program.Code = code;
                program.DurationMonths = request.DurationMonths;
                await _dbContext.SaveChangesAsync(ct);

                var tracks = await _dbContext.Tracks
                    .Where(x => x.ProgramId == programId)
                    .ToListAsync(ct);
                int trackCount = tracks.Count;
                int cycleCount = await _dbContext.AdmissionCycles.CountAsync(
                    x => x.ProgramId == programId,
                    ct);

                return Result<ProgramDto>.Success(
                    MapProgram(
                        program,
                        program.Institution.Name,
                        trackCount,
                        cycleCount),
                    message: "Program updated successfully.");
            },
            ("UX_Programs_Code", $"Program code '{code}' already exists."));
    }

    public async Task<Result<bool>> DeleteProgramAsync(
        Guid programId,
        CancellationToken cancellationToken = default)
    {
        if (programId == Guid.Empty)
        {
            return Result<bool>.Failure(
                400,
                "A valid program ID is required.");
        }

        return await _writeExecutor.ExecuteSerializableAsync(
            cancellationToken,
            async ct =>
            {
                var program = await _dbContext.Programs
                    .SingleOrDefaultAsync(x => x.Id == programId, ct);

                if (program is null)
                {
                    return Result<bool>.Failure(
                        404,
                        "Program was not found.");
                }

                if (await _applicationReader.IsProgramConfigurationLockedAsync(
                        programId,
                        ct))
                {
                    return Result<bool>.Failure(
                        409,
                        "Program cannot be deleted because one of its admission cycles is Active or already has applications.");
                }

                var cycles = await _dbContext.AdmissionCycles
                    .Where(x => x.ProgramId == programId)
                    .ToListAsync(ct);
                Guid[] cycleIds = cycles.Select(x => x.Id).ToArray();

                var tracks = await _dbContext.Tracks
                    .Where(x => x.ProgramId == programId)
                    .ToListAsync(ct);
                Guid[] trackIds = tracks.Select(x => x.Id).ToArray();

                var offerings = await _dbContext.TrackBranchOfferings
                    .Where(x =>
                        cycleIds.Contains(x.CycleId) ||
                        trackIds.Contains(x.TrackId))
                    .ToListAsync(ct);

                var eligibilityRules = await _dbContext.CycleEligibilityRules
                    .Where(x => cycleIds.Contains(x.CycleId))
                    .ToListAsync(ct);

                var documentRequirements = await _dbContext.ProgramDocumentRequirements
                    .Where(x => x.ProgramId == programId)
                    .ToListAsync(ct);

                _dbContext.TrackBranchOfferings.RemoveRange(offerings);
                _dbContext.CycleEligibilityRules.RemoveRange(eligibilityRules);
                _dbContext.ProgramDocumentRequirements.RemoveRange(documentRequirements);
                _dbContext.AdmissionCycles.RemoveRange(cycles);
                _dbContext.Tracks.RemoveRange(tracks);
                _dbContext.Programs.Remove(program);

                await _dbContext.SaveChangesAsync(ct);

                return Result<bool>.Success(
                    true,
                    message: "Program and its configuration data were deleted successfully.");
            });
    }

    private DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    private static InstitutionDto MapInstitution(
        Institution institution,
        int programCount)
    {
        return new InstitutionDto
        {
            Id = institution.Id,
            Name = institution.Name,
            Code = institution.Code,
            ProgramCount = programCount,
        };
    }

    private static ProgramDto MapProgram(
        AdmissionProgram program,
        string institutionName,
        int trackCount,
        int cycleCount)
    {
        return new ProgramDto
        {
            Id = program.Id,
            InstitutionId = program.InstitutionId,
            InstitutionName = institutionName,
            Name = program.Name,
            Code = program.Code,
            DurationMonths = program.DurationMonths,
            TrackCount = trackCount,
            CycleCount = cycleCount,
        };
    }
}
