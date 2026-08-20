using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EduFlowAI.Api.Middlewares;

public sealed class ExceptionHandler(
    ILogger<ExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var (status, title, detail) = exception switch
        {
            BadHttpRequestException => (
                StatusCodes.Status400BadRequest,
                "Invalid request",
                exception.Message),
            DbUpdateException
            {
                InnerException: PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation
                }
            } => (
                StatusCodes.Status409Conflict,
                "Data conflict",
                "A record with the same unique value already exists."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Unexpected server error",
                environment.IsDevelopment()
                    ? GetDevelopmentDetail(exception)
                    : "An unexpected error occurred.")
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}.",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(
                exception,
                "Request failed for {Method} {Path}.",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            cancellationToken: cancellationToken);
        return true;
    }

    private static string GetDevelopmentDetail(Exception exception)
    {
        var rootCause = exception.GetBaseException();

        return ReferenceEquals(rootCause, exception)
            ? exception.Message
            : $"{rootCause.GetType().Name}: {rootCause.Message}";
    }
}