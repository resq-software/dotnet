using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Adapters.Messaging;

/// <summary>
/// Turns an inbound <see cref="MessageEnvelope"/> into a typed integration event and fans it out to every
/// registered <see cref="IIntegrationEventHandler{TEvent}"/>. The event's CLR type is resolved from the
/// registry, the body deserialized, and handlers resolved from the current DI scope. Per-type invokers
/// are reflection-built once and cached.
/// </summary>
/// <remarks>
/// <para>
/// <b>Per-handler fan-out idempotency.</b> The consumer's retry pipeline re-invokes
/// <see cref="DispatchAsync"/> on the <i>same</i> dispatcher instance (it is scoped per message), so a
/// handler that already succeeded on an earlier attempt would otherwise run again when a <i>later</i>
/// handler forces a retry. This dispatcher records which handler types have completed for the message and
/// skips them on subsequent attempts, so each handler runs at most once per delivery even though the
/// message-level idempotency key is per (message, consumer) rather than per handler.
/// </para>
/// <para>
/// This in-memory tracking only spans the retries of a single delivery; a redelivery (a fresh message
/// scope, hence a fresh dispatcher) starts clean. Handlers must therefore still be individually idempotent
/// to tolerate at-least-once redelivery — this only removes the redundant re-execution within one delivery.
/// </para>
/// </remarks>
/// <param name="provider">The (scoped) provider from which handlers are resolved.</param>
/// <param name="serializer">The serializer used to deserialize the body.</param>
/// <param name="registry">The registry mapping the message type to a CLR type.</param>
public sealed class IntegrationEventDispatcher(
    IServiceProvider provider,
    IMessageSerializer serializer,
    IIntegrationEventTypeRegistry registry)
{
    private static readonly ConcurrentDictionary<Type, HandlerInvoker> Invokers = new();

    // Handler types that have already completed, keyed by message id so re-running the fan-out (a retry from
    // the consumer pipeline) skips handlers that already succeeded instead of re-executing them. Keyed by
    // message id as well as handler type so the tracking stays correct even if this dispatcher is ever
    // resolved with a lifetime broader than the per-message scope.
    private readonly ConcurrentDictionary<(string MessageId, Type Handler), byte> _completedHandlers = new();

    /// <summary>Deserializes the message and invokes all handlers registered for its event type.</summary>
    /// <param name="message">The inbound message envelope.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that completes when every handler has run.</returns>
    /// <exception cref="InvalidOperationException">
    /// The message type is not registered, or its body could not be deserialized.
    /// </exception>
    [RequiresDynamicCode("Builds a closed generic handler invoker per event CLR type via MakeGenericType.")]
    public async Task DispatchAsync(MessageEnvelope message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!registry.TryResolve(message.MessageType, out var clrType))
        {
            throw new InvalidOperationException(
                $"No integration-event type is registered for '{message.MessageType}'.");
        }

        if (serializer.Deserialize(message.Body.Span, clrType) is not IntegrationEvent @event)
        {
            throw new InvalidOperationException(
                $"Message '{message.MessageId}' of type '{message.MessageType}' deserialized to null.");
        }

        var invoker = Invokers.GetOrAdd(
            clrType,
            static type => (HandlerInvoker)Activator.CreateInstance(
                typeof(HandlerInvoker<>).MakeGenericType(type))!);

        await invoker.InvokeAsync(provider, @event, message.MessageId, _completedHandlers, ct).ConfigureAwait(false);
    }

    private abstract class HandlerInvoker
    {
        public abstract Task InvokeAsync(
            IServiceProvider provider,
            IntegrationEvent @event,
            string messageId,
            ConcurrentDictionary<(string MessageId, Type Handler), byte> completedHandlers,
            CancellationToken ct);
    }

    [SuppressMessage("Performance", "CA1812", Justification = "Instantiated via reflection, one per event CLR type.")]
    private sealed class HandlerInvoker<TEvent> : HandlerInvoker
        where TEvent : IntegrationEvent
    {
        public override async Task InvokeAsync(
            IServiceProvider provider,
            IntegrationEvent @event,
            string messageId,
            ConcurrentDictionary<(string MessageId, Type Handler), byte> completedHandlers,
            CancellationToken ct)
        {
            var typed = (TEvent)@event;
            foreach (var handler in provider.GetServices<IIntegrationEventHandler<TEvent>>())
            {
                var key = (messageId, handler.GetType());
                if (completedHandlers.ContainsKey(key))
                {
                    // Already succeeded on an earlier fan-out attempt for this message — do not re-run it.
                    continue;
                }

                await handler.Handle(typed, ct).ConfigureAwait(false);
                completedHandlers[key] = 0;
            }
        }
    }
}
