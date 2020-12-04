using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Common.Json.Converters
{
    /// <summary>
    /// Converts a Version object or value to/from JSON.
    /// </summary>
    public class JsonVersionConverter : JsonConverter<Version>
    {
        /// <inheritdoc />
        public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new Version(reader.GetString());

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString());
    }
}
