using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Lyrics;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// Parser for QQ Music QRC lyrics.
/// </summary>
public partial class QrcLyricParser : ILyricParser
{
    private static readonly string[] _supportedMediaTypes = [".qrc"];

    /// <inheritdoc />
    public string Name => "QrcLyricProvider";

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
            var content = ExtractQrcContent(lyrics.Content);
            var offset = ParseOffset(content);
            var result = new List<LyricLine>();
            foreach (var rawLine in TimedLyricParserHelpers.SplitLines(content, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                var match = LineRegex().Match(line);
                if (!match.Success || !TimedLyricParserHelpers.TryMilliseconds(match.Groups[1].Value, out var lineStart)
                    || !TimedLyricParserHelpers.TryMilliseconds(match.Groups[2].Value, out var lineDuration))
                {
                    continue;
                }

                if (!TimedLyricParserHelpers.TryAdd(lineStart, offset, out lineStart)
                    || !TimedLyricParserHelpers.TryAdd(lineStart, lineDuration, out var lineEnd))
                {
                    continue;
                }

                var syllables = ParseSyllables(match.Groups[3].Value, offset);
                if (syllables.Count == 0)
                {
                    var plainText = match.Groups[3].Value.Trim();
                    if (plainText.Length > 0)
                    {
                        result.Add(new LyricLine(plainText, lineStart) { End = lineEnd });
                    }

                    continue;
                }

                lineEnd = Math.Max(lineEnd, syllables[^1].End ?? lineEnd);
                result.Add(new LyricLine(string.Concat(syllables.Select(i => i.Text)), lineStart)
                {
                    End = lineEnd,
                    Syllables = syllables
                });
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

    private static string ExtractQrcContent(string content)
    {
        var match = XmlContentRegex().Match(content);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : content;
    }

    private static long ParseOffset(string content)
    {
        var match = OffsetRegex().Match(content);
        if (!match.Success
            || !long.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var milliseconds))
        {
            return 0;
        }

        try
        {
            return checked(milliseconds * TimeSpan.TicksPerMillisecond);
        }
        catch (OverflowException)
        {
            return 0;
        }
    }

    private static List<LyricSyllable> ParseSyllables(string content, long offset)
    {
        var result = new List<LyricSyllable>();
        var textStart = 0;
        foreach (Match match in SyllableRegex().Matches(content))
        {
            var text = content[textStart..match.Index];
            if (!TimedLyricParserHelpers.TryMilliseconds(match.Groups[1].Value, out var start)
                || !TimedLyricParserHelpers.TryMilliseconds(match.Groups[2].Value, out var duration)
                || !TimedLyricParserHelpers.TryAdd(start, duration, out var end)
                || !TimedLyricParserHelpers.TryAdd(start, offset, out start)
                || !TimedLyricParserHelpers.TryAdd(end, offset, out end))
            {
                textStart = match.Index + match.Length;
                continue;
            }

            result.Add(new LyricSyllable { Text = text, Start = start, End = end });
            textStart = match.Index + match.Length;
        }

        return result;
    }

    [GeneratedRegex(@"^\[(\d+),\s*(\d+)\](.*)$")]
    private static partial Regex LineRegex();

    [GeneratedRegex(@"\((\d+),\s*(\d+)\)")]
    private static partial Regex SyllableRegex();

    [GeneratedRegex(@"^\[offset:\s*(-?\d+)\]$", RegexOptions.Multiline)]
    private static partial Regex OffsetRegex();

    [GeneratedRegex(@"LyricContent\s*=\s*""([\s\S]*?)""")]
    private static partial Regex XmlContentRegex();
}
