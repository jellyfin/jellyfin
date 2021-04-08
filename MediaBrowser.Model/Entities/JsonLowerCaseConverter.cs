#nullable disable
// THIS IS A HACK
// TODO: @bond Move to separate project

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Converts an object to a lowercase string.
    /// </summary>
    /// <typeparam name="T">The object type.</typeparam>
    public class JsonLowerCaseConverter<T> : JsonConverter<T>
    {
        /// <inheritdoc />
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<T>(ref reader, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value?.ToString().ToLowerInvariant());
        }
    }
}
