using System;
using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Common.Json.Converters
{
    /// <summary>
    /// Double to String JSON converter.
    /// Web client send quoted doubles.
    /// </summary>
    public class JsonDoubleConverter : JsonConverter<double>
    {
        /// <summary>
        /// Read JSON string as double.
        /// </summary>
        /// <param name="reader"><see cref="Utf8JsonReader"/>.</param>
        /// <param name="typeToConvert">Type.</param>
        /// <param name="options">Options.</param>
        /// <returns>Parsed value.</returns>
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                // try to parse number directly from bytes
                var span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
                if (Utf8Parser.TryParse(span, out double number, out var bytesConsumed) && span.Length == bytesConsumed)
                {
                    return number;
                }

                // try to parse from a string if the above failed, this covers cases with other escaped/UTF characters
                if (double.TryParse(reader.GetString(), out number))
                {
                    return number;
                }
            }

            // fallback to default handling
            return reader.GetDouble();
        }

        /// <summary>
        /// Write double to JSON string.
        /// </summary>
        /// <param name="writer"><see cref="Utf8JsonWriter"/>.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="options">Options.</param>
        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
}
