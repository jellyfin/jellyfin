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
        private readonly JsonConverter<int?> _baseJsonConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNullableInt32Converter"/> class.
        /// </summary>
        /// <param name="baseJsonConverter">The base json converter.</param>
        public JsonNullableInt32Converter(JsonConverter<int?> baseJsonConverter)
        {
            _baseJsonConverter = baseJsonConverter;
        }

        /// <inheritdoc />
        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String && ((reader.HasValueSequence && reader.ValueSequence.IsEmpty) || reader.ValueSpan.IsEmpty))
            {
                return null;
            }

            return _baseJsonConverter.Read(ref reader, typeToConvert, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
        {
            _baseJsonConverter.Write(writer, value, options);
        }
    }
}
