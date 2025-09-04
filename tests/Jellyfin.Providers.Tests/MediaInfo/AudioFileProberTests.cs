using MediaBrowser.Providers.MediaInfo;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

public static class AudioFileProberTests
{
    [Theory]
    [InlineData("[00:21.5]", "[00:21.50]")]
    [InlineData("[00:25.0]", "[00:25.00]")]
    [InlineData("[00:14.86]", "[00:14.86]")] // Should not change properly formatted timestamps
    [InlineData("[00:22.66]", "[00:22.66]")] // Should not change properly formatted timestamps
    [InlineData("[00:29.67]", "[00:29.67]")] // Should not change properly formatted timestamps
    public static void FixLrcTimestampPadding_FixesSingleDigitHundredths(string input, string expected)
    {
        var result = AudioFileProber.FixLrcTimestampPadding(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public static void FixLrcTimestampPadding_FixesMultipleTimestampsInContent()
    {
        var input = """
            [00:14.86]But, um, for everything else we ditched that in favor of
            [00:18.37]The base 10 system
            [00:21.5]Which the Egyptians came up with
            [00:22.66]A few hundred years later
            [00:25.0]And this was rounded out by a notation for zero
            [00:29.67]Courtesy of the Mayans a couple hundred years after that
            """;

        var expected = """
            [00:14.86]But, um, for everything else we ditched that in favor of
            [00:18.37]The base 10 system
            [00:21.50]Which the Egyptians came up with
            [00:22.66]A few hundred years later
            [00:25.00]And this was rounded out by a notation for zero
            [00:29.67]Courtesy of the Mayans a couple hundred years after that
            """;

        var result = AudioFileProber.FixLrcTimestampPadding(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public static void FixLrcTimestampPadding_HandlesNullOrEmptyInput(string? input)
    {
        var result = AudioFileProber.FixLrcTimestampPadding(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public static void FixLrcTimestampPadding_DoesNotChangeNonTimestampContent()
    {
        var input = "This is just regular text with numbers like 21.5 and 25.0";
        var result = AudioFileProber.FixLrcTimestampPadding(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public static void FixLrcTimestampPadding_HandlesEdgeCases()
    {
        // Test the specific case from the bug report
        var input = """
            [00:14.86] But, um, for everything else we ditched that in favor of
            [00:18.37] The base 10 system
            [00:21.5] Which the Egyptians came up with
            [00:22.66] A few hundred years later
            [00:25.0] And this was rounded out by a notation for zero
            [00:29.67] Courtesy of the Mayans a couple hundred years after that
            """;

        var expected = """
            [00:14.86] But, um, for everything else we ditched that in favor of
            [00:18.37] The base 10 system
            [00:21.50] Which the Egyptians came up with
            [00:22.66] A few hundred years later
            [00:25.00] And this was rounded out by a notation for zero
            [00:29.67] Courtesy of the Mayans a couple hundred years after that
            """;

        var result = AudioFileProber.FixLrcTimestampPadding(input);
        Assert.Equal(expected, result);
    }
}
