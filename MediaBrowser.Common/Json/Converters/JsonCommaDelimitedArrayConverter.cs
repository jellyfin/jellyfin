using System;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Common.Json.Converters
{
    /// <summary>
    /// Convert comma delimited string to array of type.
    /// </summary>
    /// <typeparam name="T">Type to convert to.</typeparam>
    public class JsonCommaDelimitedArrayConverter<T> : JsonConverter<T[]>
    {
        private readonly TypeConverter _typeConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonCommaDelimitedArrayConverter{T}"/> class.
        /// </summary>
        public JsonCommaDelimitedArrayConverter()
        {
            _typeConverter = TypeDescriptor.GetConverter(typeof(T));
        }

        /// <inheritdoc />
        public override T[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringEntries = reader.GetString()?.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (stringEntries == null || stringEntries.Length == 0)
                {
                    return Array.Empty<T>();
                }

                var parsedValues = new object[stringEntries.Length];
                var convertedCount = 0;
                for (var i = 0; i < stringEntries.Length; i++)
                {
                    try
                    {
                        parsedValues[i] = _typeConverter.ConvertFrom(stringEntries[i].Trim());
                        convertedCount++;
                    }
                    catch (FormatException)
                    {
                        // TODO log when upgraded to .Net6
                        // https://github.com/dotnet/runtime/issues/42975
                        // _logger.LogDebug(e, "Error converting value.");
                    }
                }

                var typedValues = new T[convertedCount];
                var typedValueIndex = 0;
                for (var i = 0; i < stringEntries.Length; i++)
                {
                    if (parsedValues[i] != null)
                    {
                        typedValues.SetValue(parsedValues[i], typedValueIndex);
                        typedValueIndex++;
                    }
                }

                return typedValues;
            }

            return JsonSerializer.Deserialize<T[]>(ref reader, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, T[] value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
