using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Common.Json.Converters
{
    /// <summary>
    /// Converts a number to a boolean.
    /// This is needed for HDHomerun.
    /// </summary>
    /// <remarks>
    /// Adding this to the JsonConverter list causes recursion.
    /// </remarks>
    public class JsonBoolNumberConverter : JsonConverter<bool>
    {
        /// <inheritdoc />
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return Convert.ToBoolean(reader.GetInt32());
            }

            return JsonSerializer.Deserialize<bool>(ref reader, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}