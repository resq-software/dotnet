namespace ResQ.BuildingBlocks.Application;

/// <summary>Publishes integration events onto the transport (broker, in-memory channel, or outbox relay).</summary>
public interface IIntegrationEventPublisher
{
    /// <summary>Publishes an integration event.</summary>
    /// <param name="event">The event to publish.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task PublishAsync(IntegrationEvent @event, CancellationToken ct = default);
}
