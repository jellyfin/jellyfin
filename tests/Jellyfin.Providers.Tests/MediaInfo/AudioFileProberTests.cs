using MediaBrowser.Model.Configuration;
using MediaBrowser.Providers.MediaInfo;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

public class AudioFileProberTests
{
    [Theory]
    [InlineData("Narrator One; Narrator Two", true, new[] { "Narrator One", "Narrator Two" })]
    [InlineData("Narrator One / Narrator Two", true, new[] { "Narrator One", "Narrator Two" })]
    [InlineData("Narrator One; Narrator Two", false, new[] { "Narrator One; Narrator Two" })]
    [InlineData("Narrator One\u001FNarrator Two", false, new[] { "Narrator One", "Narrator Two" })]
    [InlineData("", true, new string[0])]
    public void SplitTagValues_SplitsOnCustomDelimitersWhenEnabled(string value, bool useCustomTagDelimiters, string[] expected)
    {
        var libraryOptions = new LibraryOptions { UseCustomTagDelimiters = useCustomTagDelimiters };

        Assert.Equal(expected, AudioFileProber.SplitTagValues(value, libraryOptions));
    }
}
