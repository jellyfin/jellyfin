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
    public IReadOnlyList<string> SupportedExtensions => _supportedMediaTypes;

    /// <inheritdoc />
    public LyricDto? ParseLyrics(LyricFile lyrics)
    {
        if (!_supportedMediaTypes.Contains(Path.GetExtension(lyrics.Name.AsSpan()), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var lyricData = _lrcLyricParser.Decode(lyrics.Content);
            List<LrcParser.Model.Lyric> sortedLyricData = lyricData.Lyrics.OrderBy(x => x.StartTime).ToList();

            if (sortedLyricData.Count == 0)
            {
                return null;
            }

            List<LyricLine> lyricList = [];
            for (var lineIndex = 0; lineIndex < sortedLyricData.Count; lineIndex++)
            {
                var lyric = sortedLyricData[lineIndex];
                if (lyric.Text is null || !TryConvertMilliseconds(lyric.StartTime, out var lyricStartTicks))
                {
                    return null;
                }

                long? lyricEndTicks = null;
                if (lineIndex + 1 < sortedLyricData.Count)
                {
                    if (!TryConvertMilliseconds(sortedLyricData[lineIndex + 1].StartTime, out var nextLineStartTicks))
                    {
                        return null;
                    }

                    lyricEndTicks = nextLineStartTicks;
                }

                var syllables = new List<LyricSyllable>();
                if (lyric.TimeTags.Count > 0)
                {
                    var keys = lyric.TimeTags.Keys.ToList();
                    for (var tagIndex = 0; tagIndex < keys.Count - 1; tagIndex++)
                    {
                        var currentKey = keys[tagIndex];
                        var nextKey = keys[tagIndex + 1];
                        var currentPos = currentKey.State == IndexState.End ? (long)currentKey.Index + 1 : currentKey.Index;
                        var nextPos = nextKey.State == IndexState.End ? (long)nextKey.Index + 1 : nextKey.Index;
                        if (currentPos < 0 || nextPos < currentPos || nextPos > lyric.Text.Length
                            || !TryConvertMilliseconds(lyric.TimeTags[currentKey] ?? 0, out var currentTicks)
                            || !TryConvertMilliseconds(lyric.TimeTags[nextKey] ?? 0, out var nextTicks))
                        {
                            return null;
                        }

                        var currentSlice = lyric.Text[(int)currentPos..(int)nextPos];
                        if (currentSlice.Trim().Length > 0)
                        {
                            syllables.Add(new LyricSyllable
                            {
                                Text = currentSlice,
                                Start = currentTicks,
                                End = nextTicks
                            });
                        }
                    }

                    var lastKey = keys[^1];
                    var lastPos = lastKey.State == IndexState.End ? (long)lastKey.Index + 1 : lastKey.Index;
                    if (lastPos < 0 || lastPos > lyric.Text.Length
                        || !TryConvertMilliseconds(lyric.TimeTags[lastKey] ?? 0, out var lastTicks))
                    {
                        return null;
                    }

                    var lastSlice = lyric.Text[(int)lastPos..];
                    if (lastSlice.Trim().Length > 0)
                    {
                        syllables.Add(new LyricSyllable
                        {
                            Text = lastSlice,
                            Start = lastTicks,
                            End = lyricEndTicks
                        });
                    }
                }

                lyricList.Add(new LyricLine(lyric.Text, lyricStartTicks)
                {
                    End = lyricEndTicks,
                    Syllables = syllables
                });
            }

            return new LyricDto
            {
                Tracks =
                [
                    new LyricTrack
                    {
                        Type = LyricTrackType.Main,
                        Lines = lyricList
                    }
                ]
            };
        }
        catch (Exception)
        {
            // Malformed parser output must not abort lyric loading.
            return null;
        }
    }

    private static bool TryConvertMilliseconds(double milliseconds, out long ticks)
    {
        if (!double.IsFinite(milliseconds) || milliseconds < 0)
        {
            ticks = 0;
            return false;
        }

        try
        {
            ticks = TimeSpan.FromMilliseconds(milliseconds).Ticks;
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            ticks = 0;
            return false;
        }
    }
}
