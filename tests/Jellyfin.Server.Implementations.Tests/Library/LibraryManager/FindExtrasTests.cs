using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Naming.Common;
using Emby.Server.Implementations.Library.Resolvers.Audio;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
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
    private readonly Mock<IFileSystem> _fileSystemMock;

    public FindExtrasTests()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Register(() => new NamingOptions());
        var configMock = fixture.Freeze<Mock<IServerConfigurationManager>>();
        configMock.Setup(c => c.ApplicationPaths.ProgramDataPath).Returns("/data");
        var itemRepository = fixture.Freeze<Mock<IItemRepository>>();
        itemRepository.Setup(i => i.RetrieveItem(It.IsAny<Guid>())).Returns<BaseItem>(null);
        _fileSystemMock = fixture.Freeze<Mock<IFileSystem>>();
        _fileSystemMock.Setup(f => f.GetFileInfo(It.IsAny<string>())).Returns<string>(path => new FileSystemMetadata { FullName = path });
        _libraryManager = fixture.Build<Emby.Server.Implementations.Library.LibraryManager>().Do(s => s.AddParts(
                fixture.Create<IEnumerable<IResolverIgnoreRule>>(),
                new List<IItemResolver> { new AudioResolver(fixture.Create<NamingOptions>()) },
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
            "/movies/Up/Up something else.mkv",
            "/movies/Up/Up-extra.mkv"
        };

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            IsDirectory = false
        }).ToList();

        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object)).OrderBy(e => e.ExtraType).ToList();

        Assert.Equal(3, extras.Count);
        Assert.Equal(ExtraType.Unknown, extras[0].ExtraType);
        Assert.Equal(ExtraType.Trailer, extras[1].ExtraType);
        Assert.Equal(typeof(Trailer), extras[1].GetType());
        Assert.Equal(ExtraType.Sample, extras[2].ExtraType);
    }

    [Fact]
    public void FindExtras_SeparateMovieFolder_CleanExtraNames()
    {
        var owner = new Movie { Name = "Up", Path = "/movies/Up/Up.mkv" };
        var paths = new List<string>
        {
            "/movies/Up/Up.mkv",
            "/movies/Up/Recording the audio[Bluray]-behindthescenes.mkv",
            "/movies/Up/Interview with the dog-interview.mkv",
            "/movies/Up/shorts/Balloons[1080p].mkv"
        };

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            IsDirectory = false
        }).ToList();

        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object)).OrderBy(e => e.ExtraType).ToList();

        Assert.Equal(3, extras.Count);
        Assert.Equal(ExtraType.BehindTheScenes, extras[0].ExtraType);
        Assert.Equal("Recording the audio", extras[0].Name);
        Assert.Equal(ExtraType.Interview, extras[1].ExtraType);
        Assert.Equal("Interview with the dog", extras[1].Name);
        Assert.Equal(ExtraType.Short, extras[2].ExtraType);
        Assert.Equal("Balloons", extras[2].Name);
    }

    [Fact]
    public void FindExtras_SeparateMovieFolderWithMixedExtras_FindsCorrectExtras()
    {
        var owner = new Movie { Name = "Up", Path = "/movies/Up/Up.mkv" };
        var paths = new List<string>
        {
            "/movies/Up/Up.mkv",
            "/movies/Up/Up - trailer.mkv",
            "/movies/Up/trailers",
            "/movies/Up/theme-music",
            "/movies/Up/theme.mp3",
            "/movies/Up/not a theme.mp3",
            "/movies/Up/behind the scenes",
            "/movies/Up/behind the scenes.mkv",
            "/movies/Up/Up - sample.mkv",
            "/movies/Up/Up something else.mkv",
            "/movies/Up/extras"
        };

        _fileSystemMock.Setup(f => f.GetFiles(
                "/movies/Up/trailers",
                It.IsAny<string[]>(),
                false,
                false))
            .Returns(new List<FileSystemMetadata>
            {
                new()
                {
                    FullName = "/movies/Up/trailers/some trailer.mkv",
                    Name = "some trailer.mkv",
                    IsDirectory = false
                }
            }).Verifiable();

        _fileSystemMock.Setup(f => f.GetFiles(
                "/movies/Up/behind the scenes",
                It.IsAny<string[]>(),
                false,
                false))
            .Returns(new List<FileSystemMetadata>
            {
                new()
                {
                    FullName = "/movies/Up/behind the scenes/the making of Up.mkv",
                    Name = "the making of Up.mkv",
                    IsDirectory = false
                }
            }).Verifiable();

        _fileSystemMock.Setup(f => f.GetFiles(
                "/movies/Up/theme-music",
                It.IsAny<string[]>(),
                false,
                false))
            .Returns(new List<FileSystemMetadata>
            {
                new()
                {
                    FullName = "/movies/Up/theme-music/theme2.mp3",
                    Name = "theme2.mp3",
                    IsDirectory = false
                }
            }).Verifiable();

        _fileSystemMock.Setup(f => f.GetFiles(
                "/movies/Up/extras",
                It.IsAny<string[]>(),
                false,
                false))
            .Returns(new List<FileSystemMetadata>
            {
                new()
                {
                    FullName = "/movies/Up/extras/Honest Trailer.mkv",
                    Name = "Honest Trailer.mkv",
                    IsDirectory = false
                }
            }).Verifiable();

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            Name = Path.GetFileName(p),
            IsDirectory = !Path.HasExtension(p)
        }).ToList();

        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object)).OrderBy(e => e.ExtraType).ToList();

        _fileSystemMock.Verify();
        Assert.Equal(7, extras.Count);
        Assert.Equal(ExtraType.Unknown, extras[0].ExtraType);
        Assert.Equal(typeof(Video), extras[0].GetType());
        Assert.Equal(ExtraType.Trailer, extras[1].ExtraType);
        Assert.Equal(typeof(Trailer), extras[1].GetType());
        Assert.Equal(ExtraType.Trailer, extras[2].ExtraType);
        Assert.Equal(typeof(Trailer), extras[2].GetType());
        Assert.Equal(ExtraType.BehindTheScenes, extras[3].ExtraType);
        Assert.Equal(ExtraType.Sample, extras[4].ExtraType);
        Assert.Equal(ExtraType.ThemeSong, extras[5].ExtraType);
        Assert.Equal(typeof(Audio), extras[5].GetType());
        Assert.Equal(ExtraType.ThemeSong, extras[6].ExtraType);
        Assert.Equal(typeof(Audio), extras[6].GetType());
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

        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object)).OrderBy(e => e.ExtraType).ToList();

        Assert.Single(extras);
        Assert.Equal(ExtraType.Trailer, extras[0].ExtraType);
        Assert.Equal(typeof(Trailer), extras[0].GetType());
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

        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object)).OrderBy(e => e.ExtraType).ToList();

        Assert.Single(extras);
        Assert.Equal(ExtraType.Trailer, extras[0].ExtraType);
        Assert.Equal(typeof(Trailer), extras[0].GetType());
        Assert.Equal("trailer", extras[0].FileNameWithoutExtension);
        Assert.Equal("/movies/Up/trailer.mkv", extras[0].Path);
    }

    [Fact]
    public void FindExtras_WrongExtensions_FindsNoExtras()
    {
        var owner = new Movie { Name = "Up", Path = "/movies/Up/Up.mkv" };
        var paths = new List<string>
        {
            "/movies/Up/Up.mkv",
            "/movies/Up/trailer.noext",
            "/movies/Up/theme.png",
            "/movies/Up/trailers"
        };

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            Name = Path.GetFileName(p),
            IsDirectory = !Path.HasExtension(p)
        }).ToList();

        _fileSystemMock.Setup(f => f.GetFiles(
                "/movies/Up/trailers",
                It.IsAny<string[]>(),
                false,
                false))
            .Returns(new List<FileSystemMetadata>
            {
                new()
                {
                    FullName = "/movies/Up/trailers/trailer.jpg",
                    Name = "trailer.jpg",
                    IsDirectory = false
                }
            }).Verifiable();

        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object)).OrderBy(e => e.ExtraType).ToList();

        _fileSystemMock.Verify();
        Assert.Empty(extras);
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

        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object)).OrderBy(e => e.ExtraType).ToList();

        Assert.Equal(2, extras.Count);
        Assert.Equal(ExtraType.Trailer, extras[0].ExtraType);
        Assert.Equal(typeof(Trailer), extras[0].GetType());
        Assert.Equal("trailer", extras[0].FileNameWithoutExtension);
        Assert.Equal("/series/Dexter/trailer.mkv", extras[0].Path);
        Assert.Equal("/series/Dexter/trailers/trailer2.mkv", extras[1].Path);
    }
}
