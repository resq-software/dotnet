using System.Diagnostics.CodeAnalysis;

namespace ResQ.BuildingBlocks.Application;

/// <summary>Maps logical event-type names to CLR types so consumers can deserialize incoming messages.</summary>
public interface IIntegrationEventTypeRegistry
{
    /// <summary>Resolves the CLR type for a logical event-type name.</summary>
    /// <param name="eventType">The logical event-type name.</param>
    /// <param name="type">The resolved CLR type when found.</param>
    /// <returns><see langword="true"/> when a type is registered for <paramref name="eventType"/>.</returns>
    bool TryResolve(string eventType, [NotNullWhen(true)] out Type? type);

    /// <summary>Registers an integration-event CLR type under its logical name.</summary>
    /// <param name="eventType">The integration-event CLR type to register.</param>
    void Register(Type eventType);
}
