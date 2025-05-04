using System.Text.Json;

namespace Jellyfin.Extensions.Json;

/// <summary>
/// Extensions for Utf8JsonReader and Utf8JsonWriter.
/// </summary>
public static class Utf8JsonExtensions
{
    /// <summary>
    /// Determines if the reader contains an empty string.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <returns>Whether the reader contains an empty string.</returns>
    public static bool IsEmptyString(this Utf8JsonReader reader)
        => reader.TokenType == JsonTokenType.String
           && ((reader.HasValueSequence && reader.ValueSequence.IsEmpty)
               || (!reader.HasValueSequence && reader.ValueSpan.IsEmpty));

    /// <summary>
    /// Determines if the reader contains a null value.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <returns>Whether the reader contains null.</returns>
    public static bool IsNull(this Utf8JsonReader reader)
        => reader.TokenType == JsonTokenType.Null;
}
