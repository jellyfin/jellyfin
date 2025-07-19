using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        for (var lineIndex = 0; lineIndex < sortedLyricData.Count; lineIndex++)
        {
            var lyric = sortedLyricData[lineIndex];

            // Extract cues from time tags
            var cues = new List<LyricLineCue>();
            if (lyric.TimeTags.Count > 0)
            {
                var keys = lyric.TimeTags.Keys.ToList();
                for (var tagIndex = 0; tagIndex < keys.Count - 1; tagIndex++)
                {
                    var currentKey = keys[tagIndex];
                    var nextKey = keys[tagIndex + 1];

                    var currentPos = currentKey.State == IndexState.End ? currentKey.Index + 1 : currentKey.Index;
                    var nextPos = nextKey.State == IndexState.End ? nextKey.Index + 1 : nextKey.Index;
                    var currentMs = lyric.TimeTags[currentKey] ?? 0;
                    var nextMs = lyric.TimeTags[keys[tagIndex + 1]] ?? 0;
                    var currentSlice = lyric.Text[currentPos..nextPos];
                    var currentSliceTrimmed = currentSlice.Trim();
                    if (currentSliceTrimmed.Length > 0)
                    {
                        cues.Add(new LyricLineCue(
                            position: currentPos,
                            endPosition: nextPos,
                            start: TimeSpan.FromMilliseconds(currentMs).Ticks,
                            end: TimeSpan.FromMilliseconds(nextMs).Ticks));
                    }
                }

                var lastKey = keys[^1];
                var lastPos = lastKey.State == IndexState.End ? lastKey.Index + 1 : lastKey.Index;
                var lastMs = lyric.TimeTags[lastKey] ?? 0;
                var lastSlice = lyric.Text[lastPos..];
                var lastSliceTrimmed = lastSlice.Trim();

                if (lastSliceTrimmed.Length > 0)
                {
                    cues.Add(new LyricLineCue(
                        position: lastPos,
                        endPosition: lyric.Text.Length,
                        start: TimeSpan.FromMilliseconds(lastMs).Ticks,
                        end: lineIndex + 1 < sortedLyricData.Count ? TimeSpan.FromMilliseconds(sortedLyricData[lineIndex + 1].StartTime).Ticks : null));
                }
            }

            long lyricStartTicks = TimeSpan.FromMilliseconds(lyric.StartTime).Ticks;
            lyricList.Add(new LyricLine(lyric.Text, lyricStartTicks, cues));
        }

        return new LyricDto { Lyrics = lyricList };
    }
}
