using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Extensions.Json.Converters
{
    /// <summary>
    /// Converts a GUID object or value to/from JSON.
    /// </summary>
    public class JsonNullableGuidConverter : JsonConverter<Guid?>
    {
        /// <inheritdoc />
        public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => JsonGuidConverter.ReadInternal(ref reader);

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
        {
            if (value == Guid.Empty)
            {
                writer.WriteNullValue();
            }
            else
            {
                // null got handled higher up the call stack
                JsonGuidConverter.WriteInternal(writer, value!.Value);
            }
        }
    }
}
