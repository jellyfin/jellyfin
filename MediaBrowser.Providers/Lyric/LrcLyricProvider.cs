using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using LrcParser.Model;
using LrcParser.Parser;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Lyrics;
using Newtonsoft.Json.Linq;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// LRC Lyric Provider.
/// </summary>
public class LrcLyricProvider : ILyricProvider
{
    /// <inheritdoc />
    public string Name => "LrcLyricProvider";

    /// <inheritdoc />
    public IEnumerable<string> SupportedMediaTypes
    {
        get => new Collection<string>
            {
                "lrc"
            };
    }

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

        IDictionary<string, string> fileMetaData = new Dictionary<string, string>();
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

                string metaDataFieldName = metaDataField[0][1..].Trim().ToLowerInvariant();
                string metaDataFieldValue = metaDataField[1][..^1].Trim();

                fileMetaData.Add(metaDataFieldName, metaDataFieldValue);
            }
        }
        catch
        {
            return null;
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

            long ticks = TimeSpan.FromMilliseconds((double)timeData).Ticks;
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

        if (metaData.TryGetValue("ar", out var artist) && artist is not null)
        {
            lyricMetadata.Artist = artist;
        }

        if (metaData.TryGetValue("al", out var album) && album is not null)
        {
            lyricMetadata.Album = album;
        }

        if (metaData.TryGetValue("ti", out var title) && title is not null)
        {
            lyricMetadata.Title = title;
        }

        if (metaData.TryGetValue("au", out var author) && author is not null)
        {
            lyricMetadata.Author = author;
        }

        if (metaData.TryGetValue("length", out var length) && length is not null)
        {
            lyricMetadata.Length = length;
        }

        if (metaData.TryGetValue("by", out var by) && by is not null)
        {
            lyricMetadata.By = by;
        }

        if (metaData.TryGetValue("offset", out var offset) && offset is not null)
        {
            lyricMetadata.Offset = offset;
        }

        if (metaData.TryGetValue("re", out var creator) && creator is not null)
        {
            lyricMetadata.Creator = creator;
        }

        if (metaData.TryGetValue("ve", out var version) && version is not null)
        {
            lyricMetadata.Version = version;
        }

        return lyricMetadata;

    }
}
