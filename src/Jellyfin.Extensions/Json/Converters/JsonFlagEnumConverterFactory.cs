using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Extensions.Json.Converters;

/// <summary>
/// Json flag enum converter factory.
/// </summary>
public class JsonFlagEnumConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum && typeToConvert.IsDefined(typeof(FlagsAttribute));
    }

    /// <inheritdoc />
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return (JsonConverter?)Activator.CreateInstance(typeof(JsonFlagEnumConverter<>).MakeGenericType(typeToConvert));
    }
}
