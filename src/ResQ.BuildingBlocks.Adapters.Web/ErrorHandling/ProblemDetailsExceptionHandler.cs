using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ResQ.BuildingBlocks.Adapters.Web;

/// <summary>
/// A last-resort <see cref="IExceptionHandler"/> behind the <see cref="ResultExtensions"/> path.
/// FluentValidation failures become a 400 validation problem; everything else becomes a 500
/// <see cref="ProblemDetails"/>. The stack trace is logged server-side and never leaked to the client.
/// </summary>
/// <param name="problemDetails">The framework problem-details writer.</param>
/// <param name="logger">The logger used to record unhandled exceptions.</param>
public sealed class ProblemDetailsExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (exception is ValidationException validation)
        {
            logger.LogWarning(validation, "Request validation failed (traceId: {TraceId}).", traceId);
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

            var errors = validation.Errors
                .GroupBy(e => e.PropertyName, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray(), StringComparer.Ordinal);

            return await problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed",
                    Extensions = { ["errors"] = errors, ["traceId"] = traceId },
                },
            }).ConfigureAwait(false);
        }

        // Strip CR/LF from the user-controllable request path before logging (prevents log forging).
        var safePath = httpContext.Request.Path.HasValue
            ? httpContext.Request.Path.Value!.Replace('\r', '_').Replace('\n', '_')
            : "/";
        logger.LogError(exception, "Unhandled exception processing {Path} (traceId: {TraceId}).", safePath, traceId);
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Server error",
                Extensions = { ["traceId"] = traceId },
            },
        }).ConfigureAwait(false);
    }
}
