using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Common.Json.Converters
{
    /// <summary>
    /// Parse JSON string as nullable long.
    /// Javascript does not support 64-bit integers.
    /// Required - some clients send an empty string.
    /// </summary>
    public class JsonNullableInt64Converter : JsonConverter<long?>
    {
        /// <summary>
        /// Read JSON string as int64.
        /// </summary>
        /// <param name="reader"><see cref="Utf8JsonReader"/>.</param>
        /// <param name="type">Type.</param>
        /// <param name="options">Options.</param>
        /// <returns>Parsed value.</returns>
        public override long? Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String when (reader.HasValueSequence && reader.ValueSequence.IsEmpty) || reader.ValueSpan.IsEmpty:
                case JsonTokenType.Null:
                    return null;
                default:
                    // fallback to default handling
                    return reader.GetInt64();
            }
        }

        /// <summary>
        /// Write long to JSON long.
        /// </summary>
        /// <param name="writer"><see cref="Utf8JsonWriter"/>.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="options">Options.</param>
        public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteNumberValue(value.Value);
            }
        }
    }
}
