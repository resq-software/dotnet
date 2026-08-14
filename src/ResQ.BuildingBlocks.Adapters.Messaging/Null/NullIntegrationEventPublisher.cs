using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Adapters.Messaging;

/// <summary>
/// An <see cref="IIntegrationEventPublisher"/> that discards every event. Use it to disable outbound
/// publishing (e.g. in tests, or a service that only consumes) without changing call sites.
/// </summary>
public sealed class NullIntegrationEventPublisher : IIntegrationEventPublisher
{
    /// <summary>No-op — the event is discarded.</summary>
    /// <param name="event">The event that would be published.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A completed task.</returns>
    public Task PublishAsync(IntegrationEvent @event, CancellationToken ct = default) =>
        Task.CompletedTask;
}
