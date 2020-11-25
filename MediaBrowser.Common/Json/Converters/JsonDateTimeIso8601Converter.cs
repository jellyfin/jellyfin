using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Common.Json.Converters
{
    /// <summary>
    /// Returns an ISO8601 formatted datetime.
    /// </summary>
    /// <remarks>
    /// Used for legacy compatibility.
    /// </remarks>
    public class JsonDateTimeIso8601Converter : JsonConverter<DateTime>
    {
        /// <inheritdoc />
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetDateTime();

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            // Must specify DateTime.Kind so the DateTime is properly formatted.
            // Expected output 2000-01-01T00:00:00.0000000Z
            => writer.WriteStringValue(
                DateTime.SpecifyKind(value, DateTimeKind.Utc)
                    .ToString("O", CultureInfo.InvariantCulture));
    }
}
