using System.Collections.Concurrent;
using System.Threading.Channels;
using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Adapters.Messaging;

/// <summary>
/// The default in-memory transport: an unbounded <see cref="Channel{T}"/> that is both an
/// <see cref="IIntegrationEventPublisher"/> (write side) and an <see cref="IMessageSource"/> (read side).
/// Publishing serializes an event into a <see cref="MessageEnvelope"/> and enqueues it; the consumer
/// drains it in-process. Ideal as a loopback for tests and single-process services.
/// </summary>
/// <param name="serializer">The serializer used to encode event bodies.</param>
/// <param name="registry">The registry, kept in sync so published types resolve on the read side.</param>
public sealed class ChannelMessageBroker(
    IMessageSerializer serializer,
    IIntegrationEventTypeRegistry registry) : IIntegrationEventPublisher, IMessageSource
{
    // Redelivery safeguard: the maximum number of times NackAsync will requeue a single message before it
    // is dropped, so a message that keeps failing cannot loop through the channel forever.
    private const int MaxRedeliveries = 5;

    private readonly Channel<MessageEnvelope> _channel = Channel.CreateUnbounded<MessageEnvelope>();

    // Tracks how many times each message has been requeued via NackAsync. Cleared by AcknowledgeAsync (or
    // once the cap is hit), keeping the map bounded to the set of in-flight, previously-nacked messages.
    private readonly ConcurrentDictionary<string, int> _deliveryAttempts = new(StringComparer.Ordinal);

    /// <summary>Serializes the event to an envelope and enqueues it onto the in-memory channel.</summary>
    /// <param name="event">The event to publish.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that completes once the envelope is enqueued.</returns>
    public async Task PublishAsync(IntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var clrType = @event.GetType();
        registry.Register(clrType);

        var body = serializer.Serialize(@event, clrType);
        var envelope = new MessageEnvelope(
            @event.Id.ToString(),
            @event.EventType,
            @event.OccurredOnUtc,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["content-type"] = serializer.ContentType },
            body);

        await _channel.Writer.WriteAsync(envelope, ct).ConfigureAwait(false);
    }

    /// <summary>Streams enqueued envelopes until the token is cancelled.</summary>
    /// <param name="ct">A token that stops the stream.</param>
    /// <returns>An asynchronous sequence of message envelopes.</returns>
    public IAsyncEnumerable<MessageEnvelope> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);

    /// <summary>
    /// Acknowledges a processed message. The in-memory transport has no broker-side state to commit, so this
    /// only clears the message's redelivery tracking.
    /// </summary>
    /// <param name="message">The processed message.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A completed task.</returns>
    public Task AcknowledgeAsync(MessageEnvelope message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        _deliveryAttempts.TryRemove(message.MessageId, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Re-enqueues a failed message for redelivery, up to <see cref="MaxRedeliveries"/> attempts. Once that
    /// cap is exceeded the message is dropped instead of requeued, so a poison message cannot loop forever.
    /// </summary>
    /// <param name="message">The failed message.</param>
    /// <param name="error">The error that caused the failure.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that completes once the message is re-enqueued or dropped.</returns>
    public async Task NackAsync(MessageEnvelope message, Exception error, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        var attempts = _deliveryAttempts.AddOrUpdate(message.MessageId, 1, static (_, current) => current + 1);
        if (attempts > MaxRedeliveries)
        {
            _deliveryAttempts.TryRemove(message.MessageId, out _);
            return;
        }

        await _channel.Writer.WriteAsync(message, ct).ConfigureAwait(false);
    }
}
