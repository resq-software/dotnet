using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using ResQ.BuildingBlocks.Domain;

namespace ResQ.BuildingBlocks.ServiceDefaults;

/// <summary>
/// Registers named Polly resilience pipelines for the domain <see cref="Result"/> world. The retry
/// strategy uses a typed rule instead of a plain retryable-error flag:
/// retry on any exception, or when the outcome is a failed <see cref="Result"/> whose
/// <see cref="Error.Type"/> is <see cref="ErrorType.Failure"/> (transient failures) — never on
/// <see cref="ErrorType.Validation"/>, <see cref="ErrorType.NotFound"/>, <see cref="ErrorType.Conflict"/>,
/// or <see cref="ErrorType.Unauthorized"/>, which are not retryable.
/// </summary>
public static class ResiliencePipelineExtensions
{
    private const int DefaultMaxRetryAttempts = 3;
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Registers a resilience pipeline under <paramref name="key"/> composed of retry (keyed on
    /// <see cref="ErrorType.Failure"/>), a circuit breaker, and a per-attempt timeout. Callers may tweak
    /// the retry options via <paramref name="configure"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="key">The pipeline key used to resolve it from a <c>ResiliencePipelineProvider</c>.</param>
    /// <param name="configure">Optional hook to adjust the default retry options.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddResqResiliencePipeline(
        this IServiceCollection services,
        string key,
        Action<RetryStrategyOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(key);

        services.AddResiliencePipeline(key, builder =>
        {
            var retry = new RetryStrategyOptions
            {
                ShouldHandle = ShouldRetry,
                MaxRetryAttempts = DefaultMaxRetryAttempts,
                Delay = DefaultRetryDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
            };
            configure?.Invoke(retry);

            builder
                .AddRetry(retry)
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions { ShouldHandle = ShouldBreak })
                .AddTimeout(DefaultTimeout);
        });

        return services;
    }

    private static ValueTask<bool> ShouldRetry(RetryPredicateArguments<object> args) =>
        ShouldHandleOutcome(args.Outcome)
            && args.Outcome.Exception is not BrokenCircuitException and not TimeoutRejectedException
            ? PredicateResult.True()
            : PredicateResult.False();

    private static ValueTask<bool> ShouldBreak(CircuitBreakerPredicateArguments<object> args) =>
        ShouldHandleOutcome(args.Outcome) ? PredicateResult.True() : PredicateResult.False();

    /// <summary>
    /// The shared outcome rule for both the retry and the circuit breaker: handle any exception, or a
    /// failed <see cref="Result"/> classified <see cref="ErrorType.Failure"/>. Sharing it ensures the
    /// breaker actually opens on a sustained stream of failed <see cref="Result"/>s instead of retrying
    /// them forever (its default rule matches exceptions only).
    /// </summary>
    private static bool ShouldHandleOutcome(Outcome<object> outcome) =>
        outcome.Exception is not null
            || outcome.Result is Result { IsFailure: true, Error.Type: ErrorType.Failure };
}
