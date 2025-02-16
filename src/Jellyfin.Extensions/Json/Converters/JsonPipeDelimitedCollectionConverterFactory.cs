using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Extensions.Json.Converters
{
    /// <summary>
    /// Json Pipe delimited collection converter factory.
    /// </summary>
    /// <remarks>
    /// This must be applied as an attribute, adding to the JsonConverter list causes stack overflow.
    /// </remarks>
    public class JsonPipeDelimitedCollectionConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsArray
                || (typeToConvert.IsGenericType
                    && (typeToConvert.GetGenericTypeDefinition().IsAssignableFrom(typeof(IReadOnlyCollection<>)) || typeToConvert.GetGenericTypeDefinition().IsAssignableFrom(typeof(IReadOnlyList<>))));
        }

        /// <inheritdoc />
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var structType = typeToConvert.GetElementType() ?? typeToConvert.GenericTypeArguments[0];
            return (JsonConverter?)Activator.CreateInstance(typeof(JsonPipeDelimitedCollectionConverter<>).MakeGenericType(structType));
        }
    }
}
