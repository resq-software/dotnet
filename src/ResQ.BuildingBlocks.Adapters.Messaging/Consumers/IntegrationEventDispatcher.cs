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
/// <param name="provider">The (scoped) provider from which handlers are resolved.</param>
/// <param name="serializer">The serializer used to deserialize the body.</param>
/// <param name="registry">The registry mapping the message type to a CLR type.</param>
public sealed class IntegrationEventDispatcher(
    IServiceProvider provider,
    IMessageSerializer serializer,
    IIntegrationEventTypeRegistry registry)
{
    private static readonly ConcurrentDictionary<Type, HandlerInvoker> Invokers = new();

    /// <summary>Deserializes the message and invokes all handlers registered for its event type.</summary>
    /// <param name="message">The inbound message envelope.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task that completes when every handler has run.</returns>
    /// <exception cref="InvalidOperationException">
    /// The message type is not registered, or its body could not be deserialized.
    /// </exception>
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

        await invoker.InvokeAsync(provider, @event, ct).ConfigureAwait(false);
    }

    private abstract class HandlerInvoker
    {
        public abstract Task InvokeAsync(IServiceProvider provider, IntegrationEvent @event, CancellationToken ct);
    }

    [SuppressMessage("Performance", "CA1812", Justification = "Instantiated via reflection, one per event CLR type.")]
    private sealed class HandlerInvoker<TEvent> : HandlerInvoker
        where TEvent : IntegrationEvent
    {
        public override async Task InvokeAsync(IServiceProvider provider, IntegrationEvent @event, CancellationToken ct)
        {
            var typed = (TEvent)@event;
            foreach (var handler in provider.GetServices<IIntegrationEventHandler<TEvent>>())
            {
                await handler.Handle(typed, ct).ConfigureAwait(false);
            }
        }
    }
}
