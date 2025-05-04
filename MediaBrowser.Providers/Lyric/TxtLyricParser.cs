using System;
using System.IO;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Lyrics;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// TXT Lyric Parser.
/// </summary>
public class TxtLyricParser : ILyricParser
{
    private static readonly string[] _supportedMediaTypes = [".lrc", ".elrc", ".txt"];
    private static readonly string[] _lineBreakCharacters = ["\r\n", "\r", "\n"];

    /// <inheritdoc />
    public string Name => "TxtLyricProvider";

    /// <summary>
    /// Gets the priority.
    /// </summary>
    /// <value>The priority.</value>
    public ResolverPriority Priority => ResolverPriority.Fifth;

    /// <inheritdoc />
    public LyricDto? ParseLyrics(LyricFile lyrics)
    {
        if (!_supportedMediaTypes.Contains(Path.GetExtension(lyrics.Name.AsSpan()), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string[] lyricTextLines = lyrics.Content.Split(_lineBreakCharacters, StringSplitOptions.None);
        LyricLine[] lyricList = new LyricLine[lyricTextLines.Length];

        for (int lyricLineIndex = 0; lyricLineIndex < lyricTextLines.Length; lyricLineIndex++)
        {
            lyricList[lyricLineIndex] = new LyricLine(lyricTextLines[lyricLineIndex].Trim());
        }

        return new LyricDto { Lyrics = lyricList };
    }
}
