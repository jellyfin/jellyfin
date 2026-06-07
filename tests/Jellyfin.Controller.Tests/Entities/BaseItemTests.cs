using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

public class BaseItemTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("1", "0000000001")]
    [InlineData("t", "t")]
    [InlineData("test", "test")]
    [InlineData("test1", "test0000000001")]
    [InlineData("1test 2", "0000000001test 0000000002")]
    public void BaseItem_ModifySortChunks_Valid(string input, string expected)
        => Assert.Equal(expected, BaseItem.ModifySortChunks(input));

    [Theory]
    [InlineData("/Movies/Ted/Ted.mp4", "/Movies/Ted/Ted - Unrated Edition.mp4", "Ted", "Unrated Edition")]
    [InlineData("/Movies/Deadpool 2 (2018)/Deadpool 2 (2018).mkv", "/Movies/Deadpool 2 (2018)/Deadpool 2 (2018) - Super Duper Cut.mkv", "Deadpool 2 (2018)", "Super Duper Cut")]
    public void GetMediaSourceName_Valid(string primaryPath, string altPath, string name, string altName)
    {
        var video = new Video()
        {
            Path = primaryPath
        };

        var videoAlt = new Video()
        {
            Path = altPath,
        };

        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>()))
                .Returns((string x) => MediaProtocol.File);
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(x => x.GetLocalAlternateVersionIds(It.IsAny<Video>()))
                .Returns([Guid.Empty]);
        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        BaseItem.LibraryManager = libraryManager.Object;

        Assert.Equal(name, video.GetMediaSourceName(video));
        Assert.Equal(altName, video.GetMediaSourceName(videoAlt));
    }

    [Theory]
    // Episode versions share a season folder; the common prefix (not the folder name) yields the label.
    // Both files carry a suffix (no bare base name), so the shared "- " must be stripped too.
    [InlineData(
        "Spider-Noir - S01E02 - Wo ist Flint - Greyscale",
        "Spider-Noir - S01E02 - Wo ist Flint - Colorized",
        "Greyscale",
        "Colorized")]
    // One version is the bare base name; the other is suffixed.
    [InlineData(
        "Spider-Noir - S01E02 - Wo ist Flint",
        "Spider-Noir - S01E02 - Wo ist Flint - Greyscale",
        "Spider-Noir - S01E02 - Wo ist Flint",
        "Greyscale")]
    // Suffixes share a leading word ("Grey"); the prefix must retreat to the separator, not split it.
    [InlineData(
        "Demo - S01E01 - Greyscale",
        "Demo - S01E01 - Greyish",
        "Greyscale",
        "Greyish")]
    // Underscore separator.
    [InlineData("Movie (2020)_4K", "Movie (2020)_1080p", "4K", "1080p")]
    // Dot separator.
    [InlineData("Movie (2020).UHD", "Movie (2020).1080p", "UHD", "1080p")]
    // Resolution variants that share leading digits must retreat to the separator, not yield "p"/"i".
    [InlineData("Movie - 1080p", "Movie - 1080i", "1080p", "1080i")]
    // Bracketed version labels: the opening bracket is kept in the label.
    [InlineData(
        "Blade Runner (1982) [Final Cut] [1080p HEVC AAC]",
        "Blade Runner (1982) [EE by ADM] [480p HEVC AAC]",
        "[Final Cut] [1080p HEVC AAC]",
        "[EE by ADM] [480p HEVC AAC]")]
    public void GetMediaSourceName_CommonPrefix_Valid(string primaryName, string altName, string expectedPrimary, string expectedAlt)
    {
        var primaryPath = "/Shows/Demo/Season 01/" + primaryName + ".mkv";
        var altPath = "/Shows/Demo/Season 01/" + altName + ".mkv";
        var commonPrefix = BaseItem.GetCommonVersionPrefix([primaryName, altName]);

        var video = new Video()
        {
            Path = primaryPath
        };

        var videoAlt = new Video()
        {
            Path = altPath,
        };

        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>()))
                .Returns((string x) => MediaProtocol.File);
        var libraryManager = new Mock<ILibraryManager>();
        // No local alternate versions: these are linked (separate items), so the folder fallback is unavailable.
        libraryManager.Setup(x => x.GetLocalAlternateVersionIds(It.IsAny<Video>()))
                .Returns(Array.Empty<Guid>());
        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        BaseItem.LibraryManager = libraryManager.Object;

        Assert.Equal(expectedPrimary, video.GetMediaSourceName(video, commonPrefix));
        Assert.Equal(expectedAlt, videoAlt.GetMediaSourceName(videoAlt, commonPrefix));
    }
}
