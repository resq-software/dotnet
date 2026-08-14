using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResQ.BuildingBlocks.Adapters.Messaging;
using ResQ.BuildingBlocks.Adapters.Persistence;
using Widgets.Application;

namespace Widgets.Infrastructure;

/// <summary>Composition root for the Widgets persistence + messaging adapters.</summary>
public static class WidgetsInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Wires EF Core (Npgsql, snake_case) with the ResQ persistence adapter, the widget repository, and
    /// the in-memory messaging transport.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration supplying the <c>Widgets</c> connection string.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddWidgetsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddResqPersistence<WidgetsDbContext>(
            (_, options) =>
            {
                var connectionString = configuration.GetConnectionString("Widgets")
                    ?? throw new InvalidOperationException("Connection string 'Widgets' is not configured.");
                options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
            },
            setup => setup.UseOutbox = false);

        services.AddResqRepositories(typeof(WidgetRepository).Assembly);
        services.AddResqMessaging(builder => builder.UseInMemory(), typeof(WidgetCreatedIntegrationEvent).Assembly);

        return services;
    }
}
