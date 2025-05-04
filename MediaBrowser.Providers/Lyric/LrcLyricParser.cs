using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
public partial class LrcLyricParser : ILyricParser
{
    private readonly LyricParser _lrcLyricParser;

    private static readonly string[] _supportedMediaTypes = [".lrc", ".elrc"];

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

        List<LrcParser.Model.Lyric> sortedLyricData = lyricData.Lyrics.OrderBy(x => x.StartTime).ToList();

        if (sortedLyricData.Count == 0)
        {
            return null;
        }

        List<LyricLine> lyricList = [];
        for (var l = 0; l < sortedLyricData.Count; l++)
        {
            var cues = new List<LyricLineCue>();
            var lyric = sortedLyricData[l];

            if (lyric.TimeTags.Count != 0)
            {
                var keys = lyric.TimeTags.Keys.ToList();
                int current = 0, next = 1;
                while (next < keys.Count)
                {
                    var currentKey = keys[current];
                    var currentMs = lyric.TimeTags[currentKey] ?? 0;
                    var nextMs = lyric.TimeTags[keys[next]] ?? 0;

                    cues.Add(new LyricLineCue(
                        position: Math.Max(currentKey.Index, 0),
                        start: TimeSpan.FromMilliseconds(currentMs).Ticks,
                        end: TimeSpan.FromMilliseconds(nextMs).Ticks));

                    current++;
                    next++;
                }

                var lastKey = keys[current];
                var lastMs = lyric.TimeTags[lastKey] ?? 0;

                cues.Add(new LyricLineCue(
                    position: Math.Max(lastKey.Index, 0),
                    start: TimeSpan.FromMilliseconds(lastMs).Ticks,
                    end: l + 1 < sortedLyricData.Count ? TimeSpan.FromMilliseconds(sortedLyricData[l + 1].StartTime).Ticks : null));
            }

            long lyricStartTicks = TimeSpan.FromMilliseconds(lyric.StartTime).Ticks;
            lyricList.Add(new LyricLine(WhitespaceRegex().Replace(lyric.Text.Trim(), " "), lyricStartTicks, cues));
        }

        return new LyricDto { Lyrics = lyricList };
    }

    // Replacement is required until https://github.com/karaoke-dev/LrcParser/issues/83 is resolved.
    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
