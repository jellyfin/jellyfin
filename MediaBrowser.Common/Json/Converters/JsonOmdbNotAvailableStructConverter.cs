using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Common.Json.Converters
{
    /// <summary>
    /// Converts a string <c>N/A</c> to <c>string.Empty</c>.
    /// </summary>
    /// <typeparam name="T">The resulting type.</typeparam>
    public class JsonOmdbNotAvailableStructConverter<T> : JsonConverter<T?>
        where T : struct
    {
        /// <inheritdoc />
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();
                if (str != null && str.Equals("N/A", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
            }

            return JsonSerializer.Deserialize<T>(ref reader, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(value, options);
        }
    }
}
