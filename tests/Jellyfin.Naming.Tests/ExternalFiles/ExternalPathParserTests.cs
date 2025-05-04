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
        var hindiCultureDto = new CultureDto("Hindi", "Hindi", "hi", new[] { "hin" });

        var localizationManager = new Mock<ILocalizationManager>(MockBehavior.Loose);
        localizationManager.Setup(lm => lm.FindLanguageInfo(It.IsRegex("en.*", RegexOptions.IgnoreCase)))
            .Returns(englishCultureDto);
        localizationManager.Setup(lm => lm.FindLanguageInfo(It.IsRegex("fr.*", RegexOptions.IgnoreCase)))
            .Returns(frenchCultureDto);
        localizationManager.Setup(lm => lm.FindLanguageInfo(It.IsRegex("hi.*", RegexOptions.IgnoreCase)))
            .Returns(hindiCultureDto);

        _audioPathParser = new ExternalPathParser(new NamingOptions(), localizationManager.Object, DlnaProfileType.Audio);
        _subtitlePathParser = new ExternalPathParser(new NamingOptions(), localizationManager.Object, DlnaProfileType.Subtitle);
    }

    [Theory]
    [InlineData("")]
    [InlineData("MyVideo.ass")]
    [InlineData("MyVideo.mks")]
    [InlineData("MyVideo.sami")]
    [InlineData("MyVideo.srt")]
    [InlineData("MyVideo.m4v")]
    public void ParseFile_AudioExtensionsNotMatched_ReturnsNull(string path)
    {
        Assert.Null(_audioPathParser.ParseFile(path, string.Empty));
    }

    [Theory]
    [InlineData("MyVideo.aa")]
    [InlineData("MyVideo.aac")]
    [InlineData("MyVideo.flac")]
    [InlineData("MyVideo.m4a")]
    [InlineData("MyVideo.mka")]
    [InlineData("MyVideo.mp3")]
    public void ParseFile_AudioExtensionsMatched_ReturnsPath(string path)
    {
        var actual = _audioPathParser.ParseFile(path, string.Empty);
        Assert.NotNull(actual);
        Assert.Equal(path, actual!.Path);
    }

    [Theory]
    [InlineData("")]
    [InlineData("MyVideo.aa")]
    [InlineData("MyVideo.aac")]
    [InlineData("MyVideo.flac")]
    [InlineData("MyVideo.mka")]
    [InlineData("MyVideo.m4v")]
    public void ParseFile_SubtitleExtensionsNotMatched_ReturnsNull(string path)
    {
        Assert.Null(_subtitlePathParser.ParseFile(path, string.Empty));
    }

    [Theory]
    [InlineData("MyVideo.ass")]
    [InlineData("MyVideo.mks")]
    [InlineData("MyVideo.sami")]
    [InlineData("MyVideo.srt")]
    [InlineData("MyVideo.vtt")]
    public void ParseFile_SubtitleExtensionsMatched_ReturnsPath(string path)
    {
        var actual = _subtitlePathParser.ParseFile(path, string.Empty);
        Assert.NotNull(actual);
        Assert.Equal(path, actual!.Path);
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
    [InlineData(".hi", null, "hin")]
    [InlineData(".fr.en", "fr", "eng")]
    [InlineData(".en.fr", "en", "fre")]
    [InlineData(".title.en.fr", "title.en", "fre")]
    [InlineData(".Title Goes Here", "Title Goes Here", null)]
    [InlineData(".Title.with.Separator", "Title.with.Separator", null)]
    [InlineData(".title.en.default.forced", "title", "eng", true, true)]
    [InlineData(".forced.default.en.title", "title", "eng", true, true)]
    [InlineData(".sdh.en.title", "title", "eng", false, false, true)]
    [InlineData(".en.cc.title", "title", "eng", false, false, true)]
    [InlineData(".hi.en.title", "title", "eng", false, false, true)]
    [InlineData(".en.hi.title", "title", "eng", false, false, true)]
    [InlineData(".Subs for Chinese Audio.eng", "Subs for Chinese Audio", "eng", false, false, false)]
    public void ParseFile_ExtraTokens_ParseToValues(string tokens, string? title, string? language, bool isDefault = false, bool isForced = false, bool isHearingImpaired = false)
    {
        var path = "My.Video" + tokens + ".srt";

        var actual = _subtitlePathParser.ParseFile(path, tokens);

        Assert.NotNull(actual);
        Assert.Equal(title, actual!.Title);
        Assert.Equal(language, actual.Language);
        Assert.Equal(isDefault, actual.IsDefault);
        Assert.Equal(isForced, actual.IsForced);
        Assert.Equal(isHearingImpaired, actual.IsHearingImpaired);
    }
}
