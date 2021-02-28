using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Common.Json.Converters
{
    /// <summary>
    /// Converts a string <c>N/A</c> to <c>string.Empty</c>.
    /// </summary>
    public class JsonOmdbNotAvailableStringConverter : JsonConverter<string>
    {
        /// <inheritdoc />
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();
                if (str != null && str.Equals("N/A", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return str;
            }

            return JsonSerializer.Deserialize<string>(ref reader, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
