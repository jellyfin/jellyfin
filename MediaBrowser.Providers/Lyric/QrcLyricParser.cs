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
            foreach (var rawLine in content.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                var match = LineRegex().Match(line);
                if (!match.Success || !TryMilliseconds(match.Groups[1].Value, out var lineStart)
                    || !TryMilliseconds(match.Groups[2].Value, out var lineDuration))
                {
                    continue;
                }

                if (!TryAdd(lineStart, offset, out lineStart)
                    || !TryAdd(lineStart, lineDuration, out var lineEnd))
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
            if (!TryMilliseconds(match.Groups[1].Value, out var start)
                || !TryMilliseconds(match.Groups[2].Value, out var duration)
                || !TryAdd(start, duration, out var end)
                || !TryAdd(start, offset, out start)
                || !TryAdd(end, offset, out end))
            {
                textStart = match.Index + match.Length;
                continue;
            }

            result.Add(new LyricSyllable { Text = text, Start = start, End = end });
            textStart = match.Index + match.Length;
        }

        return result;
    }

    private static bool TryMilliseconds(string value, out long ticks)
    {
        if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var milliseconds) || milliseconds < 0)
        {
            ticks = 0;
            return false;
        }

        try
        {
            ticks = checked(milliseconds * TimeSpan.TicksPerMillisecond);
            return true;
        }
        catch (OverflowException)
        {
            ticks = 0;
            return false;
        }
    }

    private static bool TryAdd(long first, long second, out long result)
    {
        try
        {
            result = checked(first + second);
            return true;
        }
        catch (OverflowException)
        {
            result = 0;
            return false;
        }
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
