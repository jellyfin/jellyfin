using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MediaBrowser.Model.Lyrics;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// Shared helpers for lyric formats whose timestamps are expressed in milliseconds.
/// </summary>
internal static partial class TimedLyricParserHelpers
{
    public static string[] SplitLines(string content, StringSplitOptions options)
        => content.Split(["\r\n", "\r", "\n"], options);

    public static bool TryMilliseconds(string value, out long ticks)
    {
        if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var milliseconds)
            || milliseconds < 0)
        {
            ticks = 0;
            return false;
        }

        return TryMilliseconds(milliseconds, out ticks);
    }

    public static bool TryMilliseconds(long milliseconds, out long ticks)
    {
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

    public static bool TryMilliseconds(double milliseconds, out long ticks)
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
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or OverflowException)
        {
            ticks = 0;
            return false;
        }
    }

    public static bool TryAdd(long first, long second, out long result)
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

    public static long ParseOffset(string content)
    {
        foreach (var line in SplitLines(content, StringSplitOptions.RemoveEmptyEntries))
        {
            var match = OffsetRegex().Match(line.Trim());
            if (match.Success
                && long.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var milliseconds)
                && TryMilliseconds(milliseconds, out var ticks))
            {
                return ticks;
            }
        }

        return 0;
    }

    public static bool TryApplyOffset(long ticks, long offset, out long result)
    {
        try
        {
            // A positive lyric offset advances the lyrics, rather than delaying them.
            result = checked(ticks - offset);
            return true;
        }
        catch (OverflowException)
        {
            result = 0;
            return false;
        }
    }

    public static List<LyricSyllable> ParsePostfixSyllables(string content, long offset, out bool hasTiming)
    {
        var result = new List<LyricSyllable>();
        hasTiming = false;
        var textStart = 0;
        foreach (Match match in PostfixTimingRegex().Matches(content))
        {
            hasTiming = true;
            var text = content[textStart..match.Index];
            textStart = match.Index + match.Length;
            if (!TryMilliseconds(match.Groups[1].Value, out var start)
                || !TryMilliseconds(match.Groups[2].Value, out var duration)
                || !TryApplyOffset(start, offset, out start)
                || !TryAdd(start, duration, out var end))
            {
                continue;
            }

            result.Add(new LyricSyllable { Text = text, Start = start, End = end });
        }

        if (result.Count > 0)
        {
            result[^1].Text += content[textStart..];
        }

        return result;
    }

    public static void StripBackgroundParentheses(List<LyricSyllable> syllables)
    {
        var text = string.Concat(syllables.Select(i => i.Text));
        var first = 0;
        var last = text.Length - 1;
        while (first <= last && char.IsWhiteSpace(text[first]))
        {
            first++;
        }

        while (last >= first && char.IsWhiteSpace(text[last]))
        {
            last--;
        }

        if (first >= last || text[first] is not ('(' or '（'))
        {
            return;
        }

        var opening = text[first];
        var closing = opening == '(' ? ')' : '）';
        if (text[last] != closing)
        {
            return;
        }

        var depth = 0;
        for (var i = first; i <= last; i++)
        {
            if (text[i] == opening)
            {
                depth++;
            }
            else if (text[i] == closing)
            {
                depth--;
            }

            // Only remove a pair enclosing the whole lyric, not separate phrases.
            if (depth < 0 || (depth == 0 && i < last))
            {
                return;
            }
        }

        if (depth != 0)
        {
            return;
        }

        var position = 0;
        foreach (var syllable in syllables)
        {
            var originalLength = syllable.Text.Length;
            if (last >= position && last < position + originalLength)
            {
                syllable.Text = syllable.Text.Remove(last - position, 1);
            }

            if (first >= position && first < position + originalLength)
            {
                syllable.Text = syllable.Text.Remove(first - position, 1);
            }

            position += originalLength;
        }
    }

    [GeneratedRegex(@"^\[offset:\s*([+-]?\d+)\]$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OffsetRegex();

    // Recognize signed malformed markers too, so they cannot become plain lyrics.
    [GeneratedRegex(@"\(([+-]?\d+),\s*([+-]?\d+)\)")]
    private static partial Regex PostfixTimingRegex();
}
