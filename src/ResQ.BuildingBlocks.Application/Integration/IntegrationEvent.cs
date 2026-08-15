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

    /// <summary>
    /// The logical event-type name used for routing and type resolution. Defaults to the namespace-qualified
    /// CLR type name (<see cref="System.Type.FullName"/>) so two events that share a simple name in different
    /// namespaces stay distinct on the wire and resolve to the correct CLR type end-to-end (publisher →
    /// envelope/outbox → dispatcher). Falls back to the simple name only for the (open-generic/edge) types
    /// whose <see cref="System.Type.FullName"/> is <see langword="null"/>.
    /// </summary>
    public virtual string EventType => GetType().FullName ?? GetType().Name;
}
