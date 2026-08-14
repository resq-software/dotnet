namespace ResQ.BuildingBlocks.Adapters.Persistence;

/// <summary>
/// A record that a given handler has already processed a message (inbox idempotency). The composite key
/// <c>(MessageId, Handler)</c> lets several handlers each process the same message once.
/// </summary>
public sealed class InboxMessage
{
    /// <summary>The unique identifier of the processed message.</summary>
    public string MessageId { get; set; } = null!;

    /// <summary>The logical handler that processed the message.</summary>
    public string Handler { get; set; } = null!;

    /// <summary>When the message was processed (UTC).</summary>
    public DateTimeOffset ProcessedOnUtc { get; set; }
}
