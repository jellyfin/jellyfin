using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Common.Json.Converters
{
    /// <summary>
    /// Converts a nullable struct or value to/from JSON.
    /// Required - some clients send an empty string.
    /// </summary>
    /// <typeparam name="TStruct">The struct type.</typeparam>
    public class JsonNullableStructConverter<TStruct> : JsonConverter<TStruct?>
        where TStruct : struct
    {
        /// <inheritdoc />
        public override TStruct? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            // Token is empty string.
            if (reader.TokenType == JsonTokenType.String && ((reader.HasValueSequence && reader.ValueSequence.IsEmpty) || reader.ValueSpan.IsEmpty))
            {
                return null;
            }

            return JsonSerializer.Deserialize<TStruct>(ref reader, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, TStruct? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                JsonSerializer.Serialize(writer, value.Value, options);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
