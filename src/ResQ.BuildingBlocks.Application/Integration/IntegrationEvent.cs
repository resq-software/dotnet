namespace ResQ.BuildingBlocks.Application;

/// <summary>
/// The base for an integration event — a fact published across service boundaries. Adapters serialize
/// and transport concrete subtypes; the type lives in the application core so persistence (outbox) and
/// messaging adapters share one contract without referencing each other.
/// </summary>
public abstract record IntegrationEvent
{
    /// <summary>A unique identifier for this event instance.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>When the event occurred (UTC).</summary>
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>The logical event type name used for routing and type resolution.</summary>
    public virtual string EventType => GetType().Name;
}
