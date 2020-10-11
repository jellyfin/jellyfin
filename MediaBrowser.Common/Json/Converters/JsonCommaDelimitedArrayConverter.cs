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
                var stringEntries = reader.GetString()?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (stringEntries == null || stringEntries.Length == 0)
                {
                    return Array.Empty<T>();
                }

                var entries = new T[stringEntries.Length];
                for (var i = 0; i < stringEntries.Length; i++)
                {
                    entries[i] = (T)_typeConverter.ConvertFrom(stringEntries[i]);
                }

                return entries;
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