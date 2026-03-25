using System;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

public class VideoLinkedAlternateTests
{
    private readonly Mock<ILibraryManager> _libraryManager;
    private readonly Mock<IFileSystem> _fileSystem;

    public VideoLinkedAlternateTests()
    {
        _libraryManager = new Mock<ILibraryManager>();
        _fileSystem = new Mock<IFileSystem>();
        _fileSystem.Setup(x => x.MakeAbsolutePath(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string folder, string file) => file);
        BaseItem.LibraryManager = _libraryManager.Object;
        BaseItem.FileSystem = _fileSystem.Object;
        BaseItem.Logger = Mock.Of<ILogger<BaseItem>>();
    }

    [Fact]
    public void GetLinkedAlternateVersions_ReturnsValidVideos()
    {
        var alt1 = new Video { Id = Guid.NewGuid(), ForcedSortName = "Alt1", Path = "/movies/test/test_4k.mkv" };
        var alt2 = new Video { Id = Guid.NewGuid(), ForcedSortName = "Alt2", Path = "/movies/test/test_1080p.mkv" };

        _libraryManager.Setup(x => x.GetItemById(alt1.Id)).Returns(alt1);
        _libraryManager.Setup(x => x.GetItemById(alt2.Id)).Returns(alt2);

        var primary = new Video
        {
            Id = Guid.NewGuid(),
            Path = "/movies/test/test.mkv",
            LinkedAlternateVersions =
            [
                new LinkedChild { Path = alt1.Path, ItemId = alt1.Id },
                new LinkedChild { Path = alt2.Path, ItemId = alt2.Id }
            ]
        };

        var result = primary.GetLinkedAlternateVersions().ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(alt1, result);
        Assert.Contains(alt2, result);
    }

    [Fact]
    public void GetLinkedAlternateVersions_RemovesBrokenLinks()
    {
        var validAlt = new Video { Id = Guid.NewGuid(), ForcedSortName = "Valid", Path = "/movies/test/test_4k.mkv" };
        var deadId = Guid.NewGuid();

        _libraryManager.Setup(x => x.GetItemById(validAlt.Id)).Returns(validAlt);
        _libraryManager.Setup(x => x.GetItemById(deadId)).Returns((BaseItem?)null);
        _libraryManager.Setup(x => x.FindByPath(It.IsAny<string>(), null)).Returns((BaseItem?)null);

        var primary = new Video
        {
            Id = Guid.NewGuid(),
            Path = "/movies/test/test.mkv",
            LinkedAlternateVersions =
            [
                new LinkedChild { Path = validAlt.Path, ItemId = validAlt.Id },
                new LinkedChild { Path = "/movies/test/deleted.mkv", ItemId = deadId }
            ]
        };

        var result = primary.GetLinkedAlternateVersions().ToList();

        Assert.Single(result);
        Assert.Equal(validAlt.Id, result[0].Id);
        // Broken link should be cleaned from the array
        Assert.Single(primary.LinkedAlternateVersions);
    }

    [Fact]
    public void GetLinkedAlternateVersions_EmptyArray_ReturnsEmpty()
    {
        var primary = new Video
        {
            Id = Guid.NewGuid(),
            Path = "/movies/test/test.mkv",
            LinkedAlternateVersions = Array.Empty<LinkedChild>()
        };

        var result = primary.GetLinkedAlternateVersions().ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void GetLinkedChild_InvalidatesStaleCache_AndReresolvesViaPath()
    {
        var staleId = Guid.NewGuid();
        var freshVideo = new Video { Id = Guid.NewGuid(), ForcedSortName = "Fresh", Path = "/movies/test/found.mkv" };

        // Stale ID returns null (item was deleted)
        _libraryManager.Setup(x => x.GetItemById(staleId)).Returns((BaseItem?)null);
        // FindByPath fallback finds the replacement item
        _libraryManager.Setup(x => x.FindByPath("/movies/test/found.mkv", null)).Returns(freshVideo);
        // After re-caching, the fresh ID should resolve
        _libraryManager.Setup(x => x.GetItemById(freshVideo.Id)).Returns(freshVideo);

        var primary = new Video
        {
            Id = Guid.NewGuid(),
            Path = "/movies/test/test.mkv",
            LinkedAlternateVersions =
            [
                new LinkedChild { Path = "/movies/test/found.mkv", ItemId = staleId }
            ]
        };

        var result = primary.GetLinkedAlternateVersions().ToList();

        Assert.Single(result);
        Assert.Equal(freshVideo.Id, result[0].Id);
        // Cache should be updated
        Assert.Equal(freshVideo.Id, primary.LinkedAlternateVersions[0].ItemId);
    }
}
