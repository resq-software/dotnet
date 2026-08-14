using System.Text.Json;
using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Adapters.Messaging;

/// <summary>
/// An <see cref="IMessageSerializer"/> backed by <see cref="System.Text.Json"/>. Serializes payloads to
/// UTF-8 bytes and deserializes them back, using web-style options unless a custom set is supplied.
/// </summary>
/// <param name="options">Optional serializer options; defaults to <see cref="JsonSerializerDefaults.Web"/>.</param>
public sealed class SystemTextJsonMessageSerializer(JsonSerializerOptions? options = null) : IMessageSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new(JsonSerializerDefaults.Web);

    private readonly JsonSerializerOptions _options = options ?? DefaultOptions;

    /// <summary>The MIME content type produced by this serializer (<c>application/json</c>).</summary>
    public string ContentType => "application/json";

    /// <summary>Serializes a value of the given declared type to UTF-8 JSON bytes.</summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="type">The declared type to serialize as.</param>
    /// <returns>The UTF-8 encoded JSON payload.</returns>
    public byte[] Serialize(object value, Type type)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(type);
        return JsonSerializer.SerializeToUtf8Bytes(value, type, _options);
    }

    /// <summary>Deserializes UTF-8 JSON bytes into an instance of the given type.</summary>
    /// <param name="data">The serialized payload.</param>
    /// <param name="type">The target CLR type.</param>
    /// <returns>The deserialized instance, or <see langword="null"/>.</returns>
    public object? Deserialize(ReadOnlySpan<byte> data, Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return JsonSerializer.Deserialize(data, type, _options);
    }
}
