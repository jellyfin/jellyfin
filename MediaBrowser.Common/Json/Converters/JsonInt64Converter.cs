using System;
using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Common.Json.Converters
{
    /// <summary>
    /// Parse JSON string as long.
    /// Javascript does not support 64-bit integers.
    /// </summary>
    public class JsonInt64Converter : JsonConverter<long>
    {
        /// <summary>
        /// Read JSON string as int64.
        /// </summary>
        /// <param name="reader"><see cref="Utf8JsonReader"/>.</param>
        /// <param name="type">Type.</param>
        /// <param name="options">Options.</param>
        /// <returns>Parsed value.</returns>
        public override long Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                // try to parse number directly from bytes
                var span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
                if (Utf8Parser.TryParse(span, out long number, out var bytesConsumed) && span.Length == bytesConsumed)
                {
                    return number;
                }

                // try to parse from a string if the above failed, this covers cases with other escaped/UTF characters
                if (long.TryParse(reader.GetString(), out number))
                {
                    return number;
                }
            }

            // fallback to default handling
            return reader.GetInt64();
        }

        /// <summary>
        /// Write long to JSON long.
        /// </summary>
        /// <param name="writer"><see cref="Utf8JsonWriter"/>.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="options">Options.</param>
        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}
