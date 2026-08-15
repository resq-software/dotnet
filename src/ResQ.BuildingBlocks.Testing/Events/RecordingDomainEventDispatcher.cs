using System.Collections.Concurrent;
using ResQ.BuildingBlocks.Application;
using ResQ.BuildingBlocks.Domain;

namespace ResQ.BuildingBlocks.Testing;

/// <summary>
/// An <see cref="IDomainEventDispatcher"/> that captures every dispatched domain event instead of
/// invoking handlers, letting a test assert exactly which events an operation raised.
/// </summary>
public sealed class RecordingDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly ConcurrentQueue<IDomainEvent> _dispatched = new();

    /// <summary>Gets a snapshot of the domain events captured so far, in dispatch order.</summary>
    public IReadOnlyList<IDomainEvent> Dispatched => _dispatched.ToArray();

    /// <summary>Captures the supplied domain events and completes.</summary>
    /// <param name="domainEvents">The domain events being dispatched.</param>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>A completed task.</returns>
    public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);
        foreach (var domainEvent in domainEvents)
        {
            _dispatched.Enqueue(domainEvent);
        }

        return Task.CompletedTask;
    }
}
