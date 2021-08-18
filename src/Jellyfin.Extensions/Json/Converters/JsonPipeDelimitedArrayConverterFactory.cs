using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Extensions.Json.Converters
{
    /// <summary>
    /// Json Pipe delimited array converter factory.
    /// </summary>
    /// <remarks>
    /// This must be applied as an attribute, adding to the JsonConverter list causes stack overflow.
    /// </remarks>
    public class JsonPipeDelimitedArrayConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return true;
        }

        /// <inheritdoc />
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var structType = typeToConvert.GetElementType() ?? typeToConvert.GenericTypeArguments[0];
            return (JsonConverter?)Activator.CreateInstance(typeof(JsonPipeDelimitedArrayConverter<>).MakeGenericType(structType));
        }
    }
}
