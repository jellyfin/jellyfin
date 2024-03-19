using System;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Extensions.Json.Converters;

/// <summary>
/// Json unknown enum converter.
/// </summary>
/// <typeparam name="T">The type of enum.</typeparam>
public class JsonDefaultStringEnumConverter<T> : JsonConverter<T>
    where T : struct, Enum
{
    private readonly JsonConverter<T> _baseConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDefaultStringEnumConverter{T}"/> class.
    /// </summary>
    /// <param name="baseConverter">The base json converter.</param>
    public JsonDefaultStringEnumConverter(JsonConverter<T> baseConverter)
    {
        _baseConverter = baseConverter;
    }

    /// <inheritdoc />
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.IsNull() || reader.IsEmptyString())
        {
            var customValueAttribute = typeToConvert.GetCustomAttribute<DefaultValueAttribute>();
            if (customValueAttribute?.Value is null)
            {
                throw new InvalidOperationException($"Default value not set for '{typeToConvert.Name}'");
            }

            return (T)customValueAttribute.Value;
        }

        return _baseConverter.Read(ref reader, typeToConvert, options);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        _baseConverter.Write(writer, value, options);
    }
}
