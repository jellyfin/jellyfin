using System.Text.RegularExpressions;
using Emby.Naming.Common;
using Emby.Naming.ExternalFiles;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Globalization;
using Moq;
using Xunit;

namespace Jellyfin.Naming.Tests.ExternalFiles;

public class ExternalPathParserTests
{
    private readonly ExternalPathParser _audioPathParser;
    private readonly ExternalPathParser _subtitlePathParser;

    public ExternalPathParserTests()
    {
        var englishCultureDto = new CultureDto("English", "English", "en", new[] { "eng" });
        var frenchCultureDto = new CultureDto("French", "French", "fr", new[] { "fre", "fra" });

        var localizationManager = new Mock<ILocalizationManager>(MockBehavior.Loose);
        localizationManager.Setup(lm => lm.FindLanguageInfo(It.IsRegex(@"en.*", RegexOptions.IgnoreCase)))
            .Returns(englishCultureDto);
        localizationManager.Setup(lm => lm.FindLanguageInfo(It.IsRegex(@"fr.*", RegexOptions.IgnoreCase)))
            .Returns(frenchCultureDto);

        _audioPathParser = new ExternalPathParser(new NamingOptions(), localizationManager.Object, DlnaProfileType.Audio);
        _subtitlePathParser = new ExternalPathParser(new NamingOptions(), localizationManager.Object, DlnaProfileType.Subtitle);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("MyVideo.srt", false)]
    [InlineData("MyVideo.mka", true)]
    public void ParseFile_AudioFile_ReturnsPathWhenAudio(string path, bool valid)
    {
        var actual = _audioPathParser.ParseFile(path, string.Empty);

        if (valid)
        {
            Assert.NotNull(actual);
            Assert.Equal(path, actual!.Path);
        }
        else
        {
            Assert.Null(actual);
        }
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("MyVideo.srt", true)]
    [InlineData("MyVideo.mka", false)]
    public void ParseFile_SubtitleFile_ReturnsPathWhenSubtitle(string path, bool valid)
    {
        var actual = _subtitlePathParser.ParseFile(path, string.Empty);

        if (valid)
        {
            Assert.NotNull(actual);
            Assert.Equal(path, actual!.Path);
        }
        else
        {
            Assert.Null(actual);
        }
    }

    [Theory]
    [InlineData("", null, null)]
    [InlineData(".default", null, null, true, false)]
    [InlineData(".forced", null, null, false, true)]
    [InlineData(".foreign", null, null, false, true)]
    [InlineData(".default.forced", null, null, true, true)]
    [InlineData(".forced.default", null, null, true, true)]
    [InlineData(".DEFAULT.FORCED", null, null, true, true)]
    [InlineData(".en", null, "eng")]
    [InlineData(".EN", null, "eng")]
    [InlineData(".fr.en", "fr", "eng")]
    [InlineData(".en.fr", "en", "fre")]
    [InlineData(".title.en.fr", "title.en", "fre")]
    [InlineData(".Title Goes Here", "Title Goes Here", null)]
    [InlineData(".Title.with.Separator", "Title.with.Separator", null)]
    [InlineData(".title.en.default.forced", "title", "eng", true, true)]
    [InlineData(".forced.default.en.title", "title", "eng", true, true)]
    public void ParseFile_ExtraTokens_ParseToValues(string tokens, string? title, string? language, bool isDefault = false, bool isForced = false)
    {
        var path = "My.Video" + tokens + ".srt";

        var actual = _subtitlePathParser.ParseFile(path, tokens);

        Assert.NotNull(actual);
        Assert.Equal(title, actual!.Title);
        Assert.Equal(language, actual.Language);
        Assert.Equal(isDefault, actual.IsDefault);
        Assert.Equal(isForced, actual.IsForced);
    }
}
