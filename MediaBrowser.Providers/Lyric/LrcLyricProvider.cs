using System;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="LrcLyricProvider"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    public LrcLyricProvider(ILogger<LrcLyricProvider> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "LrcLyricProvider";

    /// <summary>
    /// Gets the priority.
    /// </summary>
    /// <value>The priority.</value>
    public ResolverPriority Priority => ResolverPriority.First;

    /// <inheritdoc />
    public IEnumerable<string> SupportedMediaTypes { get; } = new[] { "lrc" };

    /// <summary>
    /// Opens lyric file for the requested item, and processes it for API return.
    /// </summary>
    /// <param name="item">The item to to process.</param>
    /// <returns>If provider can determine lyrics, returns a <see cref="LyricResponse"/> with or without metadata; otherwise, null.</returns>
    public LyricResponse? GetLyrics(BaseItem item)
    {
        string? lyricFilePath = LyricInfo.GetLyricFilePath(this, item.Path);

        if (string.IsNullOrEmpty(lyricFilePath))
        {
            return null;
        }

        List<Controller.Lyrics.Lyric> lyricList = new List<Controller.Lyrics.Lyric>();
        List<LrcParser.Model.Lyric> sortedLyricData = new List<LrcParser.Model.Lyric>();

        IDictionary<string, string> fileMetaData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string lrcFileContent = System.IO.File.ReadAllText(lyricFilePath);

        try
        {
            // Parse and sort lyric rows
            LyricParser lrcLyricParser = new LrcParser.Parser.Lrc.LrcParser();
            Song lyricData = lrcLyricParser.Decode(lrcFileContent);
            sortedLyricData = lyricData.Lyrics.Where(x => x.TimeTags.Count > 0).OrderBy(x => x.TimeTags.First().Value).ToList();

            // Parse metadata rows
            var metaDataRows = lyricData.Lyrics
                .Where(x => x.TimeTags.Count == 0)
                .Where(x => x.Text.StartsWith('[') && x.Text.EndsWith(']'))
                .Select(x => x.Text)
                .ToList();

            foreach (string metaDataRow in metaDataRows)
            {
                var metaDataField = metaDataRow.Split(':');
                if (metaDataField.Length != 2)
                {
                    continue;
                }

                string metaDataFieldName = metaDataField[0][1..].Trim();
                string metaDataFieldValue = metaDataField[1][..^1].Trim();

                fileMetaData.Add(metaDataFieldName, metaDataFieldValue);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing lyric data from {Provider}", Name);
        }

        if (sortedLyricData.Count == 0)
        {
            return null;
        }

        for (int i = 0; i < sortedLyricData.Count; i++)
        {
            var timeData = sortedLyricData[i].TimeTags.First().Value;
            if (timeData is null)
            {
                continue;
            }

            long ticks = TimeSpan.FromMilliseconds(timeData.Value).Ticks;
            lyricList.Add(new Controller.Lyrics.Lyric(sortedLyricData[i].Text, ticks));
        }

        if (fileMetaData.Count != 0)
        {
            // Map metaData values from LRC file to LyricMetadata properties
            LyricMetadata lyricMetadata = MapMetadataValues(fileMetaData);

            return new LyricResponse { Metadata = lyricMetadata, Lyrics = lyricList };
        }

        return new LyricResponse { Lyrics = lyricList };
    }

    /// <summary>
    /// Converts metadata from an LRC file to LyricMetadata properties.
    /// </summary>
    /// <param name="metaData">The metadata from the LRC file.</param>
    /// <returns>A lyricMetadata object with mapped property data.</returns>
    private LyricMetadata MapMetadataValues(IDictionary<string, string> metaData)
    {
        LyricMetadata lyricMetadata = new LyricMetadata();

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
            lyricMetadata.Length = length;
        }

        if (metaData.TryGetValue("by", out var by) && !string.IsNullOrEmpty(by))
        {
            lyricMetadata.By = by;
        }

        if (metaData.TryGetValue("offset", out var offset) && !string.IsNullOrEmpty(offset))
        {
            lyricMetadata.Offset = offset;
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
}
