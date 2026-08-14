using ResQ.BuildingBlocks.Application;
using Widgets.Domain;

namespace Widgets.Application;

/// <summary>
/// Reacts to the <see cref="WidgetCreated"/> domain event by publishing a
/// <see cref="WidgetCreatedIntegrationEvent"/> through the application's
/// <see cref="IIntegrationEventPublisher"/> port.
/// </summary>
public sealed class WidgetCreatedHandler(IIntegrationEventPublisher publisher)
    : IDomainEventHandler<WidgetCreated>
{
    /// <inheritdoc />
    public Task Handle(WidgetCreated domainEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var integrationEvent = new WidgetCreatedIntegrationEvent(domainEvent.WidgetId, domainEvent.Name, domainEvent.Quantity)
        {
            OccurredOnUtc = domainEvent.OccurredOnUtc,
        };

        // Transactional alternative: with AddResqPersistence(setup => setup.UseOutbox = true), an injected
        // IOutbox.Enqueue(...) here commits atomically with the aggregate write via the EfUnitOfWork drain
        // loop, instead of publishing directly onto the transport.
        return publisher.PublishAsync(integrationEvent, ct);
    }
}
