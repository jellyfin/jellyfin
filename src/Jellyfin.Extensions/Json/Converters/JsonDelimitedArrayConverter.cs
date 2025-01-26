using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Extensions.Json.Converters
{
    /// <summary>
    /// Convert delimited string to array of type.
    /// </summary>
    /// <typeparam name="T">Type to convert to.</typeparam>
    public abstract class JsonDelimitedArrayConverter<T> : JsonConverter<T[]>
    {
        private readonly TypeConverter _typeConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDelimitedArrayConverter{T}"/> class.
        /// </summary>
        protected JsonDelimitedArrayConverter()
        {
            _typeConverter = TypeDescriptor.GetConverter(typeof(T));
        }

        /// <summary>
        /// Gets the array delimiter.
        /// </summary>
        protected virtual char Delimiter { get; }

        /// <inheritdoc />
        public override T[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                // null got handled higher up the call stack
                var stringEntries = reader.GetString()!.Split(Delimiter, StringSplitOptions.RemoveEmptyEntries);
                if (stringEntries.Length == 0)
                {
                    return [];
                }

                var typedValues = new List<T>();
                for (var i = 0; i < stringEntries.Length; i++)
                {
                    try
                    {
                        var parsedValue = _typeConverter.ConvertFromInvariantString(stringEntries[i].Trim());
                        if (parsedValue is not null)
                        {
                            typedValues.Add((T)parsedValue);
                        }
                    }
                    catch (FormatException)
                    {
                        // Ignore unconvertable inputs
                    }
                }

                return typedValues.ToArray();
            }

            return JsonSerializer.Deserialize<T[]>(ref reader, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, T[]? value, JsonSerializerOptions options)
        {
            if (value is not null)
            {
                writer.WriteStartArray();
                if (value.Length > 0)
                {
                    foreach (var it in value)
                    {
                        if (it is not null)
                        {
                            writer.WriteStringValue(it.ToString());
                        }
                    }
                }

                writer.WriteEndArray();
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
