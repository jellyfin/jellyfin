using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Extensions.Json.Converters
{
    /// <summary>
    /// Legacy DateTime converter.
    /// Milliseconds aren't output if zero by default.
    /// </summary>
    public class JsonDateTimeConverter : JsonConverter<DateTime>
    {
        /// <inheritdoc />
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TryGetDateTime(out var result) )
            {
                return result;
            }
            else
            {
                // it failed, let's look at the string value, and if it looks like a zero date, return DateTime.MinValue
                var text = reader.GetString();
                if (text != null && System.Text.RegularExpressions.Regex.IsMatch(text, @"^0000-\d{2}-\d{2}"))
                {
                    return DateTime.MinValue;
                }

                // We tried. Let's just throw the original exception for backwards compatiblity.
                return reader.GetDateTime();
            }
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            if (value.Millisecond == 0)
            {
                // Remaining ticks value will be 0, manually format.
                writer.WriteStringValue(value.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffZ", CultureInfo.InvariantCulture));
            }
            else
            {
                writer.WriteStringValue(value);
            }
        }
    }
}
