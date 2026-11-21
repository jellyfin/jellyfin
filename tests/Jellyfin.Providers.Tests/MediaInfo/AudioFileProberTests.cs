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

    [Theory]
    [InlineData("Artist B / Artist C", "Artist A", new[] { "Artist B", "Artist C" })]
    [InlineData(null, "Artist A", new[] { "Artist A" })]
    [InlineData("", "Artist A", new[] { "Artist A" })]
    [InlineData("/", "Artist A", new[] { "Artist A" })]
    public void SplitTagValuesWithFallback_UsesFallbackWhenPreferredTagHasNoValues(string? preferred, string fallback, string[] expected)
    {
        var libraryOptions = new LibraryOptions { UseCustomTagDelimiters = true };

        Assert.Equal(expected, AudioFileProber.SplitTagValuesWithFallback(preferred, fallback, libraryOptions));
    }
}
