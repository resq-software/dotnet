using ResQ.BuildingBlocks.Application;
using ResQ.Service.Domain;

namespace ResQ.Service.Application;

/// <summary>
/// Reacts to the <see cref="SampleCreated"/> domain event by publishing a
/// <see cref="SampleCreatedIntegrationEvent"/> through the application's
/// <see cref="IIntegrationEventPublisher"/> port.
/// </summary>
public sealed class SampleCreatedHandler(IIntegrationEventPublisher publisher)
    : IDomainEventHandler<SampleCreated>
{
    /// <inheritdoc />
    public Task Handle(SampleCreated domainEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var integrationEvent = new SampleCreatedIntegrationEvent(domainEvent.SampleId, domainEvent.Name, domainEvent.Quantity)
        {
            OccurredOnUtc = domainEvent.OccurredOnUtc,
        };

        // Transactional alternative: with AddResqPersistence(setup => setup.UseOutbox = true), an injected
        // IOutbox.Enqueue(...) here commits atomically with the aggregate write via the EfUnitOfWork drain
        // loop, instead of publishing directly onto the transport.
        return publisher.PublishAsync(integrationEvent, ct);
    }
}
