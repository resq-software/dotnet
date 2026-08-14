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
#pragma warning disable RS0030 // Banned API: DateTimeOffset.UtcNow.
    // Legitimate use: this is the record's default-value initializer for a fact's occurrence time,
    // evaluated once at construction where no IClock is in scope (property initializers cannot take a
    // dependency). Callers that need deterministic time set OccurredOnUtc explicitly via 'with'.
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
#pragma warning restore RS0030

    /// <summary>The logical event type name used for routing and type resolution.</summary>
    public virtual string EventType => GetType().Name;
}
