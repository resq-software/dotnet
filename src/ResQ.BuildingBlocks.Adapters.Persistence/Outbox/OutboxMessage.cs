namespace ResQ.BuildingBlocks.Adapters.Persistence;

/// <summary>
/// A persisted, not-yet-published integration event (transactional outbox pattern). The row is written
/// in the same transaction as the aggregate change and later relayed by the <see cref="OutboxRelay"/>.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>The event's unique identifier and the row's primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>The logical event type used to resolve the CLR type when relaying.</summary>
    public string Type { get; set; } = null!;

    /// <summary>The serialized event payload, as bytes (mapped to a BLOB column).</summary>
    public byte[] Content { get; set; } = null!;

    /// <summary>When the event occurred (UTC).</summary>
    public DateTimeOffset OccurredOnUtc { get; set; }

    /// <summary>When the event was successfully published, or <see langword="null"/> while pending.</summary>
    public DateTimeOffset? ProcessedOnUtc { get; set; }

    /// <summary>How many relay attempts have been made.</summary>
    public int Attempts { get; set; }

    /// <summary>The last relay error, or <see langword="null"/> when none.</summary>
    public string? Error { get; set; }
}
