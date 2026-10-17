using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Nikse.SubtitleEdit.Core.Common;
using Nikse.SubtitleEdit.Core.SubtitleFormats;

namespace MediaBrowser.MediaEncoding.Subtitles;

/// <summary>
/// JSON subtitle writer.
/// </summary>
public class JsonWriter : SubtitleFormat
{
    /// <inheritdoc />
    public override string Extension => ".json";

    /// <inheritdoc />
    public override string Name => "JSON Jellyfin";

    /// <inheritdoc />
    public override string ToText(Subtitle subtitle, string title)
    {
        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms))
        {
            var trackevents = subtitle.Paragraphs;
            writer.WriteStartObject();
            writer.WriteStartArray("TrackEvents");

            for (int i = 0; i < trackevents.Count; i++)
            {
                var current = trackevents[i];
                writer.WriteStartObject();

                writer.WriteString("Id", current.Number.ToString(CultureInfo.InvariantCulture));
                writer.WriteString("Text", current.Text);
                writer.WriteNumber("StartPositionTicks", current.StartTime.TimeSpan.Ticks);
                writer.WriteNumber("EndPositionTicks", current.EndTime.TimeSpan.Ticks);

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();

            writer.Flush();
        }

        return Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
    }

    /// <inheritdoc />
    public override void LoadSubtitle(Subtitle subtitle, List<string> lines, string fileName)
        => throw new NotImplementedException();
}
