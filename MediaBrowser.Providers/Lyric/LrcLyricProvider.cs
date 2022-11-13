using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LrcParser.Model;
using LrcParser.Parser;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Resolvers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// LRC Lyric Provider.
/// </summary>
public class LrcLyricProvider : ILyricProvider
{
    private readonly ILogger<LrcLyricProvider> _logger;

    private readonly LyricParser _lrcLyricParser;

    private static readonly string[] _acceptedTimeFormats = { "HH:mm:ss", "H:mm:ss", "mm:ss", "m:ss" };

    /// <summary>
    /// Initializes a new instance of the <see cref="LrcLyricProvider"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    public LrcLyricProvider(ILogger<LrcLyricProvider> logger)
    {
        _logger = logger;
        _lrcLyricParser = new LrcParser.Parser.Lrc.LrcParser();
    }

    /// <inheritdoc />
    public string Name => "LrcLyricProvider";

    /// <summary>
    /// Gets the priority.
    /// </summary>
    /// <value>The priority.</value>
    public ResolverPriority Priority => ResolverPriority.First;

    /// <inheritdoc />
    public IReadOnlyCollection<string> SupportedMediaTypes { get; } = new[] { "lrc", "elrc" };

    /// <summary>
    /// Opens lyric file for the requested item, and processes it for API return.
    /// </summary>
    /// <param name="item">The item to to process.</param>
    /// <returns>If provider can determine lyrics, returns a <see cref="LyricResponse"/> with or without metadata; otherwise, null.</returns>
    public async Task<LyricResponse?> GetLyrics(BaseItem item)
    {
        string? lyricFilePath = this.GetLyricFilePath(item.Path);

        if (string.IsNullOrEmpty(lyricFilePath))
        {
            return null;
        }

        var fileMetaData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string lrcFileContent = await File.ReadAllTextAsync(lyricFilePath).ConfigureAwait(false);

        Song lyricData;

        try
        {
            lyricData = _lrcLyricParser.Decode(lrcFileContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing lyric file {LyricFilePath} from {Provider}", lyricFilePath, Name);
            return null;
        }

        List<LrcParser.Model.Lyric> sortedLyricData = lyricData.Lyrics.Where(x => x.TimeTags.Count > 0).OrderBy(x => x.TimeTags.First().Value).ToList();

        // Parse metadata rows
        var metaDataRows = lyricData.Lyrics
            .Where(x => x.TimeTags.Count == 0)
            .Where(x => x.Text.StartsWith('[') && x.Text.EndsWith(']'))
            .Select(x => x.Text)
            .ToList();

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

        List<LyricLine> lyricList = new();

        for (int i = 0; i < sortedLyricData.Count; i++)
        {
            var timeData = sortedLyricData[i].TimeTags.First().Value;
            if (timeData is null)
            {
                continue;
            }

            long ticks = TimeSpan.FromMilliseconds(timeData.Value).Ticks;
            lyricList.Add(new LyricLine(sortedLyricData[i].Text, ticks));
        }

        if (fileMetaData.Count != 0)
        {
            // Map metaData values from LRC file to LyricMetadata properties
            LyricMetadata lyricMetadata = MapMetadataValues(fileMetaData);

            return new LyricResponse
            {
                Metadata = lyricMetadata,
                Lyrics = lyricList
            };
        }

        return new LyricResponse
        {
            Lyrics = lyricList
        };
    }

    /// <summary>
    /// Converts metadata from an LRC file to LyricMetadata properties.
    /// </summary>
    /// <param name="metaData">The metadata from the LRC file.</param>
    /// <returns>A lyricMetadata object with mapped property data.</returns>
    private static LyricMetadata MapMetadataValues(IDictionary<string, string> metaData)
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
