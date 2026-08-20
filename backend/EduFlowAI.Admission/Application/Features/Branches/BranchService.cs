using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Application.Features.Configuration.Common;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Infrastructure.Seeding;
using EduFlowAI.Shared.Kernel.Common;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace EduFlowAI.Admission.Application.Features.Branches;

public sealed class BranchDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Governorate { get; init; }
    public bool IsActive { get; init; }
    public bool IsOfficialIntake47Location { get; init; }
}

public sealed class CreateBranchRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? Governorate { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class UpdateBranchRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? Governorate { get; init; }

    public bool IsActive { get; init; } = true;
}

public interface IBranchService
{
    Task<IReadOnlyList<BranchDto>> GetBranchesAsync(
        CancellationToken cancellationToken = default);

    Task<Result<BranchDto>> CreateBranchAsync(
        CreateBranchRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<BranchDto>> UpdateBranchAsync(
        Guid branchId,
        UpdateBranchRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed class BranchService : IBranchService
{
    private readonly IAdmissionDbContext _dbContext;
    private readonly IAdmissionApplicationReader _applicationReader;
    private readonly TimeProvider _timeProvider;
    private readonly AdmissionWriteExecutor _writeExecutor;

    public BranchService(
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

    public async Task<IReadOnlyList<BranchDto>> GetBranchesAsync(
        CancellationToken cancellationToken = default)
    {
        var branches = await _dbContext.Branches
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return branches.Select(MapBranch).ToList();
    }

    public async Task<Result<BranchDto>> CreateBranchAsync(
        CreateBranchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Result<BranchDto>.Failure(400, "Request body is required.");
        }

        string name = AdmissionConfigurationText.NormalizeRequired(request.Name);
        string? governorate = AdmissionConfigurationText.NormalizeOptional(
            request.Governorate);

        if (!AdmissionConfigurationText.IsRequiredValid(name, 200) ||
            governorate?.Length > 100)
        {
            return Result<BranchDto>.Failure(
                400,
                "Branch name is required and must not exceed 200 characters; governorate must not exceed 100 characters.");
        }

        return await _writeExecutor.ExecuteAsync(
            cancellationToken,
            async ct =>
            {
                if (await _dbContext.Branches.AnyAsync(
                        x => x.Name.ToUpper() == name.ToUpper(),
                        ct))
                {
                    return Result<BranchDto>.Failure(
                        409,
                        "A branch with the same name already exists.");
                }

                var branch = new Branch
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Governorate = governorate,
                    IsActive = request.IsActive,
                    CreatedAt = UtcNow
                };

                _dbContext.Branches.Add(branch);
                await _dbContext.SaveChangesAsync(ct);

                return Result<BranchDto>.Success(
                    MapBranch(branch),
                    statusCode: 201,
                    message: "Branch created successfully.");
            },
            (
                "UX_Branches_Name",
                "A branch with the same name already exists."));
    }

    public async Task<Result<BranchDto>> UpdateBranchAsync(
        Guid branchId,
        UpdateBranchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty)
        {
            return Result<BranchDto>.Failure(
                400,
                "A valid branch ID is required.");
        }

        if (request is null)
        {
            return Result<BranchDto>.Failure(400, "Request body is required.");
        }

        string name = AdmissionConfigurationText.NormalizeRequired(request.Name);
        string? governorate = AdmissionConfigurationText.NormalizeOptional(
            request.Governorate);

        if (!AdmissionConfigurationText.IsRequiredValid(name, 200) ||
            governorate?.Length > 100)
        {
            return Result<BranchDto>.Failure(
                400,
                "Branch name is required and must not exceed 200 characters; governorate must not exceed 100 characters.");
        }

        return await _writeExecutor.ExecuteSerializableAsync(
            cancellationToken,
            async ct =>
            {
                var branch = await _dbContext.Branches.SingleOrDefaultAsync(
                    x => x.Id == branchId,
                    ct);

                if (branch is null)
                {
                    return Result<BranchDto>.Failure(
                        404,
                        "Branch was not found.");
                }

                if (await _applicationReader.IsBranchConfigurationLockedAsync(
                        branchId,
                        ct))
                {
                    return Result<BranchDto>.Failure(
                        409,
                        "Branch configuration is locked because it belongs to an Active cycle or a cycle that already has applications.");
                }

                if (await _dbContext.Branches.AnyAsync(
                        x =>
                            x.Id != branchId &&
                            x.Name.ToUpper() == name.ToUpper(),
                        ct))
                {
                    return Result<BranchDto>.Failure(
                        409,
                        "A branch with the same name already exists.");
                }

                branch.Name = name;
                branch.Governorate = governorate;
                branch.IsActive = request.IsActive;
                await _dbContext.SaveChangesAsync(ct);

                return Result<BranchDto>.Success(
                    MapBranch(branch),
                    message: "Branch updated successfully.");
            },
            (
                "UX_Branches_Name",
                "A branch with the same name already exists."));
    }

    private DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    private static BranchDto MapBranch(Branch branch)
    {
        return new BranchDto
        {
            Id = branch.Id,
            Name = branch.Name,
            Governorate = branch.Governorate,
            IsActive = branch.IsActive,
            IsOfficialIntake47Location = AdmissionBranchSeedCatalog.Find(
                branch.Id,
                branch.Name) is not null
        };
    }
}
