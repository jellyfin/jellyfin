using System;
using System.Buffers;
using System.Buffers.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Extensions.Json.Converters;

/// <summary>
/// Converts a string to a boolean.
/// This is needed for FFprobe.
/// </summary>
public class JsonBoolStringConverter : JsonConverter<bool>
{
    /// <inheritdoc />
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            ReadOnlySpan<byte> utf8Span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
            if (Utf8Parser.TryParse(utf8Span, out bool val, out _, 'l'))
            {
                return val;
            }
        }

        return reader.GetBoolean();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        => writer.WriteBooleanValue(value);
}
