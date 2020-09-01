using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Common.Json.Converters
{
    /// <summary>
    /// Converts a nullable struct or value to/from JSON.
    /// Required - some clients send an empty string.
    /// </summary>
    /// <typeparam name="T">The struct type.</typeparam>
    public class JsonNullableStructConverter<T> : JsonConverter<T?>
        where T : struct
    {
        private readonly JsonConverter<T?> _baseJsonConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNullableStructConverter{T}"/> class.
        /// </summary>
        /// <param name="baseJsonConverter">The base json converter.</param>
        public JsonNullableStructConverter(JsonConverter<T?> baseJsonConverter)
        {
            _baseJsonConverter = baseJsonConverter;
        }

        /// <inheritdoc />
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Handle empty string.
            if (reader.TokenType == JsonTokenType.String && ((reader.HasValueSequence && reader.ValueSequence.IsEmpty) || reader.ValueSpan.IsEmpty))
            {
                return null;
            }

            return _baseJsonConverter.Read(ref reader, typeToConvert, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            _baseJsonConverter.Write(writer, value, options);
        }
    }
}
