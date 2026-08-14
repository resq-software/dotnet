using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Adapters.Persistence;

/// <summary>
/// Stages an integration event for reliable, at-least-once delivery by writing it to the outbox in the
/// current unit of work, so it commits atomically with the aggregate change that raised it.
/// </summary>
public interface IOutbox
{
    /// <summary>Stages an integration event onto the outbox (committed with the current unit of work).</summary>
    /// <param name="event">The integration event to enqueue.</param>
    void Enqueue(IntegrationEvent @event);
}
