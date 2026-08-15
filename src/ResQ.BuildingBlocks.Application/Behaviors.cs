using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ResQ.BuildingBlocks.Domain;

namespace ResQ.BuildingBlocks.Application;

/// <summary>Invokes the next stage of the pipeline (the handler, or the next behavior).</summary>
/// <typeparam name="TResponse">The response type produced by the pipeline.</typeparam>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// A cross-cutting behavior that wraps handler execution (validation, logging, transactions, caching…).
/// Adapters compose these around each command/query handler.
/// </summary>
/// <typeparam name="TRequest">The request (command or query) type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
{
    /// <summary>Runs this behavior, calling <paramref name="next"/> to continue the pipeline.</summary>
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}

/// <summary>
/// Runs all registered FluentValidation validators before the handler. On failure it short-circuits the
/// pipeline with a failed <see cref="Result"/> carrying a single <see cref="ErrorType.Validation"/> error
/// (the per-field failures aggregated into its message) instead of throwing — consistent with the library's
/// Result-over-exceptions idiom, where validation failures are an <em>expected</em> outcome rather than an
/// exceptional one.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type; always <see cref="Result"/> or <see cref="Result{TValue}"/>.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
{
    /// <summary>Stable code for the aggregated validation error.</summary>
    private const string ValidationErrorCode = "validation.failed";

    // Built once per closed (TRequest, TResponse): maps the aggregated Error to a failed TResponse with no
    // per-invocation reflection. Null when TResponse is neither Result nor Result<T> (defensive; see Handle).
    private static readonly Func<Error, TResponse>? FailureFactory = BuildFailureFactory();

    /// <summary>
    /// Runs the validators; on failure short-circuits with a failed <typeparamref name="TResponse"/> carrying
    /// an <see cref="ErrorType.Validation"/> error, otherwise continues the pipeline via <paramref name="next"/>.
    /// </summary>
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var validatorList = validators as IReadOnlyList<IValidator<TRequest>> ?? validators.ToList();
        if (validatorList.Count != 0)
        {
            var context = new ValidationContext<TRequest>(request);

            // Await ValidateAsync so async validators (e.g. ones that hit a store) run correctly and the
            // CancellationToken is honored; running them concurrently keeps the pipeline stage cheap.
            var results = await Task.WhenAll(
                validatorList.Select(validator => validator.ValidateAsync(context, cancellationToken)))
                .ConfigureAwait(false);

            var failures = results
                .SelectMany(result => result.Errors)
                .Where(failure => failure is not null)
                .ToList();

            if (failures.Count != 0)
            {
                // Defensive: TResponse is neither Result nor Result<T>, so there is no failed value to
                // return — fall back to throwing so the failure is never silently dropped.
                if (FailureFactory is null)
                {
                    throw new ValidationException(failures);
                }

                // Preserve per-field detail in the message; per-field RFC7807 rendering stays in the Web adapter.
                var message = string.Join(
                    "; ", failures.Select(failure => $"{failure.PropertyName}: {failure.ErrorMessage}"));
                return FailureFactory(Error.Validation(ValidationErrorCode, message));
            }
        }

        return await next().ConfigureAwait(false);
    }

    // Reflection is confined here and cached in FailureFactory. TResponse is already a closed type, so the
    // failed-Result factory is built against it directly (no MakeGenericType). Annotated to match the other
    // reflection seams in this assembly (Sender, DomainEventDispatcher).
    [RequiresUnreferencedCode(
        "Builds a failed Result<TValue> for TResponse via reflection over the Result.Failure<T>(Error) factory.")]
    [RequiresDynamicCode("Closes the generic Result.Failure<T>(Error) factory for TValue via MakeGenericMethod.")]
    private static Func<Error, TResponse>? BuildFailureFactory()
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return static error => (TResponse)(object)Result.Failure(error);
        }

        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = typeof(TResponse).GetGenericArguments()[0];
            var failure = typeof(Result)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(method =>
                    string.Equals(method.Name, nameof(Result.Failure), StringComparison.Ordinal) &&
                    method.IsGenericMethodDefinition)
                .MakeGenericMethod(valueType);

            return error => (TResponse)failure.Invoke(null, [error])!;
        }

        return null;
    }
}

/// <summary>Logs the start and completion of each request.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {Request}", requestName);
        var response = await next().ConfigureAwait(false);
        logger.LogInformation("Handled {Request}", requestName);
        return response;
    }
}
