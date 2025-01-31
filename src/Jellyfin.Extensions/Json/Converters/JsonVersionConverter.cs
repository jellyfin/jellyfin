using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Extensions.Json.Converters
{
    /// <summary>
    /// Converts a Version object or value to/from JSON.
    /// </summary>
    /// <remarks>
    /// Required to send <see cref="Version"/> as a string instead of an object.
    /// </remarks>
    public class JsonVersionConverter : JsonConverter<Version>
    {
        /// <inheritdoc />
        public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new Version(reader.GetString()!); // Will throw ArgumentNullException on null

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString());
    }
}
