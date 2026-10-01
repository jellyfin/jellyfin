using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Lyrics;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// Parser for NetEase Cloud Music YRC lyrics.
/// </summary>
public partial class NeteaseYrcLyricParser : ILyricParser
{
    private static readonly string[] _supportedMediaTypes = [".yrc"];

    /// <inheritdoc />
    public string Name => "NeteaseYrcLyricProvider";

    /// <inheritdoc />
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
            var result = new List<LyricLine>();
            foreach (var rawLine in TimedLyricParserHelpers.SplitLines(lyrics.Content, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                if (line.StartsWith('{'))
                {
                    continue;
                }

                var match = LineRegex().Match(line);
                if (!match.Success || !TimedLyricParserHelpers.TryMilliseconds(match.Groups[1].Value, out var lineStart)
                    || !TimedLyricParserHelpers.TryMilliseconds(match.Groups[2].Value, out var lineDuration))
                {
                    continue;
                }

                var syllables = ParseSyllables(match.Groups[3].Value, lineStart);
                if (!TimedLyricParserHelpers.TryAdd(lineStart, lineDuration, out var lineEnd))
                {
                    continue;
                }

                if (syllables.Count > 0)
                {
                    lineEnd = Math.Max(lineEnd, syllables[^1].End ?? lineEnd);
                    result.Add(new LyricLine(string.Concat(syllables.Select(i => i.Text)), syllables[0].Start)
                    {
                        End = lineEnd,
                        Syllables = syllables
                    });
                }
                else if (match.Groups[3].Value.Trim().Length > 0)
                {
                    result.Add(new LyricLine(match.Groups[3].Value.Trim(), lineStart) { End = lineEnd });
                }
            }

            return result.Count == 0
                ? null
                : new LyricDto
                {
                    Tracks = [new LyricTrack { Type = LyricTrackType.Main, Lines = result }]
                };
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static List<LyricSyllable> ParseSyllables(string content, long lineStart)
    {
        var raw = new List<LyricSyllable>();
        foreach (Match match in SyllableRegex().Matches(content))
        {
            if (!TimedLyricParserHelpers.TryMilliseconds(match.Groups[1].Value, out var start)
                || !TimedLyricParserHelpers.TryMilliseconds(match.Groups[2].Value, out var duration))
            {
                continue;
            }

            var text = match.Groups[3].Value;
            if (text.Length == 0)
            {
                continue;
            }

            if (!TimedLyricParserHelpers.TryAdd(start, duration, out var end))
            {
                continue;
            }

            raw.Add(new LyricSyllable { Text = text, Start = start, End = end });
        }

        if (raw.Count == 0 || raw[0].Start >= lineStart)
        {
            return raw;
        }

        foreach (var syllable in raw)
        {
            if (!TimedLyricParserHelpers.TryAdd(syllable.Start, lineStart, out var start)
                || syllable.End is not long end
                || !TimedLyricParserHelpers.TryAdd(end, lineStart, out var adjustedEnd))
            {
                return [];
            }

            syllable.Start = start;
            syllable.End = adjustedEnd;
        }

        return raw;
    }

    [GeneratedRegex(@"^\[(\d+),\s*(\d+)\](.*)$")]
    private static partial Regex LineRegex();

    [GeneratedRegex(@"\((\d+),\s*(\d+),\s*-?\d+\)([^()\r\n]*)")]
    private static partial Regex SyllableRegex();
}
