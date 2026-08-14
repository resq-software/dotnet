namespace ResQ.BuildingBlocks.Adapters.Messaging;

/// <summary>
/// The transport-neutral wire form of a published message: identity, logical type, timestamp, headers,
/// and an opaque serialized body. Adapters translate their broker's native record to and from this shape.
/// </summary>
/// <param name="MessageId">A unique identifier for this message (used for idempotency and acknowledgement).</param>
/// <param name="MessageType">The logical event-type name used to resolve the CLR type on the read side.</param>
/// <param name="OccurredOnUtc">When the originating event occurred (UTC).</param>
/// <param name="Headers">Transport headers (e.g. content type, correlation id).</param>
/// <param name="Body">The serialized payload.</param>
public sealed record MessageEnvelope(
    string MessageId,
    string MessageType,
    DateTimeOffset OccurredOnUtc,
    IReadOnlyDictionary<string, string> Headers,
    ReadOnlyMemory<byte> Body);
