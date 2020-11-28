using System;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Common.Json.Converters
{
    /// <summary>
    /// Json comma delimited array converter factory.
    /// </summary>
    /// <remarks>
    /// This must be applied as an attribute, adding to the JsonConverter list causes stack overflow.
    /// </remarks>
    public class JsonCommaDelimitedArrayConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return true;
        }

        /// <inheritdoc />
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var structType = typeToConvert.GetElementType() ?? typeToConvert.GenericTypeArguments[0];
            var converter = (JsonConverter?)Activator.CreateInstance(typeof(JsonCommaDelimitedArrayConverter<>).MakeGenericType(structType));

            // Should not happen
            if (converter == null)
            {
                throw new NoNullAllowedException("Activator.CreateInstance failed to create instance of JsonCommaDelimitedArrayConverter!");
            }

            return converter;
        }
    }
}
