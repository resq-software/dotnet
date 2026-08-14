using ResQ.BuildingBlocks.Domain;

namespace ResQ.BuildingBlocks.Application;

/// <summary>Handles a domain event of type <typeparamref name="TEvent"/> after its aggregate is persisted.</summary>
/// <typeparam name="TEvent">The domain event handled.</typeparam>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>Reacts to the domain event.</summary>
    /// <param name="domainEvent">The event that was raised.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task Handle(TEvent domainEvent, CancellationToken ct);
}
