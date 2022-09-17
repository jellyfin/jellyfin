using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Lyrics;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// TXT Lyric Provider.
/// </summary>
public class TxtLyricProvider : ILyricProvider
{
    /// <inheritdoc />
    public string Name { get; } = "TxtLyricProvider";

    /// <inheritdoc />
    public IEnumerable<string> SupportedMediaTypes
    {
        get => new Collection<string>
            {
                "lrc", "txt"
            };
    }

    /// <summary>
    /// Opens lyric file for the requested item, and processes it for API return.
    /// </summary>
    /// <param name="item">The item to to process.</param>
    /// <returns>If provider can determine lyrics, returns a <see cref="LyricResponse"/>; otherwise, null.</returns>
    public LyricResponse? GetLyrics(BaseItem item)
    {
        string? lyricFilePath = LyricInfo.GetLyricFilePath(this, item.Path);

        if (string.IsNullOrEmpty(lyricFilePath))
        {
            return null;
        }

        string[] lyricTextLines = System.IO.File.ReadAllLines(lyricFilePath);

        List<Controller.Lyrics.Lyric> lyricList = new List<Controller.Lyrics.Lyric>();

        if (lyricTextLines.Length == 0)
        {
            return null;
        }

        foreach (string lyricTextLine in lyricTextLines)
        {
            lyricList.Add(new Controller.Lyrics.Lyric(lyricTextLine));
        }

        return new LyricResponse { Lyrics = lyricList };
    }
}
