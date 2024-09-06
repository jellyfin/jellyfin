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
            return reader.GetDateTime();
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
