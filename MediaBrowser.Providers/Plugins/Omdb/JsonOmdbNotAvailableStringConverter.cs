using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Extensions.Json;

namespace MediaBrowser.Providers.Plugins.Omdb
{
    /// <summary>
    /// Converts a string <c>N/A</c> to <c>string.Empty</c> and decodes HTML entities in every other string.
    /// </summary>
    public class JsonOmdbNotAvailableStringConverter : JsonConverter<string?>
    {
        /// <inheritdoc />
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.IsNull())
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                // GetString can't return null here because we already handled it above
                var str = reader.GetString()!;
                if (str.Equals("N/A", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                // Some OMDb records are HTML encoded, e.g. the cast of tt0093058 lists "Vincent D&apos;Onofrio".
                // Stored verbatim that name is a second person next to the correctly spelled one.
                return WebUtility.HtmlDecode(str);
            }

            return JsonSerializer.Deserialize<string?>(ref reader, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
