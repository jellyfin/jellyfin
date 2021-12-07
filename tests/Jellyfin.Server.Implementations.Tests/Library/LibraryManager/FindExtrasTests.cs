using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Naming.Common;
using Emby.Server.Implementations.Library.Resolvers;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library.LibraryManager;

public class FindExtrasTests
{
    private readonly Emby.Server.Implementations.Library.LibraryManager _libraryManager;

    public FindExtrasTests()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Register(() => new NamingOptions());
        var configMock = fixture.Freeze<Mock<IServerConfigurationManager>>();
        configMock.Setup(c => c.ApplicationPaths.ProgramDataPath).Returns("/data");
        var fileSystemMock = fixture.Freeze<Mock<IFileSystem>>();
        fileSystemMock.Setup(f => f.GetFileInfo(It.IsAny<string>())).Returns<string>(path => new FileSystemMetadata { FullName = path });
        _libraryManager = fixture.Build<Emby.Server.Implementations.Library.LibraryManager>().Do(s => s.AddParts(
                fixture.Create<IEnumerable<IResolverIgnoreRule>>(),
                new List<IItemResolver> { new GenericVideoResolver<Video>(fixture.Create<NamingOptions>()) },
                fixture.Create<IEnumerable<IIntroProvider>>(),
                fixture.Create<IEnumerable<IBaseItemComparer>>(),
                fixture.Create<IEnumerable<ILibraryPostScanTask>>()))
            .Create();

        // This is pretty terrible but unavoidable
        BaseItem.FileSystem ??= fixture.Create<IFileSystem>();
        BaseItem.MediaSourceManager ??= fixture.Create<IMediaSourceManager>();
    }

    [Fact]
    public void FindExtras_SeparateMovieFolder_FindsCorrectExtras()
    {
        var owner = new Movie { Name = "Up", Path = "/movies/Up/Up.mkv" };
        var paths = new List<string>
        {
            "/movies/Up/Up.mkv",
            "/movies/Up/Up - trailer.mkv",
            "/movies/Up/Up - sample.mkv",
            "/movies/Up/Up something else.mkv"
        };

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            IsDirectory = false
        }).ToList();

        var extras = _libraryManager.FindExtras(owner, files).OrderBy(e => e.ExtraType).ToList();

        Assert.Equal(2, extras.Count);
        Assert.Equal(ExtraType.Trailer, extras[0].ExtraType);
        Assert.Equal(ExtraType.Sample, extras[1].ExtraType);
    }

    [Fact]
    public void FindExtras_SeparateMovieFolderWithMixedExtras_FindsCorrectExtras()
    {
        var owner = new Movie { Name = "Up", Path = "/movies/Up/Up.mkv" };
        var paths = new List<string>
        {
            "/movies/Up/Up.mkv",
            "/movies/Up/Up - trailer.mkv",
            "/movies/Up/trailers/some trailer.mkv",
            "/movies/Up/behind the scenes/the making of Up.mkv",
            "/movies/Up/behind the scenes.mkv",
            "/movies/Up/Up - sample.mkv",
            "/movies/Up/Up something else.mkv"
        };

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            IsDirectory = false
        }).ToList();

        var extras = _libraryManager.FindExtras(owner, files).OrderBy(e => e.ExtraType).ToList();

        Assert.Equal(4, extras.Count);
        Assert.Equal(ExtraType.Trailer, extras[0].ExtraType);
        Assert.Equal(ExtraType.Trailer, extras[1].ExtraType);
        Assert.Equal(ExtraType.BehindTheScenes, extras[2].ExtraType);
        Assert.Equal(ExtraType.Sample, extras[3].ExtraType);
    }

    [Fact]
    public void FindExtras_SeparateMovieFolderWithMixedExtras_FindsOnlyExtrasInMovieFolder()
    {
        var owner = new Movie { Name = "Up", Path = "/movies/Up/Up.mkv" };
        var paths = new List<string>
        {
            "/movies/Up/Up.mkv",
            "/movies/Up/trailer.mkv",
            "/movies/Another Movie/trailer.mkv"
        };

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            IsDirectory = false
        }).ToList();

        var extras = _libraryManager.FindExtras(owner, files).OrderBy(e => e.ExtraType).ToList();

        Assert.Single(extras);
        Assert.Equal(ExtraType.Trailer, extras[0].ExtraType);
        Assert.Equal("trailer", extras[0].FileNameWithoutExtension);
        Assert.Equal("/movies/Up/trailer.mkv", extras[0].Path);
    }

    [Fact]
    public void FindExtras_SeparateMovieFolderWithParts_FindsCorrectExtras()
    {
        var owner = new Movie { Name = "Up", Path = "/movies/Up/Up - part1.mkv" };
        var paths = new List<string>
        {
            "/movies/Up/Up - part1.mkv",
            "/movies/Up/Up - part2.mkv",
            "/movies/Up/trailer.mkv",
            "/movies/Another Movie/trailer.mkv"
        };

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            IsDirectory = false
        }).ToList();

        var extras = _libraryManager.FindExtras(owner, files).OrderBy(e => e.ExtraType).ToList();

        Assert.Single(extras);
        Assert.Equal(ExtraType.Trailer, extras[0].ExtraType);
        Assert.Equal("trailer", extras[0].FileNameWithoutExtension);
        Assert.Equal("/movies/Up/trailer.mkv", extras[0].Path);
    }

    [Fact]
    public void FindExtras_SeriesWithTrailers_FindsCorrectExtras()
    {
        var owner = new Series { Name = "Dexter", Path = "/series/Dexter" };
        var paths = new List<string>
        {
            "/series/Dexter/Season 1/S01E01.mkv",
            "/series/Dexter/trailer.mkv",
            "/series/Dexter/trailers/trailer2.mkv",
        };

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            IsDirectory = string.IsNullOrEmpty(Path.GetExtension(p))
        }).ToList();

        var extras = _libraryManager.FindExtras(owner, files).OrderBy(e => e.ExtraType).ToList();

        Assert.Equal(2, extras.Count);
        Assert.Equal(ExtraType.Trailer, extras[0].ExtraType);
        Assert.Equal("trailer", extras[0].FileNameWithoutExtension);
        Assert.Equal("/series/Dexter/trailer.mkv", extras[0].Path);
        Assert.Equal("/series/Dexter/trailers/trailer2.mkv", extras[1].Path);
    }
}
