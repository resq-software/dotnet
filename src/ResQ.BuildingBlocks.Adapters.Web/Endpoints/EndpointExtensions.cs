using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ResQ.BuildingBlocks.Adapters.Web;

/// <summary>
/// Discovers <see cref="IEndpoint"/> implementations by assembly scan and maps them at startup.
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Registers every concrete <see cref="IEndpoint"/> found in the given assemblies as a transient
    /// service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The same service collection for chaining.</returns>
    [RequiresUnreferencedCode("Scans assemblies for IEndpoint implementations; their types must be preserved when trimming.")]
    public static IServiceCollection AddResqEndpoints(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type is { IsAbstract: false, IsInterface: false } && typeof(IEndpoint).IsAssignableFrom(type))
                {
                    services.AddTransient(typeof(IEndpoint), type);
                }
            }
        }

        return services;
    }

    /// <summary>
    /// Resolves every registered <see cref="IEndpoint"/> from the application's services and invokes
    /// <see cref="IEndpoint.MapEndpoint"/> on each.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapResqEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        foreach (var endpoint in app.ServiceProvider.GetServices<IEndpoint>())
        {
            endpoint.MapEndpoint(app);
        }

        return app;
    }
}
