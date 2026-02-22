using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos;

/// <summary>
/// Converter for the <c>data</c> field in SD image responses.
/// The Schedules Direct API may return a non-array value (e.g. a string error message)
/// instead of the expected image data array for programs with errors.
/// This converter returns an empty list for any non-array value.
/// </summary>
public sealed class ImageDataArrayConverter : JsonConverter<IReadOnlyList<ImageDataDto>>
{
    /// <inheritdoc />
    public override IReadOnlyList<ImageDataDto> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var result = new List<ImageDataDto>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                var item = JsonSerializer.Deserialize<ImageDataDto>(ref reader, options);
                if (item is not null)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        // Not an array (string error, null, object, etc.) — skip and return empty.
        reader.TrySkip();
        return [];
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IReadOnlyList<ImageDataDto> value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, options);
}
