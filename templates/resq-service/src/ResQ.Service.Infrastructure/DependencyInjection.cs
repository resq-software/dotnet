using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResQ.BuildingBlocks.Adapters.Persistence;
#if (IncludeMessaging)
using ResQ.BuildingBlocks.Adapters.Messaging;
#endif
using ResQ.Service.Application;

namespace ResQ.Service.Infrastructure;

/// <summary>Composition root for the service's persistence (and optional messaging) adapters.</summary>
public static class SampleInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Wires EF Core with the ResQ persistence adapter, the sample repository, and (when included) the
    /// in-memory messaging transport.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Configuration supplying the <c>Database</c> connection string.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddSampleInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddResqPersistence<SampleDbContext>(
            (_, options) =>
            {
                var connectionString = configuration.GetConnectionString("Database")
                    ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");
#if (DatabaseProvider == "postgres")
                options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
#else
                options.UseSqlite(connectionString);
#endif
            },
#if (IncludeOutbox)
            // UseOutbox = true also registers IOutbox (EfOutbox) and the OutboxRelay hosted service.
            setup => setup.UseOutbox = true);
#else
            setup => setup.UseOutbox = false);
#endif

        services.AddResqRepositories(typeof(SampleRepository).Assembly);
#if (IncludeMessaging)
        services.AddResqMessaging(builder => builder.UseInMemory(), typeof(SampleCreatedIntegrationEvent).Assembly);
#endif

        return services;
    }
}
