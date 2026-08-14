using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Adapters.Persistence;

/// <summary>Registration helpers that wire the EF Core persistence adapter into a service collection.</summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers a pooled <typeparamref name="TContext"/>, a scoped <see cref="DbContext"/> relay, the
    /// scoped <see cref="EfUnitOfWork"/> and <see cref="EfIdempotencyStore"/>, and — depending on the
    /// options — the audit interceptor, transactional outbox, and command transaction behavior.
    /// </summary>
    /// <typeparam name="TContext">The application's <see cref="DbContext"/> type.</typeparam>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configure">Configures the context (the consumer selects a provider here).</param>
    /// <param name="setup">Optionally tunes the persistence options.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddResqPersistence<TContext>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configure,
        Action<ResqPersistenceOptions>? setup = null)
        where TContext : DbContext
    {
        var options = new ResqPersistenceOptions();
        setup?.Invoke(options);

        if (options.EnableAudit)
        {
            services.AddSingleton<AuditInterceptor>();
        }

        services.AddDbContextPool<TContext>((provider, builder) =>
        {
            if (options.EnableAudit)
            {
                builder.AddInterceptors(provider.GetRequiredService<AuditInterceptor>());
            }

            configure(provider, builder);
        });

        // Relay so EfUnitOfWork / Repository / EfOutbox can resolve the ambient DbContext.
        services.AddScoped<DbContext>(provider => provider.GetRequiredService<TContext>());
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();

        if (options.UseOutbox)
        {
            services.AddScoped<IOutbox, EfOutbox>();
            services.AddHostedService<OutboxRelay>();
        }

        if (options.UseTransactionBehavior)
        {
            // Registered here so it lands last in the pipeline — the innermost behavior around the handler.
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        }

        return services;
    }

    /// <summary>
    /// Scans the given assemblies for concrete <see cref="Repository{TAggregate,TId}"/> and
    /// <see cref="ReadRepository{TAggregate,TId}"/> derivations and registers each as its implemented
    /// interfaces, scoped.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="assemblies">The assemblies to scan; defaults to the calling assembly when empty.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddResqRepositories(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        var targets = assemblies.Length == 0 ? [Assembly.GetCallingAssembly()] : assemblies;

        services.Scan(scan => scan
            .FromAssemblies(targets)
            .AddClasses(classes => classes.AssignableTo(typeof(IRepository<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IReadRepository<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        return services;
    }
}
