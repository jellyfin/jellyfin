using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Common.Json.Converters
{
    /// <summary>
    /// Converts a nullable int64 object or value to/from JSON.
    /// Required - some clients send an empty string.
    /// </summary>
    public class JsonNullableInt64Converter : JsonConverter<long?>
    {
        private readonly JsonConverter<long?> _baseJsonConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNullableInt64Converter"/> class.
        /// </summary>
        /// <param name="baseJsonConverter">The base json converter.</param>
        public JsonNullableInt64Converter(JsonConverter<long?> baseJsonConverter)
        {
            _baseJsonConverter = baseJsonConverter;
        }

        /// <inheritdoc />
        public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String && ((reader.HasValueSequence && reader.ValueSequence.IsEmpty) || reader.ValueSpan.IsEmpty))
            {
                return null;
            }

            return _baseJsonConverter.Read(ref reader, typeToConvert, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
        {
            _baseJsonConverter.Write(writer, value, options);
        }
    }
}
