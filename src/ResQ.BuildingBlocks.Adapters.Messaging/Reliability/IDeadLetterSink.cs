using Microsoft.Extensions.Logging;

namespace ResQ.BuildingBlocks.Adapters.Messaging;

/// <summary>
/// Receives messages that could not be processed after the retry budget was exhausted. Implementations
/// route them somewhere durable (a dead-letter queue, a table, a log) for later inspection or replay.
/// </summary>
public interface IDeadLetterSink
{
    /// <summary>Sends a poison message to the dead-letter destination.</summary>
    /// <param name="message">The message that failed processing.</param>
    /// <param name="error">The last error observed.</param>
    /// <param name="attempts">The number of attempts made before giving up.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task SendAsync(MessageEnvelope message, Exception error, int attempts, CancellationToken ct);
}

/// <summary>
/// The default <see cref="IDeadLetterSink"/> — logs the poison message at error level and drops it.
/// Suitable for development and services without a durable dead-letter store.
/// </summary>
/// <param name="logger">The logger used to record dead-lettered messages.</param>
public sealed class LoggingDeadLetterSink(ILogger<LoggingDeadLetterSink> logger) : IDeadLetterSink
{
    /// <summary>Logs the dead-lettered message and drops it.</summary>
    /// <param name="message">The message that failed processing.</param>
    /// <param name="error">The last error observed.</param>
    /// <param name="attempts">The number of attempts made before giving up.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A completed task.</returns>
    public Task SendAsync(MessageEnvelope message, Exception error, int attempts, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        logger.LogError(
            error,
            "Dead-lettering message {MessageId} of type {MessageType} after {Attempts} attempt(s).",
            message.MessageId,
            message.MessageType,
            attempts);
        return Task.CompletedTask;
    }
}

/// <summary>An <see cref="IDeadLetterSink"/> that silently discards poison messages (no-op).</summary>
public sealed class NullDeadLetterSink : IDeadLetterSink
{
    /// <summary>No-op — the poison message is discarded.</summary>
    /// <param name="message">The message that failed processing.</param>
    /// <param name="error">The last error observed.</param>
    /// <param name="attempts">The number of attempts made before giving up.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A completed task.</returns>
    public Task SendAsync(MessageEnvelope message, Exception error, int attempts, CancellationToken ct) =>
        Task.CompletedTask;
}
