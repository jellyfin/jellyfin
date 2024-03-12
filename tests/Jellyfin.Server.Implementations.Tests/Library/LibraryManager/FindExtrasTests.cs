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

    [Fact]
    public void FindExtras_SeriesWithExtras_FindsCorrectExtras()
    {
        var owner = new Series { Name = "Dexter", Path = "/series/Dexter" };
        var paths = new List<string>
        {
            "/series/Dexter/Season 1/Dexter - S01E01.mkv",
            "/series/Dexter/Season 1/Dexter - S01E01-deleted.mkv",
            "/series/Dexter/Season 1/Dexter - S01E01 [WEBDL-1080p AVC][AAC 2.0][YouTube]-deleted.mkv",
            "/series/Dexter/Season 1/Dexter - S01E02 - Second Epi.mkv",
            "/series/Dexter/Season 1/Dexter - S01E02 - Second Epi-interview.mkv",
            "/series/Dexter/Season 1/Dexter - S01E02 - Second Epi-scene.mkv",
            "/series/Dexter/Season 1/It's a begining-behindthescenes.mkv",
            "/series/Dexter/Season 1/interviews/The Cast.mkv",
            "/series/Dexter/Funny-behindthescenes.mkv",
            "/series/Dexter/interviews/The Director.mkv",
            "/series/Dexter/Dexter - S02E05.mkv",
            "/series/Dexter/Dexter - S02E05-clip.mkv",
            "/series/Dexter/Dexter - S03E05/Dexter - S03E05 - Fifth.mkv",
            "/series/Dexter/Dexter - S03E05/Dexter - S03E05 - Fifth-featurette.mkv",
        };

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            Name = Path.GetFileName(p),
            Extension = Path.GetExtension(p),
            IsDirectory = string.IsNullOrEmpty(Path.GetExtension(p))
        }).ToList();

        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object)).OrderBy(e => e.ExtraType).ToList();

        Assert.Equal(2, extras.Count);
        Assert.Equal(ExtraType.BehindTheScenes, extras[0].ExtraType);
        Assert.Equal(typeof(Video), extras[0].GetType());
        Assert.Equal("Funny-behindthescenes", extras[0].FileNameWithoutExtension);
        Assert.Equal("/series/Dexter/Funny-behindthescenes.mkv", extras[0].Path);
        Assert.Equal("/series/Dexter/interviews/The Director.mkv", extras[1].Path);
    }

    [Fact]
    public void FindExtras_SeasonWithExtras_FindsCorrectExtras()
    {
        var owner = new Season { Name = "Season 1", SeriesName = "Dexter", Path = "/series/Dexter/Season 1" };
        var paths = new List<string>
        {
            "/series/Dexter/Season 1/Dexter 1x01 [Bluray-1080p x264][AC3 5.1][-reward] - Northwest Passage.mkv",
            "/series/Dexter/Season 1/Dexter 1x01-deleted.mkv",
            "/series/Dexter/Season 1/Dexter 1x01 [WEBDL-1080p AVC][AAC 2.0][YouTube]-deleted.mkv",
            "/series/Dexter/Season 1/Dexter 1x01 [WEBDL-1080p AVC][AAC 2.0][YouTube][-MrC] - Log Lady Introduction 1-extra.mkv",
            "/series/Dexter/Season 1/Dexter - S01E02 - Second Epi.mkv",
            "/series/Dexter/Season 1/Dexter - S01E02 - Second Epi-interview.mkv",
            "/series/Dexter/Season 1/Dexter - S01E02 - Second Epi-scene.mkv",
            "/series/Dexter/Season 1/It's a begining-behindthescenes.mkv",
            "/series/Dexter/Season 1/interviews/The Cast.mkv",
            "/series/Dexter/Funny-behindthescenes.mkv",
            "/series/Dexter/interviews/The Director.mkv",
            "/series/Dexter/Dexter - S02E05.mkv",
            "/series/Dexter/Dexter - S02E05-clip.mkv",
            "/series/Dexter/Dexter - S03E05/Dexter - S03E05 - Fifth.mkv",
            "/series/Dexter/Dexter - S03E05/Dexter - S03E05 - Fifth-featurette.mkv",
        };

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            Name = Path.GetFileName(p),
            Extension = Path.GetExtension(p),
            IsDirectory = string.IsNullOrEmpty(Path.GetExtension(p))
        }).ToList();

        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object)).OrderBy(e => e.ExtraType).ToList();

        Assert.Equal(2, extras.Count);
        Assert.Equal(ExtraType.BehindTheScenes, extras[0].ExtraType);
        Assert.Equal(typeof(Video), extras[0].GetType());
        Assert.Equal("It's a begining-behindthescenes", extras[0].FileNameWithoutExtension);
        Assert.Equal("/series/Dexter/Season 1/It's a begining-behindthescenes.mkv", extras[0].Path);
        Assert.Equal("/series/Dexter/Season 1/interviews/The Cast.mkv", extras[1].Path);
    }

    [Fact]
    public void FindExtras_SeasonWithExtras_FindsCorrectExtras2()
    {
        // Series name directory has special characters stripped that episodes do not
        var owner = new Season { Name = "Season 1", SeriesName = "The Venture Bros.", Path = "/series/The Venture Bros/Season 1" };
        var paths = new List<string>
        {
            "/series/The Venture Bros/Season 1/The Venture Bros. S01E01.mkv",
            "/series/The Venture Bros/Season 1/The Venture Bros. S01E01-deleted.mkv",
            "/series/The Venture Bros/Season 1/The Venture Bros. - S01E02 - Second Epi.mkv",
            "/series/The Venture Bros/Season 1/The Venture Bros. - S01E02 - Second Epi-interview.mkv",
            "/series/The Venture Bros/Season 1/The Venture Bros. - S01E02 - Second Epi-scene.mkv",
            "/series/The Venture Bros/Season 1/It's a begining-behindthescenes.mkv",
            "/series/The Venture Bros/Season 1/interviews/The Cast.mkv",
        };

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            Name = Path.GetFileName(p),
            Extension = Path.GetExtension(p),
            IsDirectory = string.IsNullOrEmpty(Path.GetExtension(p))
        }).ToList();

        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object)).OrderBy(e => e.ExtraType).ToList();

        Assert.Equal(2, extras.Count);
        Assert.Equal(ExtraType.BehindTheScenes, extras[0].ExtraType);
        Assert.Equal(typeof(Video), extras[0].GetType());
        Assert.Equal("It's a begining-behindthescenes", extras[0].FileNameWithoutExtension);
        Assert.Equal("/series/The Venture Bros/Season 1/It's a begining-behindthescenes.mkv", extras[0].Path);
        Assert.Equal("/series/The Venture Bros/Season 1/interviews/The Cast.mkv", extras[1].Path);
    }

    [Fact]
    public void FindExtras_EpisodeWithExtras_FindsCorrectExtras()
    {
        var paths = new List<string>
        {
            "/series/Dexter/Season 1/Dexter - S01E01.mkv",
            "/series/Dexter/Season 1/Dexter - S01E01-deleted.mkv",
            "/series/Dexter/Season 1/Dexter - S01E02 - Second Epi.mkv",
            "/series/Dexter/Season 1/Dexter - S01E02 - Second Epi-interview.mkv",
            "/series/Dexter/Season 1/Dexter - S01E02 - Second Epi-scene.mkv",
            "/series/Dexter/Season 1/It's a begining-behindthescenes.mkv",
            "/series/Dexter/Season 1/interviews/The Cast.mkv",
            "/series/Dexter/Funny-behindthescenes.mkv",
            "/series/Dexter/interviews/The Director.mkv",
            "/series/Dexter/Dexter - S02E05.mkv",
            "/series/Dexter/Dexter - S02E05-clip.mkv",
            "/series/Dexter/Dexter - S03E05/Dexter - S03E05 - Fifth.mkv",
            "/series/Dexter/Dexter - S03E05/Dexter - S03E05 - Fifth-featurette.mkv",
            "/series/Dexter/Dexter - S03E05/Dexter - S03E05 - Fifth-featurette2.mkv",
            "/series/Dexter/Dexter - S03E05/Deleted Scenes/Meet Friends.mkv",
        };

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            Name = Path.GetFileName(p),
            Extension = Path.GetExtension(p),
            IsDirectory = string.IsNullOrEmpty(Path.GetExtension(p))
        }).ToList();

        var owner = new Episode { Name = "Dexter - S01E01", Path = "/series/Dexter/Season 1/Dexter - S01E01.mkv", IsInMixedFolder = true };
        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object)).OrderBy(e => e.ExtraType).ToList();

        Assert.Single(extras);
        Assert.Equal(ExtraType.DeletedScene, extras[0].ExtraType);
        Assert.Equal(typeof(Video), extras[0].GetType());
        Assert.Equal("/series/Dexter/Season 1/Dexter - S01E01-deleted.mkv", extras[0].Path);

        owner = new Episode { Name = "Dexter - S01E02 - Second Epi", Path = "/series/Dexter/Season 1/Dexter - S01E02 - Second Epi.mkv", IsInMixedFolder = true };
        extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object)).OrderBy(e => e.ExtraType).ToList();

        Assert.Equal(2, extras.Count);
        Assert.Equal(ExtraType.Interview, extras[0].ExtraType);
        Assert.Equal(ExtraType.Scene, extras[1].ExtraType);
        Assert.Equal(typeof(Video), extras[0].GetType());
        Assert.Equal("/series/Dexter/Season 1/Dexter - S01E02 - Second Epi-interview.mkv", extras[0].Path);
        Assert.Equal("/series/Dexter/Season 1/Dexter - S01E02 - Second Epi-scene.mkv", extras[1].Path);

        owner = new Episode { Name = "Dexter - S02E05", Path = "/series/Dexter/Dexter - S02E05.mkv", IsInMixedFolder = true };
        extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object)).OrderBy(e => e.ExtraType).ToList();

        Assert.Single(extras);
        Assert.Equal(ExtraType.Clip, extras[0].ExtraType);
        Assert.Equal(typeof(Video), extras[0].GetType());
        Assert.Equal("/series/Dexter/Dexter - S02E05-clip.mkv", extras[0].Path);

        // episode folder with special feature subfolders are not supported yet, but it should be considered as not mixed, but current is marked as mixed
        Folder folderOwner = new Folder { Name = "Dexter - S03E05", Path = "/series/Dexter/Dexter - S03E05", IsInMixedFolder = false };
        extras = _libraryManager.FindExtras(folderOwner, files, new DirectoryService(_fileSystemMock.Object)).OrderBy(e => e.ExtraType).ToList();

        Assert.Equal(3, extras.Count);
        // directory type extras are found before suffix type
        Assert.Equal(ExtraType.DeletedScene, extras[0].ExtraType);
        Assert.Equal("/series/Dexter/Dexter - S03E05/Deleted Scenes/Meet Friends.mkv", extras[0].Path);
        Assert.Equal(ExtraType.Featurette, extras[1].ExtraType);
        Assert.Equal(typeof(Video), extras[1].GetType());
        Assert.Equal("/series/Dexter/Dexter - S03E05/Dexter - S03E05 - Fifth-featurette.mkv", extras[1].Path);
        Assert.Equal("/series/Dexter/Dexter - S03E05/Dexter - S03E05 - Fifth-featurette2.mkv", extras[2].Path);
    }

    [Fact]
    public void FindExtras_EpisodeWithExtras_CleanNameTest()
    {
        var paths = new List<string>
        {
            "/series/Dexter/Season 1/Dexter - S01E01[Bluray-1080p x264][AC3 5.1].mkv",
            "/series/Dexter/Season 1/Dexter - S01E01 [WEBDL-1080p AVC][AAC 2.0][YouTube]-deleted.mkv",
            "/series/Dexter/Season 1/Dexter - S01E01[Bluray-1080p x264][AC3 5.1] - Some crazy deleted scene -deleted.mkv"
        };

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            Name = Path.GetFileName(p),
            Extension = Path.GetExtension(p),
            IsDirectory = string.IsNullOrEmpty(Path.GetExtension(p))
        }).ToList();

        var owner = new Episode { Name = "Dexter - S01E01", Path = "/series/Dexter/Season 1/Dexter - S01E01[Bluray-1080p x264][AC3 5.1].mkv", IsInMixedFolder = true };
        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object)).OrderBy(e => e.ExtraType).ToList();

        Assert.Equal(2, extras.Count);
        Assert.Equal(ExtraType.DeletedScene, extras[0].ExtraType);
        Assert.Equal(typeof(Video), extras[0].GetType());
        Assert.Equal("/series/Dexter/Season 1/Dexter - S01E01 [WEBDL-1080p AVC][AAC 2.0][YouTube]-deleted.mkv", extras[0].Path);
    }
}
