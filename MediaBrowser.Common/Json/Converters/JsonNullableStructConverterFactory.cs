using System;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Common.Json.Converters
{
    /// <summary>
    /// Json nullable struct converter factory.
    /// </summary>
    public class JsonNullableStructConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType
                   && typeToConvert.GetGenericTypeDefinition() == typeof(Nullable<>)
                   && typeToConvert.GenericTypeArguments[0].IsValueType;
        }

        /// <inheritdoc />
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var structType = typeToConvert.GenericTypeArguments[0];
            var converter = (JsonConverter?)Activator.CreateInstance(typeof(JsonNullableStructConverter<>).MakeGenericType(structType));
            // Should not happen
            if (converter == null)
            {
                throw new NoNullAllowedException("Activator.CreateInstance failed to create instance of JsonCommaDelimitedArrayConverter!");
            }

            return converter;
        }
    }
}
