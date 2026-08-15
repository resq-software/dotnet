using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Adapters.Messaging;

/// <summary>
/// The default <see cref="IIntegrationEventTypeRegistry"/> — an in-memory dictionary keyed by each
/// integration event's namespace-qualified CLR type name (<see cref="System.Type.FullName"/>, the default
/// <see cref="IntegrationEvent.EventType"/>). Keying on the full name — rather than the simple
/// <see cref="System.Reflection.MemberInfo.Name"/> — keeps two events that share a simple name in different
/// namespaces distinct, so a message can never deserialize into the wrong CLR type. Populated by scanning consumer
/// assemblies for <see cref="IntegrationEvent"/> subtypes at registration time (and again, idempotently,
/// each time a publisher registers the type it is about to send).
/// </summary>
public sealed class DictionaryIntegrationEventTypeRegistry : IIntegrationEventTypeRegistry
{
    private readonly ConcurrentDictionary<string, Type> _types = new(StringComparer.Ordinal);

    /// <summary>Resolves the CLR type registered under a logical event-type name.</summary>
    /// <param name="eventType">The logical event-type name.</param>
    /// <param name="type">The resolved CLR type when found.</param>
    /// <returns><see langword="true"/> when a type is registered for <paramref name="eventType"/>.</returns>
    public bool TryResolve(string eventType, [NotNullWhen(true)] out Type? type) =>
        _types.TryGetValue(eventType, out type);

    /// <summary>
    /// Registers an <see cref="IntegrationEvent"/> subtype under its namespace-qualified CLR type name
    /// (<see cref="System.Type.FullName"/>). Re-registering the same type is an idempotent no-op; registering
    /// a <i>different</i> type under a key already taken is a wire-identity collision and throws, so the
    /// ambiguity surfaces at registration time rather than silently mis-deserializing messages at runtime.
    /// </summary>
    /// <param name="eventType">The integration-event CLR type to register.</param>
    /// <exception cref="ArgumentException"><paramref name="eventType"/> is not an <see cref="IntegrationEvent"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// A different type is already registered under <paramref name="eventType"/>'s key.
    /// </exception>
    public void Register(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        if (!typeof(IntegrationEvent).IsAssignableFrom(eventType))
        {
            throw new ArgumentException(
                $"Type '{eventType.FullName}' is not an {nameof(IntegrationEvent)}.",
                nameof(eventType));
        }

        var key = eventType.FullName ?? eventType.Name;
        var registered = _types.GetOrAdd(key, eventType);
        if (registered != eventType)
        {
            throw new InvalidOperationException(
                $"Cannot register integration event '{eventType.AssemblyQualifiedName}' under key '{key}': " +
                $"a different type '{registered.AssemblyQualifiedName}' is already registered under that key. " +
                "Two integration events resolve to the same wire identity; give one a distinct namespace or " +
                "override EventType.");
        }
    }
}
