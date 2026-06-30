using MediaBrowser.Providers.MediaInfo;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

public class AudioFileProberTests
{
    [Theory]
    [InlineData("01 Opening Credits", "Opening Credits")]
    [InlineData("04 Chapter 1", "Chapter 1")]
    [InlineData("076 End Credits", "End Credits")]
    [InlineData("01_Prologue", "Prologue")]
    [InlineData("12-Epilogue", "Epilogue")]
    [InlineData("Rhythm of War Chapter 34", "Rhythm of War Chapter 34")]
    [InlineData("1984Foreword", "1984Foreword")]
    [InlineData("01 ", "01 ")]
    [InlineData("Plain Name", "Plain Name")]
    public void StripLeadingTrackNumber_RemovesOnlyLeadingOrderPrefix(string input, string expected)
    {
        Assert.Equal(expected, AudioFileProber.StripLeadingTrackNumber(input));
    }
}
