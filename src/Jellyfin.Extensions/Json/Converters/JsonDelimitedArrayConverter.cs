using System;
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
                    return Array.Empty<T>();
                }

                var parsedValues = new object[stringEntries.Length];
                var convertedCount = 0;
                for (var i = 0; i < stringEntries.Length; i++)
                {
                    try
                    {
                        parsedValues[i] = _typeConverter.ConvertFromInvariantString(stringEntries[i].Trim()) ?? throw new FormatException();
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
        public override void Write(Utf8JsonWriter writer, T[]? value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
