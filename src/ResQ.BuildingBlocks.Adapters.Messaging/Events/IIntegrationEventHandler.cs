using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Adapters.Messaging;

/// <summary>
/// Handles a single integration-event type received from the transport. Consumers register one or more
/// handlers per event; the dispatcher resolves and invokes them all for each inbound message.
/// </summary>
/// <typeparam name="TEvent">The integration-event type this handler consumes.</typeparam>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IntegrationEvent
{
    /// <summary>Handles the integration event.</summary>
    /// <param name="event">The received integration event.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task Handle(TEvent @event, CancellationToken ct);
}
