using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Testing;

/// <summary>
/// Registration helpers that swap the production driven-port implementations for the in-memory test
/// doubles in this package.
/// </summary>
public static class TestDoublesExtensions
{
    /// <summary>
    /// Replaces the registrations for <see cref="IClock"/>, <see cref="IUnitOfWork"/>,
    /// <see cref="IDomainEventDispatcher"/>, and <see cref="IIntegrationEventPublisher"/> with the
    /// recording/fake doubles (<see cref="FakeClock"/>, <see cref="NoopUnitOfWork"/>,
    /// <see cref="RecordingDomainEventDispatcher"/>, <see cref="RecordingIntegrationEventPublisher"/>),
    /// each as a singleton so a test can resolve and inspect the same instance.
    /// </summary>
    /// <param name="services">The service collection to mutate.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddTestDoubles(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Replace(ServiceDescriptor.Singleton<IClock, FakeClock>());
        services.Replace(ServiceDescriptor.Singleton<IUnitOfWork, NoopUnitOfWork>());
        services.Replace(ServiceDescriptor.Singleton<IDomainEventDispatcher, RecordingDomainEventDispatcher>());
        services.Replace(ServiceDescriptor.Singleton<IIntegrationEventPublisher, RecordingIntegrationEventPublisher>());

        return services;
    }
}
