using Microsoft.Extensions.DependencyInjection;
using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.ServiceDefaults;

/// <summary>
/// Registers the observability pipeline behaviors. This is a separate, explicit call the host makes
/// <b>after</b> <c>AddLoggingPipeline</c>/<c>AddValidationPipeline</c> and <b>before</b> the persistence
/// transaction behavior, so the realized order is
/// <c>Logging → Validation → Tracing → Metrics → Transaction</c>.
/// </summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Registers <see cref="TracingBehavior{TRequest,TResponse}"/> then
    /// <see cref="MetricsBehavior{TRequest,TResponse}"/> as open-generic
    /// <see cref="IPipelineBehavior{TRequest,TResponse}"/> implementations (first-registered = outermost).
    /// Call exactly once, after the logging and validation behaviors.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddResqObservabilityBehaviors(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddPipelineBehavior(typeof(TracingBehavior<,>));
        services.AddPipelineBehavior(typeof(MetricsBehavior<,>));

        return services;
    }
}
