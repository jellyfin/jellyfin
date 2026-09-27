using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
using MediaBrowser.Model.Globalization;
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

        var strings = LoadCoreStrings();
        fixture.Freeze<Mock<ILocalizationManager>>()
            .Setup(l => l.GetServerLocalizedString(It.IsAny<string>()))
            .Returns<string>(key => strings.TryGetValue(key, out var value) ? value : key);

        _libraryManager = fixture.Build<Emby.Server.Implementations.Library.LibraryManager>().Do(s => s.AddParts(
                fixture.Create<IEnumerable<IResolverIgnoreRule>>(),
                [new AudioResolver(fixture.Create<NamingOptions>())],
                fixture.Create<IEnumerable<IIntroProvider>>(),
                fixture.Create<IEnumerable<IBaseItemComparer>>(),
                fixture.Create<IEnumerable<ILibraryPostScanTask>>()))
            .Create();

        // This is pretty terrible but unavoidable
        BaseItem.FileSystem ??= fixture.Create<IFileSystem>();
        BaseItem.MediaSourceManager ??= fixture.Create<IMediaSourceManager>();
    }

    private static Dictionary<string, string> LoadCoreStrings()
    {
        using var stream = typeof(Emby.Server.Implementations.Library.LibraryManager).Assembly
            .GetManifestResourceStream("Emby.Server.Implementations.Localization.Core.en-US.json")
            ?? throw new InvalidOperationException("Core localization resource is missing");

        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
            ?? throw new InvalidOperationException("Core localization resource is empty");
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
            .Returns(
            [
                new()
                {
                    FullName = "/movies/Up/trailers/some trailer.mkv",
                    Name = "some trailer.mkv",
                    IsDirectory = false
                }
            ]).Verifiable();

        _fileSystemMock.Setup(f => f.GetFiles(
                "/movies/Up/behind the scenes",
                It.IsAny<string[]>(),
                false,
                false))
            .Returns(
            [
                new()
                {
                    FullName = "/movies/Up/behind the scenes/the making of Up.mkv",
                    Name = "the making of Up.mkv",
                    IsDirectory = false
                }
            ]).Verifiable();

        _fileSystemMock.Setup(f => f.GetFiles(
                "/movies/Up/theme-music",
                It.IsAny<string[]>(),
                false,
                false))
            .Returns(
            [
                new()
                {
                    FullName = "/movies/Up/theme-music/theme2.mp3",
                    Name = "theme2.mp3",
                    IsDirectory = false
                }
            ]).Verifiable();

        _fileSystemMock.Setup(f => f.GetFiles(
                "/movies/Up/extras",
                It.IsAny<string[]>(),
                false,
                false))
            .Returns(
            [
                new()
                {
                    FullName = "/movies/Up/extras/Honest Trailer.mkv",
                    Name = "Honest Trailer.mkv",
                    IsDirectory = false
                }
            ]).Verifiable();

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
            .Returns(
            [
                new()
                {
                    FullName = "/movies/Up/trailers/trailer.jpg",
                    Name = "trailer.jpg",
                    IsDirectory = false
                }
            ]).Verifiable();

        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object)).OrderBy(e => e.ExtraType).ToList();

        _fileSystemMock.Verify();
        Assert.Empty(extras);
    }

    [Fact]
    public void FindExtras_TrailerWithYearInFilename_SetsProductionYearFromFilename()
    {
        var owner = new Movie { Name = "Up", Path = "/movies/Up/Up.mkv" };
        var paths = new List<string>
        {
            "/movies/Up/Up.mkv",
            "/movies/Up/trailers"
        };

        _fileSystemMock.Setup(f => f.GetFiles(
                "/movies/Up/trailers",
                It.IsAny<string[]>(),
                false,
                false))
            .Returns(
            [
                new()
                {
                    FullName = "/movies/Up/trailers/Trailer 1 (2013).mkv",
                    Name = "Trailer 1 (2013).mkv",
                    IsDirectory = false
                }
            ]).Verifiable();

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            Name = Path.GetFileName(p),
            IsDirectory = !Path.HasExtension(p)
        }).ToList();

        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object)).ToList();

        _fileSystemMock.Verify();
        var trailer = Assert.Single(extras);
        Assert.Equal(ExtraType.Trailer, trailer.ExtraType);
        Assert.Equal(typeof(Trailer), trailer.GetType());
        Assert.Equal(2013, trailer.ProductionYear);
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
    public void FindExtras_SameExtraInSeveralContainers_ReturnsEach()
    {
        var owner = new Movie { Name = "Skyscraper", Path = "/movies/Skyscraper (2018)/Skyscraper (2018) - [1080p HEVC].mkv" };
        var paths = new List<string>
        {
            "/movies/Skyscraper (2018)/Skyscraper (2018) - [1080p HEVC].mkv",
            "/movies/Skyscraper (2018)/Skyscraper (2018) - [1080p HEVC]-trailer.mkv",
            "/movies/Skyscraper (2018)/Skyscraper (2018) - [1080p HEVC]-trailer.mp4",
            "/movies/Skyscraper (2018)/Skyscraper (2018) - [1080p HEVC]-behindthescenes.mkv",
            "/movies/Skyscraper (2018)/Skyscraper (2018) - [1080p HEVC]-behindthescenes.mp4"
        };

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            IsDirectory = false
        }).ToList();

        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object))
            .ToDictionary(e => e.Path, e => e.Name, StringComparer.Ordinal);

        // A container is a separate file that plays on its own, so it is a separate extra
        Assert.Equal(4, extras.Count);
        Assert.Equal("Behind The Scenes", extras["/movies/Skyscraper (2018)/Skyscraper (2018) - [1080p HEVC]-behindthescenes.mkv"]);
        Assert.Equal("Behind The Scenes 2", extras["/movies/Skyscraper (2018)/Skyscraper (2018) - [1080p HEVC]-behindthescenes.mp4"]);
        Assert.Equal("Trailer", extras["/movies/Skyscraper (2018)/Skyscraper (2018) - [1080p HEVC]-trailer.mkv"]);
        Assert.Equal("Trailer 2", extras["/movies/Skyscraper (2018)/Skyscraper (2018) - [1080p HEVC]-trailer.mp4"]);
    }

    [Fact]
    public void FindExtras_SameExtraInSeveralResolutions_ReturnsEach()
    {
        var owner = new Movie { Name = "Dragon 2", Path = "/movies/Dragon 2 (2014)/Dragon 2 (2014) - [2160p].mkv" };
        var paths = new List<string>
        {
            "/movies/Dragon 2 (2014)/Dragon 2 (2014) - [2160p].mkv",
            "/movies/Dragon 2 (2014)/Dragon 2 (2014) - [1080p]-trailer.mkv",
            "/movies/Dragon 2 (2014)/Dragon 2 (2014) - [2160p]-trailer.mkv"
        };

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            IsDirectory = false
        }).ToList();

        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object))
            .ToDictionary(e => e.Path, e => e.Name, StringComparer.Ordinal);

        Assert.Equal(2, extras.Count);
        Assert.Equal("Trailer", extras["/movies/Dragon 2 (2014)/Dragon 2 (2014) - [1080p]-trailer.mkv"]);
        Assert.Equal("Trailer 2", extras["/movies/Dragon 2 (2014)/Dragon 2 (2014) - [2160p]-trailer.mkv"]);
    }

    [Fact]
    public void FindExtras_NumberedExtras_AreKeptApart()
    {
        var owner = new Movie { Name = "Up", Path = "/movies/Up (2009)/Up (2009).mkv" };
        var paths = new List<string>
        {
            "/movies/Up (2009)/Up (2009).mkv",
            "/movies/Up (2009)/Up (2009)-trailer.mkv",
            "/movies/Up (2009)/Up (2009)-trailer2.mkv",
            "/movies/Up (2009)/Up (2009)-trailer2.mp4",
            "/movies/Up (2009)/Up (2009)-trailer3.mkv"
        };

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            IsDirectory = false
        }).ToList();

        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object)).OrderBy(e => e.Path, StringComparer.Ordinal).ToList();

        Assert.Equal(4, extras.Count);
        Assert.Equal("/movies/Up (2009)/Up (2009)-trailer.mkv", extras[0].Path);
        Assert.Equal("/movies/Up (2009)/Up (2009)-trailer2.mkv", extras[1].Path);
        Assert.Equal("/movies/Up (2009)/Up (2009)-trailer2.mp4", extras[2].Path);
        Assert.Equal("/movies/Up (2009)/Up (2009)-trailer3.mkv", extras[3].Path);

        // The index in the file name is not the number the extra is given, which counts the
        // extras of a type as they are found
        Assert.Equal("Trailer", extras[0].Name);
        Assert.Equal("Trailer 2", extras[1].Name);
        Assert.Equal("Trailer 3", extras[2].Name);
        Assert.Equal("Trailer 4", extras[3].Name);
    }

    [Fact]
    public void FindExtras_ExtraWithOwnTitleBesideOwner_KeepsTitle()
    {
        var owner = new Movie { Name = "Up", Path = "/movies/Up (2009)/Up (2009).mkv" };
        var paths = new List<string>
        {
            "/movies/Up (2009)/Up (2009).mkv",
            "/movies/Up (2009)/Up (2009)-trailer.mkv",
            "/movies/Up (2009)/Recording the audio-behindthescenes.mkv",
            "/movies/Up (2009)/Up (2009)-behindthescenes.mkv"
        };

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            IsDirectory = false
        }).ToList();

        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object))
            .ToDictionary(e => e.Path, e => e.Name, StringComparer.Ordinal);

        Assert.Equal(3, extras.Count);
        Assert.Equal("Trailer", extras["/movies/Up (2009)/Up (2009)-trailer.mkv"]);

        // A descriptive file name is a real title and survives, and does not consume a number
        Assert.Equal("Recording the audio", extras["/movies/Up (2009)/Recording the audio-behindthescenes.mkv"]);
        Assert.Equal("Behind The Scenes", extras["/movies/Up (2009)/Up (2009)-behindthescenes.mkv"]);
    }

    [Fact]
    public void FindExtras_ExtraInOwnFolder_IsNamedAfterItsFile()
    {
        var owner = new Movie { Name = "Up", Path = "/movies/Up/Up.mkv" };
        var paths = new List<string>
        {
            "/movies/Up/Up.mkv",
            "/movies/Up/trailers"
        };

        _fileSystemMock.Setup(f => f.GetFiles(
                "/movies/Up/trailers",
                It.IsAny<string[]>(),
                false,
                false))
            .Returns(
            [
                new() { FullName = "/movies/Up/trailers/Teaser.mkv", Name = "Teaser.mkv", IsDirectory = false },
                new() { FullName = "/movies/Up/trailers/Comic-Con Reel.mkv", Name = "Comic-Con Reel.mkv", IsDirectory = false }
            ]).Verifiable();

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            Name = Path.GetFileName(p),
            IsDirectory = !Path.HasExtension(p)
        }).ToList();

        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object))
            .ToDictionary(e => e.Path, e => e.Name, StringComparer.Ordinal);

        _fileSystemMock.Verify();
        Assert.Equal(2, extras.Count);
        Assert.Equal("Teaser", extras["/movies/Up/trailers/Teaser.mkv"]);
        Assert.Equal("Comic-Con Reel", extras["/movies/Up/trailers/Comic-Con Reel.mkv"]);
    }

    [Fact]
    public void FindExtras_DistinctExtrasInSameFolder_AreKeptApart()
    {
        var owner = new Movie { Name = "Up", Path = "/movies/Up/Up.mkv" };
        var paths = new List<string>
        {
            "/movies/Up/Up.mkv",
            "/movies/Up/trailers"
        };

        _fileSystemMock.Setup(f => f.GetFiles(
                "/movies/Up/trailers",
                It.IsAny<string[]>(),
                false,
                false))
            .Returns(
            [
                new() { FullName = "/movies/Up/trailers/Teaser.mkv", Name = "Teaser.mkv", IsDirectory = false },
                new() { FullName = "/movies/Up/trailers/Official.mkv", Name = "Official.mkv", IsDirectory = false },
                new() { FullName = "/movies/Up/trailers/Official.mp4", Name = "Official.mp4", IsDirectory = false }
            ]).Verifiable();

        var files = paths.Select(p => new FileSystemMetadata
        {
            FullName = p,
            Name = Path.GetFileName(p),
            IsDirectory = !Path.HasExtension(p)
        }).ToList();

        var extras = _libraryManager.FindExtras(owner, files, new DirectoryService(_fileSystemMock.Object)).OrderBy(e => e.Path, StringComparer.Ordinal).ToList();

        _fileSystemMock.Verify();
        Assert.Equal(3, extras.Count);
        Assert.Equal("/movies/Up/trailers/Official.mkv", extras[0].Path);
        Assert.Equal("/movies/Up/trailers/Official.mp4", extras[1].Path);
        Assert.Equal("/movies/Up/trailers/Teaser.mkv", extras[2].Path);
    }
}
