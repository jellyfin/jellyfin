using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Common.Json.Converters
{
    /// <summary>
    /// Json Omdb converter factory.
    /// </summary>
    /// <remarks>
    /// Remove when Omdb is moved to plugin.
    /// </remarks>
    public class JsonOmdbNotAvailableConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return (typeToConvert.IsGenericType
                    && typeToConvert.GetGenericTypeDefinition() == typeof(Nullable<>)
                    && typeToConvert.GenericTypeArguments[0].IsValueType)
                   || typeToConvert == typeof(string);
        }

        /// <inheritdoc />
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert == typeof(string))
            {
                return (JsonConverter)Activator.CreateInstance(typeof(JsonOmdbNotAvailableStringConverter));
            }

            var structType = typeToConvert.GenericTypeArguments[0];
            return (JsonConverter)Activator.CreateInstance(typeof(JsonOmdbNotAvailableStructConverter<>).MakeGenericType(structType));
        }
    }
}
