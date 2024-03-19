using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Extensions.Json.Converters
{
    /// <summary>
    /// Converts a GUID object or value to/from JSON.
    /// </summary>
    public class JsonGuidConverter : JsonConverter<Guid>
    {
        /// <inheritdoc />
        public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.IsNull()
                ? Guid.Empty
                : ReadInternal(ref reader);

        // TODO: optimize by parsing the UTF8 bytes instead of converting to string first
        internal static Guid ReadInternal(ref Utf8JsonReader reader)
            => Guid.Parse(reader.GetString()!); // null got handled higher up the call stack

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
            => WriteInternal(writer, value);

        internal static void WriteInternal(Utf8JsonWriter writer, Guid value)
            => writer.WriteStringValue(value.ToString("N", CultureInfo.InvariantCulture));
    }
}
