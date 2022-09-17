using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using LrcParser.Model;
using LrcParser.Parser;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Lyrics;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// LRC Lyric Provider.
/// </summary>
public class LrcLyricProvider : ILyricProvider
{
    /// <inheritdoc />
    public string Name { get; } = "LrcLyricProvider";

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

        IDictionary<string, string> metaData = new Dictionary<string, string>();
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

                metaData.Add(metaDataFieldName, metaDataFieldValue);
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
            var timeData = sortedLyricData[i].TimeTags.ToArray()[0].Value;
            long ticks = TimeSpan.FromMilliseconds((double)timeData).Ticks;
            lyricList.Add(new Controller.Lyrics.Lyric(sortedLyricData[i].Text, ticks));
        }

        if (metaData.Any())
        {
           return new LyricResponse { Metadata = metaData, Lyrics = lyricList };
        }

        return new LyricResponse { Lyrics = lyricList };
    }
}
