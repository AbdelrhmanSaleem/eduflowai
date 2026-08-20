using System.Data;
using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EduFlowAI.Admission.Infrastructure.Seeding;

internal static class AdmissionDataSeeder
{
    private const long AdmissionSeedLockKey = 1_164_618_325_329_019_809;

    internal static async Task SeedAsync(
        IAdmissionDbContext context,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timeProvider);

        await ValidateDatabasePreconditionsAsync(
            context.Database,
            cancellationToken);

        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        await context.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0});",
            new object[] { AdmissionSeedLockKey },
            cancellationToken);

        await ReconcileAsync(
            context,
            timeProvider.GetUtcNow(),
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    internal static async Task ValidateDatabasePreconditionsAsync(
        DatabaseFacade database,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(database);

        if (!string.Equals(
                database.ProviderName,
                "Npgsql.EntityFrameworkCore.PostgreSQL",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Admission seed data requires the PostgreSQL EF Core provider.");
        }

        // The repository currently does not commit EF migration classes. When no
        // migrations are compiled into the shared DbContext assembly, there is no
        // EF migration state for Admission to validate. If the platform later
        // starts shipping migrations, keep protecting explicit seeding from an
        // out-of-date schema by rejecting pending migrations.
        var compiledMigrations = database.GetMigrations().ToArray();
        if (compiledMigrations.Length == 0)
        {
            return;
        }

        var pendingMigrations = await database
            .GetPendingMigrationsAsync(cancellationToken);
        if (pendingMigrations.Any())
        {
            throw new InvalidOperationException(
                "Apply all pending database migrations before running Admission seed data.");
        }
    }

    internal static async Task ReconcileAsync(
        IAdmissionDbContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var institution = await ResolveInstitutionAsync(
            context,
            now,
            cancellationToken);
        var programResolution = await ResolveProgramAsync(
            context,
            institution.Id,
            now,
            cancellationToken);

        if (programResolution.WasCreated)
        {
            await SeedProgramDocumentRequirementsAsync(
                context,
                programResolution.Program.Id,
                now,
                cancellationToken);
        }

        await SeedTracksAsync(
            context,
            programResolution.Program.Id,
            now,
            cancellationToken);
        await SeedBranchesAsync(context, now, cancellationToken);
    }

    private static async Task<Institution> ResolveInstitutionAsync(
        IAdmissionDbContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var seed = CreateInstitution(now);
        var normalizedCode = seed.Code.ToUpperInvariant();
        var candidates = await context.Institutions
            .Where(institution =>
                institution.Id == seed.Id ||
                institution.Code.ToUpper() == normalizedCode)
            .ToListAsync(cancellationToken);

        var idMatch = candidates.SingleOrDefault(candidate => candidate.Id == seed.Id);
        var codeMatches = candidates
            .Where(candidate =>
                StringComparer.OrdinalIgnoreCase.Equals(candidate.Code, seed.Code))
            .ToList();

        EnsureAtMostOneBusinessKeyMatch(
            nameof(Institution),
            $"code '{seed.Code}'",
            codeMatches.Count);

        var codeMatch = codeMatches.SingleOrDefault();
        if (idMatch is not null)
        {
            if (codeMatch is not null && codeMatch.Id != idMatch.Id)
            {
                throw SeedCollision(
                    nameof(Institution),
                    seed.Id,
                    $"the stable ID and code '{seed.Code}' identify different rows");
            }

            // Stable ID wins. Name/code may have been changed by an administrator.
            return idMatch;
        }

        if (codeMatch is not null)
        {
            return codeMatch;
        }

        context.Institutions.Add(seed);
        return seed;
    }

    private static async Task<ProgramResolution> ResolveProgramAsync(
        IAdmissionDbContext context,
        Guid institutionId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var seed = CreateNineMonthProgram(institutionId, now);
        var normalizedCode = seed.Code.ToUpperInvariant();
        var candidates = await context.Programs
            .Where(program =>
                program.Id == seed.Id ||
                program.Code.ToUpper() == normalizedCode)
            .ToListAsync(cancellationToken);

        var idMatch = candidates.SingleOrDefault(candidate => candidate.Id == seed.Id);
        if (idMatch is not null && idMatch.InstitutionId != institutionId)
        {
            throw SeedCollision(
                nameof(Program),
                seed.Id,
                "the existing row belongs to a different institution");
        }

        var codeMatches = candidates
            .Where(candidate =>
                StringComparer.OrdinalIgnoreCase.Equals(candidate.Code, seed.Code))
            .ToList();

        EnsureAtMostOneBusinessKeyMatch(
            nameof(Program),
            $"code '{seed.Code}'",
            codeMatches.Count);

        var codeMatch = codeMatches.SingleOrDefault();
        if (codeMatch is not null && codeMatch.InstitutionId != institutionId)
        {
            throw new InvalidOperationException(
                $"Admission seed conflict: program code '{seed.Code}' already belongs " +
                "to a different institution.");
        }

        if (idMatch is not null)
        {
            if (codeMatch is not null && codeMatch.Id != idMatch.Id)
            {
                throw SeedCollision(
                    nameof(Program),
                    seed.Id,
                    $"the stable ID and code '{seed.Code}' identify different rows");
            }

            // Stable ID wins. Name/code/duration may be administrator configuration.
            return new ProgramResolution(idMatch, WasCreated: false);
        }

        if (codeMatch is not null)
        {
            return new ProgramResolution(codeMatch, WasCreated: false);
        }

        context.Programs.Add(seed);
        return new ProgramResolution(seed, WasCreated: true);
    }

    internal static async Task SeedProgramDocumentRequirementsAsync(
        IAdmissionDbContext context,
        Guid programId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var seeds = CreateProgramDocumentRequirements(programId, now);
        var seedIds = seeds.Select(seed => seed.Id).ToArray();
        var existingSeedIds = await context.ProgramDocumentRequirements
            .Where(requirement => seedIds.Contains(requirement.Id))
            .Select(requirement => requirement.Id)
            .ToListAsync(cancellationToken);

        foreach (var seed in seeds)
        {
            if (!existingSeedIds.Contains(seed.Id))
            {
                context.ProgramDocumentRequirements.Add(seed);
            }
        }
    }

    private static async Task SeedTracksAsync(
        IAdmissionDbContext context,
        Guid programId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var seeds = CreateTracks(programId, now);
        var seedIds = seeds.Select(seed => seed.Id).ToArray();
        var candidates = await context.Tracks
            .Where(track => track.ProgramId == programId || seedIds.Contains(track.Id))
            .ToListAsync(cancellationToken);

        foreach (var definition in AdmissionTrackSeedCatalog.All)
        {
            var seed = seeds.Single(candidate => candidate.Id == definition.Id);
            var idMatch = candidates.SingleOrDefault(candidate => candidate.Id == seed.Id);
            if (idMatch is not null && idMatch.ProgramId != seed.ProgramId)
            {
                idMatch.ProgramId = seed.ProgramId;
            }

            var naturalKeyMatches = candidates
                .Where(candidate =>
                    candidate.ProgramId == seed.ProgramId &&
                    (StringComparer.OrdinalIgnoreCase.Equals(
                         candidate.Name,
                         definition.Name) ||
                     definition.SupportedLegacyNames.Contains(
                         candidate.Name,
                         StringComparer.OrdinalIgnoreCase)))
                .ToList();

            EnsureAtMostOneBusinessKeyMatch(
                nameof(Track),
                $"{seed.ProgramId}/'{definition.Name}' and its supported legacy names",
                naturalKeyMatches.Count);

            var naturalKeyMatch = naturalKeyMatches.SingleOrDefault();
            if (idMatch is not null &&
                naturalKeyMatch is not null &&
                idMatch.Id != naturalKeyMatch.Id)
            {
                throw SeedCollision(
                    nameof(Track),
                    seed.Id,
                    "the stable ID and natural key identify different rows");
            }

            var existing = idMatch ?? naturalKeyMatch;
            if (existing is null)
            {
                context.Tracks.Add(seed);
                candidates.Add(seed);
                continue;
            }

            ReconcileOfficialTrack(existing, programId, definition, now);
        }

        foreach (var historical in candidates.Where(candidate =>
                     candidate.ProgramId == programId &&
                     AdmissionLegacyTrackCatalog.IsHistorical(
                         candidate.Id,
                         candidate.Name)))
        {
            DeactivateNonOfficialTrack(historical, programId, now);
        }
    }

    internal static bool DeactivateNonOfficialTrack(
        Track track,
        Guid programId,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(track);

        if (track.ProgramId != programId ||
            !AdmissionLegacyTrackCatalog.IsHistorical(track.Id, track.Name) ||
            !track.IsActive)
        {
            return false;
        }

        track.IsActive = false;
        track.UpdatedAt = now;
        return true;
    }

    internal static bool ReconcileOfficialTrack(
        Track track,
        Guid programId,
        AdmissionTrackSeedDefinition definition,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(track);
        ArgumentNullException.ThrowIfNull(definition);

        if (track.ProgramId != programId)
        {
            throw SeedCollision(
                nameof(Track),
                definition.Id,
                "the existing row belongs to a different program");
        }

        bool stableIdMatch = track.Id == definition.Id;
        bool hasOfficialName = StringComparer.OrdinalIgnoreCase.Equals(
            track.Name,
            definition.Name);
        bool hasSupportedLegacyName = definition.SupportedLegacyNames.Contains(
            track.Name,
            StringComparer.OrdinalIgnoreCase);

        if (!stableIdMatch && !hasOfficialName && !hasSupportedLegacyName)
        {
            throw SeedCollision(
                nameof(Track),
                definition.Id,
                $"the existing row has unsupported name '{track.Name}'");
        }

        // Existing persisted track fields are Super Admin-owned configuration.
        // The only automatic update retained here is the narrow, known taxonomy
        // rename from a supported legacy source name to the Intake-47 name.
        if (hasSupportedLegacyName && !hasOfficialName)
        {
            track.Name = definition.Name;
            track.UpdatedAt = now;
            return true;
        }

        return false;
    }

    private static async Task SeedBranchesAsync(
        IAdmissionDbContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var seeds = CreateBranches(now);
        var candidates = await context.Branches
            .ToListAsync(cancellationToken);

        foreach (var seed in seeds)
        {
            var idMatch = candidates.SingleOrDefault(candidate => candidate.Id == seed.Id);
            var naturalKeyMatches = candidates
                .Where(candidate =>
                    StringComparer.OrdinalIgnoreCase.Equals(candidate.Name, seed.Name))
                .ToList();

            EnsureAtMostOneBusinessKeyMatch(
                nameof(Branch),
                $"name '{seed.Name}'",
                naturalKeyMatches.Count);

            var naturalKeyMatch = naturalKeyMatches.SingleOrDefault();
            if (idMatch is not null &&
                naturalKeyMatch is not null &&
                idMatch.Id != naturalKeyMatch.Id)
            {
                throw SeedCollision(
                    nameof(Branch),
                    seed.Id,
                    "the stable ID and natural key identify different rows");
            }

            var existing = idMatch ?? naturalKeyMatch;
            if (existing is null)
            {
                context.Branches.Add(seed);
                candidates.Add(seed);
                continue;
            }

            var definition = AdmissionBranchSeedCatalog.Find(
                seed.Id,
                seed.Name)!;
            ReconcileOfficialBranch(existing, definition);
        }
    }

    internal static bool ReconcileOfficialBranch(
        Branch branch,
        AdmissionBranchSeedDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(branch);
        ArgumentNullException.ThrowIfNull(definition);

        if (branch.Id != definition.Id &&
            !StringComparer.OrdinalIgnoreCase.Equals(
                branch.Name,
                definition.Name))
        {
            throw SeedCollision(
                nameof(Branch),
                definition.Id,
                $"the existing row has unsupported name '{branch.Name}'");
        }

        // Name, governorate and active state are all editable after bootstrap.
        // Do not treat null/blank values as missing seed metadata on a rerun.
        return false;
    }

    private static void EnsureAtMostOneBusinessKeyMatch(
        string entityName,
        string businessKey,
        int matchCount)
    {
        if (matchCount > 1)
        {
            throw new InvalidOperationException(
                $"Admission seed conflict: {matchCount} {entityName} rows match " +
                $"business key {businessKey}. Remove the duplicate rows before seeding.");
        }
    }

    private static InvalidOperationException SeedCollision(
        string entityName,
        Guid stableId,
        string reason)
    {
        return new InvalidOperationException(
            $"Admission seed conflict for {entityName} ID '{stableId}': {reason}.");
    }

    private static Institution CreateInstitution(DateTimeOffset now)
    {
        return new Institution
        {
            Id = AdmissionSeedIds.ItiInstitutionId,
            Name = "Information Technology Institute",
            Code = "ITI",
            CreatedAt = now
        };
    }

    private static Program CreateNineMonthProgram(
        Guid institutionId,
        DateTimeOffset now)
    {
        return new Program
        {
            Id = AdmissionSeedIds.NineMonthProgramId,
            InstitutionId = institutionId,
            Name = "9-Month Professional Training Program",
            Code = "9M",
            DurationMonths = 9,
            CreatedAt = now
        };
    }

    private static IReadOnlyList<ProgramDocumentRequirement>
        CreateProgramDocumentRequirements(Guid programId, DateTimeOffset now)
    {
        return
        [
            new()
            {
                Id = AdmissionSeedIds.NationalIdRequirementId,
                ProgramId = programId,
                DocumentType = DocumentType.NationalId,
                RequiredForGender = null,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = AdmissionSeedIds.BirthCertificateRequirementId,
                ProgramId = programId,
                DocumentType = DocumentType.BirthCertificate,
                RequiredForGender = null,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = AdmissionSeedIds.GraduationCertificateRequirementId,
                ProgramId = programId,
                DocumentType = DocumentType.GraduationCertificate,
                RequiredForGender = null,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = AdmissionSeedIds.MilitaryCertificateRequirementId,
                ProgramId = programId,
                DocumentType = DocumentType.MilitaryCertificate,
                RequiredForGender = Gender.Male,
                CreatedAt = now,
                UpdatedAt = now
            }
        ];
    }

    private static IReadOnlyList<Track> CreateTracks(
        Guid programId,
        DateTimeOffset now)
    {
        return AdmissionTrackSeedCatalog.All
            .Select(definition => CreateTrack(definition, programId, now))
            .ToArray();
    }

    private static Track CreateTrack(
        AdmissionTrackSeedDefinition definition,
        Guid programId,
        DateTimeOffset now)
    {
        return new Track
        {
            Id = definition.Id,
            ProgramId = programId,
            Name = definition.Name,
            Description = definition.Description,
            PrerequisiteTopics = [.. definition.PrerequisiteTopics],
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static IReadOnlyList<Branch> CreateBranches(DateTimeOffset now)
    {
        return AdmissionBranchSeedCatalog.All
            .Select(definition => new Branch
            {
                Id = definition.Id,
                Name = definition.Name,
                Governorate = definition.Governorate,
                IsActive = true,
                CreatedAt = now
            })
            .ToArray();
    }

    private readonly record struct ProgramResolution(
        Program Program,
        bool WasCreated);
}
