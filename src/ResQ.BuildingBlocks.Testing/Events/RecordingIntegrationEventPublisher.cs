using System.Collections.Concurrent;
using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Testing;

/// <summary>
/// An <see cref="IIntegrationEventPublisher"/> that captures every published integration event
/// instead of putting it on a transport, letting a test assert what would have been published.
/// </summary>
public sealed class RecordingIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly ConcurrentQueue<IntegrationEvent> _published = new();

    /// <summary>Gets a snapshot of the integration events captured so far, in publish order.</summary>
    public IReadOnlyList<IntegrationEvent> Published => _published.ToArray();

    /// <summary>Captures the supplied integration event and completes.</summary>
    /// <param name="event">The integration event being published.</param>
    /// <param name="ct">Ignored.</param>
    /// <returns>A completed task.</returns>
    public Task PublishAsync(IntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        _published.Enqueue(@event);
        return Task.CompletedTask;
    }
}
