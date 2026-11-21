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

                var syllables = ParseSyllables(match.Groups[3].Value, lineStart, out var hasTiming);
                if (!TimedLyricParserHelpers.TryAdd(lineStart, lineDuration, out var lineEnd))
                {
                    continue;
                }

                if (syllables.Count > 0)
                {
                    lineEnd = Math.Max(lineEnd, syllables.Max(i => i.End ?? lineEnd));
                    result.Add(new LyricLine(string.Concat(syllables.Select(i => i.Text)), syllables[0].Start)
                    {
                        End = lineEnd,
                        Syllables = syllables
                    });
                }
                else if (!hasTiming && match.Groups[3].Value.Trim().Length > 0)
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

    private static List<LyricSyllable> ParseSyllables(string content, long lineStart, out bool hasTiming)
    {
        var raw = new List<LyricSyllable>();
        var matches = SyllableRegex().Matches(content);
        hasTiming = matches.Count > 0;
        // YRC normally uses absolute times. Only a zero first marker is an
        // unambiguous indication of the relative variant, even if its text is empty.
        var isRelative = matches.Count > 0
            && TimedLyricParserHelpers.TryMilliseconds(matches[0].Groups[1].Value, out var firstStart)
            && firstStart == 0
            && lineStart > 0;
        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            if (!TimedLyricParserHelpers.TryMilliseconds(match.Groups[1].Value, out var start)
                || !TimedLyricParserHelpers.TryMilliseconds(match.Groups[2].Value, out var duration))
            {
                continue;
            }

            var textStart = match.Index + match.Length;
            var textEnd = i + 1 < matches.Count ? matches[i + 1].Index : content.Length;
            var text = content[textStart..textEnd];
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

        if (!isRelative)
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

    [GeneratedRegex(@"\(([+-]?\d+),\s*([+-]?\d+),\s*-?\d+\)")]
    private static partial Regex SyllableRegex();
}
