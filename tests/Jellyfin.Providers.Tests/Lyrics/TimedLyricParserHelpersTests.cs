using System;
using MediaBrowser.Providers.Lyric;
using Xunit;

namespace Jellyfin.Providers.Tests.Lyrics;

public static class TimedLyricParserHelpersTests
{
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.MaxValue)]
    [InlineData(1e300)]
    [InlineData(-1)]
    public static void TryMillisecondsRejectsInvalidDoubleWithoutThrowing(double value)
    {
        Assert.False(TimedLyricParserHelpers.TryMilliseconds(value, out var ticks));
        Assert.Equal(0, ticks);
    }

    [Fact]
    public static void TryMillisecondsPreservesFractionalMilliseconds()
    {
        Assert.True(TimedLyricParserHelpers.TryMilliseconds(1.5, out var ticks));
        Assert.Equal(15000, ticks);
    }

    [Theory]
    [InlineData(long.MaxValue, -1)]
    [InlineData(long.MinValue, 1)]
    public static void TryApplyOffsetRejectsOverflow(long ticks, long offset)
    {
        Assert.False(TimedLyricParserHelpers.TryApplyOffset(ticks, offset, out var result));
        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData("[offset:9223372036854775807]", 0)]
    [InlineData("[offset:bad]\r[offset:+100]", 1000000)]
    [InlineData("[offset:-100]", -1000000)]
    public static void ParseOffsetIgnoresMalformedValues(string content, long expected)
    {
        Assert.Equal(expected, TimedLyricParserHelpers.ParseOffset(content));
    }
}
