using System;
using System.Globalization;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// Shared helpers for lyric formats whose timestamps are expressed in milliseconds.
/// </summary>
internal static class TimedLyricParserHelpers
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
        catch (ArgumentOutOfRangeException)
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
}
