using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Adapters.Messaging;

/// <summary>
/// The default <see cref="IIntegrationEventTypeRegistry"/> — an in-memory dictionary keyed by each
/// integration event's CLR type name (its default <see cref="IntegrationEvent.EventType"/>). Populated
/// by scanning consumer assemblies for <see cref="IntegrationEvent"/> subtypes at registration time.
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

    /// <summary>Registers an <see cref="IntegrationEvent"/> subtype under its CLR type name.</summary>
    /// <param name="eventType">The integration-event CLR type to register.</param>
    /// <exception cref="ArgumentException"><paramref name="eventType"/> is not an <see cref="IntegrationEvent"/>.</exception>
    public void Register(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        if (!typeof(IntegrationEvent).IsAssignableFrom(eventType))
        {
            throw new ArgumentException(
                $"Type '{eventType.FullName}' is not an {nameof(IntegrationEvent)}.",
                nameof(eventType));
        }

        _types[eventType.Name] = eventType;
    }
}
