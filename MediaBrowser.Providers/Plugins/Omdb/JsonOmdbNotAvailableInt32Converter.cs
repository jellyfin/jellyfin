using System;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Providers.Plugins.Omdb
{
    /// <summary>
    /// Converts a string <c>N/A</c> to <c>string.Empty</c>.
    /// </summary>
    public class JsonOmdbNotAvailableInt32Converter : JsonConverter<int?>
    {
        /// <inheritdoc />
        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();
                if (str is null || str.Equals("N/A", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                var converter = TypeDescriptor.GetConverter(typeToConvert);
                return (int?)converter.ConvertFromString(str);
            }

            return JsonSerializer.Deserialize<int>(ref reader, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteNumberValue(value.Value);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
