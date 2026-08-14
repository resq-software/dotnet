using FluentValidation;
using Microsoft.Extensions.Logging;

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

/// <summary>Runs all registered FluentValidation validators before the handler; throws on failure.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var validatorList = validators as IReadOnlyList<IValidator<TRequest>> ?? validators.ToList();
        if (validatorList.Count != 0)
        {
            var context = new ValidationContext<TRequest>(request);
            var failures = validatorList
                .Select(validator => validator.Validate(context))
                .SelectMany(result => result.Errors)
                .Where(failure => failure is not null)
                .ToList();

            if (failures.Count != 0)
            {
                throw new ValidationException(failures);
            }
        }

        return await next();
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
        var response = await next();
        logger.LogInformation("Handled {Request}", requestName);
        return response;
    }
}
