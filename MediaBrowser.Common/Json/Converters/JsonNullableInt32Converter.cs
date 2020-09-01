using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Common.Json.Converters
{
    /// <summary>
    /// Converts a nullable int32 object or value to/from JSON.
    /// Required - some clients send an empty string.
    /// </summary>
    public class JsonNullableInt32Converter : JsonConverter<int?>
    {
        /// <inheritdoc />
        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String when (reader.HasValueSequence && reader.ValueSequence.IsEmpty) || reader.ValueSpan.IsEmpty:
                case JsonTokenType.Null:
                    return null;
                default:
                    // fallback to default handling
                    return reader.GetInt32();
            }
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
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
