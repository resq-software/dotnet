namespace ResQ.BuildingBlocks.Adapters.Messaging;

/// <summary>
/// A source of inbound messages for a consumer to drain. A transport adapter implements this to yield
/// envelopes and to acknowledge or negatively-acknowledge each one after processing.
/// </summary>
public interface IMessageSource
{
    /// <summary>Streams inbound messages until the token is cancelled.</summary>
    /// <param name="ct">A token that stops the stream.</param>
    /// <returns>An asynchronous sequence of message envelopes.</returns>
    IAsyncEnumerable<MessageEnvelope> ReadAllAsync(CancellationToken ct);

    /// <summary>Acknowledges that a message was processed successfully.</summary>
    /// <param name="message">The processed message.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task AcknowledgeAsync(MessageEnvelope message, CancellationToken ct);

    /// <summary>Negatively-acknowledges a message that failed processing (transport may re-deliver).</summary>
    /// <param name="message">The failed message.</param>
    /// <param name="error">The error that caused the failure.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task NackAsync(MessageEnvelope message, Exception error, CancellationToken ct);
}
