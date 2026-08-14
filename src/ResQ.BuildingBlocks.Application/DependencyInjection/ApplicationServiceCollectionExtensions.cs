using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ResQ.BuildingBlocks.Application;

/// <summary>
/// Composition-root helpers that wire the application core into an <see cref="IServiceCollection"/>:
/// the dispatcher, handlers, validators, and the cross-cutting pipeline.
/// </summary>
/// <remarks>
/// Behaviors run in DI registration order and <b>first-registered = outermost</b>. The canonical order
/// is <c>Logging → Validation → Tracing → Metrics → Transaction</c>: call <see cref="AddLoggingPipeline"/>
/// then <see cref="AddValidationPipeline"/> here, then the observability behaviors from
/// <c>ServiceDefaults</c>, and register the persistence transaction behavior last (innermost).
/// </remarks>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ISender"/> and <see cref="IDomainEventDispatcher"/> (scoped) and scans the
    /// given assemblies for closed CQRS and domain-event handlers, registering them scoped.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for handler implementations.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    [RequiresUnreferencedCode("Scans assemblies for CQRS and domain-event handler implementations; their types must be preserved when trimming.")]
    public static IServiceCollection AddResqApplication(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        services.AddScoped<ISender, Sender>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        if (assemblies.Length > 0)
        {
            services.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
                    .AsImplementedInterfaces()
                    .WithScopedLifetime()
                .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
                    .AsImplementedInterfaces()
                    .WithScopedLifetime()
                .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
                    .AsImplementedInterfaces()
                    .WithScopedLifetime()
                .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
                    .AsImplementedInterfaces()
                    .WithScopedLifetime());
        }

        return services;
    }

    /// <summary>Registers all FluentValidation validators found in the given assemblies as scoped.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for validators.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddValidatorsFrom(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies)
        {
            services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Scoped);
        }

        return services;
    }

    /// <summary>Registers an open-generic <see cref="IPipelineBehavior{TRequest,TResponse}"/> as transient, preserving order.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="openGenericBehaviorType">The open-generic behavior type (e.g. <c>typeof(MyBehavior&lt;,&gt;)</c>).</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddPipelineBehavior(this IServiceCollection services, Type openGenericBehaviorType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(openGenericBehaviorType);

        services.AddTransient(typeof(IPipelineBehavior<,>), openGenericBehaviorType);
        return services;
    }

    /// <summary>Registers the shipped <see cref="LoggingBehavior{TRequest,TResponse}"/> (outermost of the canonical order).</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddLoggingPipeline(this IServiceCollection services) =>
        services.AddPipelineBehavior(typeof(LoggingBehavior<,>));

    /// <summary>Registers the shipped <see cref="ValidationBehavior{TRequest,TResponse}"/> (after logging).</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddValidationPipeline(this IServiceCollection services) =>
        services.AddPipelineBehavior(typeof(ValidationBehavior<,>));
}
