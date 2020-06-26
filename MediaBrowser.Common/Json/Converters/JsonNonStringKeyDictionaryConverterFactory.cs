#nullable enable

using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Common.Json.Converters
{
    /// <summary>
    /// https://github.com/dotnet/runtime/issues/30524#issuecomment-524619972.
    /// TODO This can be removed when System.Text.Json supports Dictionaries with non-string keys.
    /// </summary>
    internal sealed class JsonNonStringKeyDictionaryConverterFactory : JsonConverterFactory
    {
        /// <summary>
        /// Only convert objects that implement IDictionary and do not have string keys.
        /// </summary>
        /// <param name="typeToConvert">Type convert.</param>
        /// <returns>Conversion ability.</returns>
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType)
            {
                return false;
            }

            // Let built in converter handle string keys
            if (typeToConvert.GenericTypeArguments[0] == typeof(string))
            {
                return false;
            }

            // Only support objects that implement IDictionary
            return typeToConvert.GetInterface(nameof(IDictionary)) != null;
        }

        /// <summary>
        /// Create converter for generic dictionary type.
        /// </summary>
        /// <param name="typeToConvert">Type to convert.</param>
        /// <param name="options">Json serializer options.</param>
        /// <returns>JsonConverter for given type.</returns>
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(JsonNonStringKeyDictionaryConverter<,>)
                .MakeGenericType(typeToConvert.GenericTypeArguments[0], typeToConvert.GenericTypeArguments[1]);
            var converter = (JsonConverter)Activator.CreateInstance(
                converterType,
                BindingFlags.Instance | BindingFlags.Public,
                null,
                null,
                CultureInfo.CurrentCulture);
            return converter;
        }
    }
}
