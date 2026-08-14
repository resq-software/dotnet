using System.Text;

namespace ResQ.BuildingBlocks.Adapters.Web;

/// <summary>
/// Encodes and decodes opaque keyset cursors as base64url (no padding).
/// EXPERIMENTAL/optional — keyset pagination has no first-cut consumer; the reference sample uses
/// offset pagination only.
/// </summary>
public static class CursorCodec
{
    /// <summary>Encodes a raw keyset key as a padding-free base64url cursor.</summary>
    /// <param name="rawKey">The raw key to encode.</param>
    /// <returns>The base64url-encoded cursor.</returns>
    public static string Encode(string rawKey)
    {
        ArgumentNullException.ThrowIfNull(rawKey);

        var bytes = Encoding.UTF8.GetBytes(rawKey);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>Decodes a base64url cursor back to its raw key.</summary>
    /// <param name="cursor">The cursor to decode, or <see langword="null"/>/empty.</param>
    /// <returns>The raw key, or <see langword="null"/> when the cursor is absent or malformed.</returns>
    public static string? Decode(string? cursor)
    {
        if (string.IsNullOrEmpty(cursor))
        {
            return null;
        }

        var normalized = cursor.Replace('-', '+').Replace('_', '/');
        normalized += (normalized.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => string.Empty,
        };

        try
        {
            var bytes = Convert.FromBase64String(normalized);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
