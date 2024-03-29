using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Jellyfin.Extensions;
using LrcParser.Model;
using LrcParser.Parser;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Lyrics;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// LRC Lyric Parser.
/// </summary>
public class LrcLyricParser : ILyricParser
{
    private readonly LyricParser _lrcLyricParser;

    private static readonly string[] _supportedMediaTypes = [".lrc", ".elrc"];
    private static readonly string[] _acceptedTimeFormats = ["HH:mm:ss", "H:mm:ss", "mm:ss", "m:ss"];

    /// <summary>
    /// Initializes a new instance of the <see cref="LrcLyricParser"/> class.
    /// </summary>
    public LrcLyricParser()
    {
        _lrcLyricParser = new LrcParser.Parser.Lrc.LrcParser();
    }

    /// <inheritdoc />
    public string Name => "LrcLyricProvider";

    /// <summary>
    /// Gets the priority.
    /// </summary>
    /// <value>The priority.</value>
    public ResolverPriority Priority => ResolverPriority.Fourth;

    /// <inheritdoc />
    public LyricDto? ParseLyrics(LyricFile lyrics)
    {
        if (!_supportedMediaTypes.Contains(Path.GetExtension(lyrics.Name.AsSpan()), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        Song lyricData;

        try
        {
            lyricData = _lrcLyricParser.Decode(lyrics.Content);
        }
        catch (Exception)
        {
            // Failed to parse, return null so the next parser will be tried
            return null;
        }

        List<LrcParser.Model.Lyric> sortedLyricData = lyricData.Lyrics.Where(x => x.TimeTags.Count > 0).OrderBy(x => x.TimeTags.First().Value).ToList();

        // Parse metadata rows
        var metaDataRows = lyricData.Lyrics
            .Where(x => x.TimeTags.Count == 0)
            .Where(x => x.Text.StartsWith('[') && x.Text.EndsWith(']'))
            .Select(x => x.Text)
            .ToList();

        var fileMetaData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string metaDataRow in metaDataRows)
        {
            var index = metaDataRow.IndexOf(':', StringComparison.OrdinalIgnoreCase);
            if (index == -1)
            {
                continue;
            }

            // Remove square bracket before field name, and after field value
            // Example 1: [au: 1hitsong]
            // Example 2: [ar: Calabrese]
            var metaDataFieldName = GetMetadataFieldName(metaDataRow, index);
            var metaDataFieldValue = GetMetadataValue(metaDataRow, index);

            if (string.IsNullOrEmpty(metaDataFieldName) || string.IsNullOrEmpty(metaDataFieldValue))
            {
                continue;
            }

            fileMetaData[metaDataFieldName] = metaDataFieldValue;
        }

        if (sortedLyricData.Count == 0)
        {
            return null;
        }

        List<LyricLine> lyricList = [];

        for (int i = 0; i < sortedLyricData.Count; i++)
        {
            var timeData = sortedLyricData[i].TimeTags.First().Value;
            if (timeData is null)
            {
                continue;
            }

            long ticks = TimeSpan.FromMilliseconds(timeData.Value).Ticks;
            lyricList.Add(new LyricLine(sortedLyricData[i].Text.Trim(), ticks));
        }

        if (fileMetaData.Count != 0)
        {
            // Map metaData values from LRC file to LyricMetadata properties
            LyricMetadata lyricMetadata = MapMetadataValues(fileMetaData);

            return new LyricDto { Metadata = lyricMetadata, Lyrics = lyricList };
        }

        return new LyricDto { Lyrics = lyricList };
    }

    /// <summary>
    /// Converts metadata from an LRC file to LyricMetadata properties.
    /// </summary>
    /// <param name="metaData">The metadata from the LRC file.</param>
    /// <returns>A lyricMetadata object with mapped property data.</returns>
    private static LyricMetadata MapMetadataValues(Dictionary<string, string> metaData)
    {
        LyricMetadata lyricMetadata = new();

        if (metaData.TryGetValue("ar", out var artist) && !string.IsNullOrEmpty(artist))
        {
            lyricMetadata.Artist = artist;
        }

        if (metaData.TryGetValue("al", out var album) && !string.IsNullOrEmpty(album))
        {
            lyricMetadata.Album = album;
        }

        if (metaData.TryGetValue("ti", out var title) && !string.IsNullOrEmpty(title))
        {
            lyricMetadata.Title = title;
        }

        if (metaData.TryGetValue("au", out var author) && !string.IsNullOrEmpty(author))
        {
            lyricMetadata.Author = author;
        }

        if (metaData.TryGetValue("length", out var length) && !string.IsNullOrEmpty(length))
        {
            if (DateTime.TryParseExact(length, _acceptedTimeFormats, null, DateTimeStyles.None, out var value))
            {
                lyricMetadata.Length = value.TimeOfDay.Ticks;
            }
        }

        if (metaData.TryGetValue("by", out var by) && !string.IsNullOrEmpty(by))
        {
            lyricMetadata.By = by;
        }

        if (metaData.TryGetValue("offset", out var offset) && !string.IsNullOrEmpty(offset))
        {
            if (int.TryParse(offset, out var value))
            {
                lyricMetadata.Offset = TimeSpan.FromMilliseconds(value).Ticks;
            }
        }

        if (metaData.TryGetValue("re", out var creator) && !string.IsNullOrEmpty(creator))
        {
            lyricMetadata.Creator = creator;
        }

        if (metaData.TryGetValue("ve", out var version) && !string.IsNullOrEmpty(version))
        {
            lyricMetadata.Version = version;
        }

        return lyricMetadata;
    }

    private static string GetMetadataFieldName(string metaDataRow, int index)
    {
        var metadataFieldName = metaDataRow.AsSpan(1, index - 1).Trim();
        return metadataFieldName.IsEmpty ? string.Empty : metadataFieldName.ToString();
    }

    private static string GetMetadataValue(string metaDataRow, int index)
    {
        var metadataValue = metaDataRow.AsSpan(index + 1, metaDataRow.Length - index - 2).Trim();
        return metadataValue.IsEmpty ? string.Empty : metadataValue.ToString();
    }
}
