using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Application.Features.Configuration.Common;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;
using EduFlowAI.Shared.Kernel.Common;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EduFlowAI.Admission.Application.Features.Requirements;

public sealed class ProgramDocumentRequirementDto
{
    public Guid Id { get; init; }
    public Guid ProgramId { get; init; }
    public DocumentType DocumentType { get; init; }
    public Gender? RequiredForGender { get; init; }
}

public sealed class ProgramDocumentRequirementInput
{
    public DocumentType DocumentType { get; init; }
    public Gender? RequiredForGender { get; init; }
}

public sealed class UpdateProgramDocumentRequirementsRequest
{
    [Required, MaxLength(12)]
    public ProgramDocumentRequirementInput[] Requirements { get; init; } = [];
}

public interface IProgramRequirementService
{
    Task<IReadOnlyList<ProgramDocumentRequirementDto>?> GetProgramRequirementsAsync(
        Guid programId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ProgramDocumentRequirementDto>>> ReplaceProgramRequirementsAsync(
        Guid programId,
        UpdateProgramDocumentRequirementsRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed class ProgramRequirementService : IProgramRequirementService
{
    private readonly IAdmissionDbContext _dbContext;
    private readonly IAdmissionApplicationReader _applicationReader;
    private readonly TimeProvider _timeProvider;
    private readonly AdmissionWriteExecutor _writeExecutor;

    public ProgramRequirementService(
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

    public async Task<IReadOnlyList<ProgramDocumentRequirementDto>?> GetProgramRequirementsAsync(
        Guid programId,
        CancellationToken cancellationToken = default)
    {
        if (!await _dbContext.Programs.AnyAsync(
                x => x.Id == programId,
                cancellationToken))
        {
            return null;
        }

        return await _dbContext.ProgramDocumentRequirements
            .AsNoTracking()
            .Where(x => x.ProgramId == programId)
            .OrderBy(x => x.DocumentType)
            .ThenBy(x => x.RequiredForGender)
            .Select(x => new ProgramDocumentRequirementDto
            {
                Id = x.Id,
                ProgramId = x.ProgramId,
                DocumentType = x.DocumentType,
                RequiredForGender = x.RequiredForGender
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<IReadOnlyList<ProgramDocumentRequirementDto>>> ReplaceProgramRequirementsAsync(
        Guid programId,
        UpdateProgramDocumentRequirementsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (programId == Guid.Empty)
        {
            return Result<IReadOnlyList<ProgramDocumentRequirementDto>>.Failure(
                400,
                "A valid program ID is required.");
        }

        if (request?.Requirements is null)
        {
            return Result<IReadOnlyList<ProgramDocumentRequirementDto>>.Failure(
                400,
                "A requirements list is required.");
        }

        if (request.Requirements.Length == 0)
        {
            return Result<IReadOnlyList<ProgramDocumentRequirementDto>>.Failure(
                400,
                "At least one document requirement is required.");
        }

        if (request.Requirements.Length > 12)
        {
            return Result<IReadOnlyList<ProgramDocumentRequirementDto>>.Failure(
                400,
                "A program can contain at most 12 document requirement combinations.");
        }

        if (request.Requirements.Any(input =>
                input is null ||
                !Enum.IsDefined(input.DocumentType) ||
                input.DocumentType == DocumentType.None ||
                (input.RequiredForGender.HasValue &&
                 (!Enum.IsDefined(input.RequiredForGender.Value) ||
                  input.RequiredForGender.Value == Gender.None))))
        {
            return Result<IReadOnlyList<ProgramDocumentRequirementDto>>.Failure(
                400,
                "Every document type must be defined; gender must be Male, Female, or null for all genders.");
        }

        bool hasDuplicates = request.Requirements
            .GroupBy(x => new { x.DocumentType, x.RequiredForGender })
            .Any(group => group.Count() > 1);

        if (hasDuplicates)
        {
            return Result<IReadOnlyList<ProgramDocumentRequirementDto>>.Failure(
                400,
                "Duplicate document requirement combinations are not allowed.");
        }

        return await _writeExecutor.ExecuteSerializableAsync(
            cancellationToken,
            async ct =>
            {
                if (!await _dbContext.Programs.AnyAsync(
                        x => x.Id == programId,
                        ct))
                {
                    return Result<IReadOnlyList<ProgramDocumentRequirementDto>>.Failure(
                        404,
                        "Program was not found.");
                }

                if (await _applicationReader.IsProgramConfigurationLockedAsync(
                        programId,
                        ct))
                {
                    return Result<IReadOnlyList<ProgramDocumentRequirementDto>>.Failure(
                        409,
                        "Document requirements are locked because the program has an Active cycle or already has applications.");
                }

                var existing = await _dbContext.ProgramDocumentRequirements
                    .Where(x => x.ProgramId == programId)
                    .ToListAsync(ct);

                if (existing
                    .GroupBy(requirement => new RequirementKey(
                        requirement.DocumentType,
                        requirement.RequiredForGender))
                    .Any(group => group.Count() > 1))
                {
                    return Result<IReadOnlyList<ProgramDocumentRequirementDto>>.Failure(
                        409,
                        "Existing document requirements contain duplicate keys. Resolve the database duplicates before updating this program.");
                }

                var existingByKey = existing.ToDictionary(
                    requirement => new RequirementKey(
                        requirement.DocumentType,
                        requirement.RequiredForGender));

                var requestedKeys = request.Requirements
                    .Select(input => new RequirementKey(
                        input.DocumentType,
                        input.RequiredForGender))
                    .ToHashSet();

                var removed = existing
                    .Where(requirement =>
                        !requestedKeys.Contains(new RequirementKey(
                            requirement.DocumentType,
                            requirement.RequiredForGender)))
                    .ToList();

                _dbContext.ProgramDocumentRequirements.RemoveRange(removed);

                var effectiveRequirements = new List<ProgramDocumentRequirement>(
                    request.Requirements.Length);

                foreach (var input in request.Requirements)
                {
                    var key = new RequirementKey(
                        input.DocumentType,
                        input.RequiredForGender);

                    if (existingByKey.TryGetValue(key, out var retained))
                    {
                        effectiveRequirements.Add(retained);
                        continue;
                    }

                    var added = new ProgramDocumentRequirement
                    {
                        Id = Guid.NewGuid(),
                        ProgramId = programId,
                        DocumentType = input.DocumentType,
                        RequiredForGender = input.RequiredForGender,
                        CreatedAt = UtcNow,
                        UpdatedAt = UtcNow
                    };

                    _dbContext.ProgramDocumentRequirements.Add(added);
                    effectiveRequirements.Add(added);
                }

                await _dbContext.SaveChangesAsync(ct);

                IReadOnlyList<ProgramDocumentRequirementDto> data =
                    effectiveRequirements
                        .OrderBy(x => x.DocumentType)
                        .ThenBy(x => x.RequiredForGender)
                        .Select(MapRequirement)
                        .ToList();

                return Result<IReadOnlyList<ProgramDocumentRequirementDto>>.Success(
                    data,
                    message: "Program document requirements updated successfully.");
            },
            (
                "UX_ProgramDocumentRequirements_AllGenders",
                "A duplicate all-genders document requirement already exists."),
            (
                "UX_ProgramDocumentRequirements_GenderScoped",
                "A duplicate gender-specific document requirement already exists."));
    }

    private DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    private static ProgramDocumentRequirementDto MapRequirement(
        ProgramDocumentRequirement requirement)
    {
        return new ProgramDocumentRequirementDto
        {
            Id = requirement.Id,
            ProgramId = requirement.ProgramId,
            DocumentType = requirement.DocumentType,
            RequiredForGender = requirement.RequiredForGender
        };
    }

    private readonly record struct RequirementKey(
        DocumentType DocumentType,
        Gender? RequiredForGender);
}
