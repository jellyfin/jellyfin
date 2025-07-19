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
    public abstract class JsonDelimitedCollectionConverter<T> : JsonConverter<IReadOnlyCollection<T>>
    {
        private readonly TypeConverter _typeConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDelimitedCollectionConverter{T}"/> class.
        /// </summary>
        protected JsonDelimitedCollectionConverter()
        {
            _typeConverter = TypeDescriptor.GetConverter(typeof(T));
        }

        /// <summary>
        /// Gets the array delimiter.
        /// </summary>
        protected virtual char Delimiter { get; }

        /// <inheritdoc />
        public override IReadOnlyCollection<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
                        // Ignore unconvertible inputs
                    }
                }

                if (typeToConvert.IsArray)
                {
                    return typedValues.ToArray();
                }

                return typedValues;
            }

            return JsonSerializer.Deserialize<T[]>(ref reader, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, IReadOnlyCollection<T>? value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
