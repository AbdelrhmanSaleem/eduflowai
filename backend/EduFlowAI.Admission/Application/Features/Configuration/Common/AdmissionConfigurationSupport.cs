using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;
using EduFlowAI.Shared.Kernel.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

namespace EduFlowAI.Admission.Application.Features.Configuration.Common;

internal sealed class AdmissionWriteExecutor
{
    private readonly IAdmissionDbContext _dbContext;

    public AdmissionWriteExecutor(IAdmissionDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<Result<T>> ExecuteAsync<T>(
        CancellationToken cancellationToken,
        Func<CancellationToken, Task<Result<T>>> operation,
        params (string ConstraintName, string Message)[] uniqueConflicts)
    {
        try
        {
            return await operation(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<T>.Failure(
                409,
                "The configuration was changed by another administrator. Reload it and try again.");
        }
        catch (DbUpdateException exception)
        {
            var mapped = MapDatabaseException<T>(exception, uniqueConflicts);
            if (mapped is not null)
            {
                return mapped;
            }

            throw;
        }
        catch (PostgresException exception)
        {
            var mapped = MapDatabaseException<T>(exception, uniqueConflicts);
            if (mapped is not null)
            {
                return mapped;
            }

            throw;
        }
    }

    public Task<Result<T>> ExecuteSerializableAsync<T>(
        CancellationToken cancellationToken,
        Func<CancellationToken, Task<Result<T>>> operation,
        params (string ConstraintName, string Message)[] uniqueConflicts)
    {
        return ExecuteAsync(
            cancellationToken,
            async ct =>
            {
                await using var transaction = await _dbContext.Database
                    .BeginTransactionAsync(IsolationLevel.Serializable, ct);

                var result = await operation(ct);
                if (result.IsSuccess)
                {
                    await transaction.CommitAsync(ct);
                }

                return result;
            },
            uniqueConflicts);
    }

    private static Result<T>? MapDatabaseException<T>(
        Exception exception,
        IReadOnlyCollection<(string ConstraintName, string Message)> uniqueConflicts)
    {
        var postgresException = FindPostgresException(exception);
        if (postgresException is null)
        {
            return null;
        }

        if (postgresException.SqlState is
            PostgresErrorCodes.SerializationFailure or
            PostgresErrorCodes.DeadlockDetected)
        {
            return Result<T>.Failure(
                409,
                "The configuration changed concurrently. Reload it and try again.");
        }

        if (postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            var conflict = uniqueConflicts.FirstOrDefault(candidate =>
                string.Equals(
                    candidate.ConstraintName,
                    postgresException.ConstraintName,
                    StringComparison.Ordinal));

            return string.IsNullOrEmpty(conflict.ConstraintName)
                ? null
                : Result<T>.Failure(409, conflict.Message);
        }

        if (postgresException.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return Result<T>.Failure(
                409,
                "A referenced configuration item changed or no longer exists. Reload and try again.");
        }

        if (postgresException.SqlState == PostgresErrorCodes.CheckViolation)
        {
            return Result<T>.Failure(
                400,
                "The submitted configuration violates a database validation rule.");
        }

        return null;
    }

    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }
        }

        return null;
    }
}

internal sealed class CycleConfigurationGuard
{
    private readonly IAdmissionApplicationReader _applicationReader;

    public CycleConfigurationGuard(IAdmissionApplicationReader applicationReader)
    {
        ArgumentNullException.ThrowIfNull(applicationReader);
        _applicationReader = applicationReader;
    }

    public async Task<string?> ValidateAsync(
        AdmissionCycle cycle,
        CancellationToken cancellationToken)
    {
        if (cycle.Status != CycleStatus.Draft)
        {
            return "Only Draft cycles can be configured.";
        }

        if (await _applicationReader.HasApplicationsForCycleAsync(
                cycle.Id,
                cancellationToken))
        {
            return "Cycle configuration is locked because applications already exist for this cycle.";
        }

        return null;
    }
}

internal static class AdmissionConfigurationText
{
    public static string NormalizeRequired(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    public static string NormalizeCode(string? value)
    {
        return NormalizeRequired(value).ToUpperInvariant();
    }

    public static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static bool IsRequiredValid(string? value, int maxLength)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Length <= maxLength;
    }
}

internal static class TrackTopicValidator
{
    public static TrackTopicValidationResult Validate(IEnumerable<string>? topics)
    {
        var suppliedTopics = topics?.ToList() ?? new List<string>();
        if (suppliedTopics.Count > 50)
        {
            return TrackTopicValidationResult.Invalid(
                "A track can contain at most 50 prerequisite topics.");
        }

        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string? suppliedTopic in suppliedTopics)
        {
            if (string.IsNullOrWhiteSpace(suppliedTopic))
            {
                continue;
            }

            string topic = suppliedTopic.Trim();
            if (topic.Length > 100)
            {
                return TrackTopicValidationResult.Invalid(
                    "Each prerequisite topic must not exceed 100 characters.");
            }

            if (seen.Add(topic))
            {
                normalized.Add(topic);
            }
        }

        return TrackTopicValidationResult.Valid(normalized);
    }
}

internal readonly record struct TrackTopicValidationResult(
    bool IsValid,
    List<string> Topics,
    string ErrorMessage)
{
    public static TrackTopicValidationResult Valid(List<string> topics)
    {
        return new TrackTopicValidationResult(true, topics, string.Empty);
    }

    public static TrackTopicValidationResult Invalid(string errorMessage)
    {
        return new TrackTopicValidationResult(false, new List<string>(), errorMessage);
    }
}
