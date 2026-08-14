using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Testing;

/// <summary>
/// An <see cref="IIntegrationEventPublisher"/> that captures every published integration event
/// instead of putting it on a transport, letting a test assert what would have been published.
/// </summary>
public sealed class RecordingIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly List<IntegrationEvent> _published = [];

    /// <summary>Gets the integration events captured so far, in publish order.</summary>
    public IReadOnlyList<IntegrationEvent> Published => _published;

    /// <summary>Captures the supplied integration event and completes.</summary>
    /// <param name="event">The integration event being published.</param>
    /// <param name="ct">Ignored.</param>
    /// <returns>A completed task.</returns>
    public Task PublishAsync(IntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        _published.Add(@event);
        return Task.CompletedTask;
    }
}
