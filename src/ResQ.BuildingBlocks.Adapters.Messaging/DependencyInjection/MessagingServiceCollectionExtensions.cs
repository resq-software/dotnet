using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Adapters.Messaging;

/// <summary>Composition-root helpers that wire the broker-agnostic messaging adapter into a container.</summary>
public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the messaging core — the JSON serializer, an event-type registry populated from the
    /// scanned assemblies, the integration-event dispatcher, the discovered
    /// <see cref="IIntegrationEventHandler{TEvent}"/> implementations, and the default no-op idempotency
    /// store and logging dead-letter sink — then applies the optional transport configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An optional callback to pick the transport and reliability components.</param>
    /// <param name="handlerAssemblies">Assemblies scanned for integration events and their handlers.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddResqMessaging(
        this IServiceCollection services,
        Action<MessagingBuilder>? configure = null,
        params Assembly[] handlerAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        handlerAssemblies ??= [];

        services.AddOptions();

        services.TryAddSingleton<IMessageSerializer, SystemTextJsonMessageSerializer>();
        services.TryAddScoped<IntegrationEventDispatcher>();
        services.TryAddSingleton<IIdempotencyStore, NullIdempotencyStore>();
        services.TryAddSingleton<IDeadLetterSink, LoggingDeadLetterSink>();

        var registry = new DictionaryIntegrationEventTypeRegistry();
        foreach (var assembly in handlerAssemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsAbstract && typeof(IntegrationEvent).IsAssignableFrom(type))
                {
                    registry.Register(type);
                }
            }
        }

        services.TryAddSingleton<IIntegrationEventTypeRegistry>(registry);

        if (handlerAssemblies.Length > 0)
        {
            services.Scan(scan => scan
                .FromAssemblies(handlerAssemblies)
                .AddClasses(classes => classes.AssignableTo(typeof(IIntegrationEventHandler<>)), publicOnly: false)
                    .AsImplementedInterfaces()
                    .WithScopedLifetime());
        }

        if (configure is not null)
        {
            configure(new MessagingBuilder(services));
        }

        return services;
    }
}
