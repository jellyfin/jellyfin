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
            // null got handled higher up the call stack
            var val = value!.Value;
            if (val.IsEmpty())
            {
                writer.WriteNullValue();
            }
            else
            {
                JsonGuidConverter.WriteInternal(writer, val);
            }
        }
    }
}
