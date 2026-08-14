namespace ResQ.BuildingBlocks.Application;

/// <summary>Serializes and deserializes message payloads to and from bytes for transport and the outbox.</summary>
public interface IMessageSerializer
{
    /// <summary>Serializes a value of the given type to bytes.</summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="type">The declared type to serialize as.</param>
    byte[] Serialize(object value, Type type);

    /// <summary>Deserializes bytes into an instance of the given type, or <see langword="null"/>.</summary>
    /// <param name="data">The serialized payload.</param>
    /// <param name="type">The target CLR type.</param>
    object? Deserialize(ReadOnlySpan<byte> data, Type type);

    /// <summary>The MIME content type this serializer produces (e.g. <c>application/json</c>).</summary>
    string ContentType { get; }
}
