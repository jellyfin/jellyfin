using System;
using MediaBrowser.Model.Drawing;
using Xunit;

namespace Jellyfin.Model.Drawing;

public static class AspectRatioLookupTests
{
    [Theory]
    [InlineData(1.78, 1.78)]
    [InlineData(2.39, 2.39)]
    [InlineData(1.33, 1.33)]
    [InlineData(2.76, 2.76)]
    [InlineData(1.85, 1.85)]
    public static void SnapToStandard_ExactMatch_ReturnsMatch(double input, double expected)
        => Assert.Equal(expected, AspectRatioLookup.SnapToStandard(input));

    [Theory]
    [InlineData(1.80, 1.78)]
    [InlineData(2.42, 2.40)]
    [InlineData(1.35, 1.33)]
    public static void SnapToStandard_WithinTolerance_SnapsToNearest(double input, double expected)
        => Assert.Equal(expected, AspectRatioLookup.SnapToStandard(input));

    [Theory]
    [InlineData(0.5)]
    [InlineData(3.5)]
    [InlineData(5.0)]
    public static void SnapToStandard_OutsideTolerance_ReturnsNegativeOne(double input)
        => Assert.Equal(-1.0, AspectRatioLookup.SnapToStandard(input));

    [Theory]
    [InlineData(1.78, "16:9 HDTV")]
    [InlineData(2.39, "Modern Anamorphic Scope")]
    [InlineData(1.33, "4:3 Standard TV")]
    [InlineData(2.76, "Ultra Panavision 70")]
    public static void GetName_KnownRatio_ReturnsName(double ratio, string expected)
        => Assert.Equal(expected, AspectRatioLookup.GetName(ratio), StringComparer.Ordinal);

    [Theory]
    [InlineData(0.5)]
    [InlineData(3.5)]
    [InlineData(99.0)]
    public static void GetName_UnknownRatio_ReturnsNull(double ratio)
        => Assert.Null(AspectRatioLookup.GetName(ratio));

    [Theory]
    [InlineData(2.35, "2.350")]
    [InlineData(1.0, "1.000")]
    [InlineData(1.7777, "1.778")]
    [InlineData(2.3999, "2.400")]
    public static void Format_ReturnsThreeDecimalInvariant(double input, string expected)
        => Assert.Equal(expected, AspectRatioLookup.Format(input), StringComparer.Ordinal);
}
