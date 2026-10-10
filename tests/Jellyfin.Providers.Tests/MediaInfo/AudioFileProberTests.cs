using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Providers.MediaInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

public sealed class AudioFileProberTests : IDisposable
{
    private const string TestFileName = "Test Data/Audio/silence.mp3";

    private readonly string _workingDirectory;
    private readonly AudioFileProber _audioFileProber;

    public AudioFileProberTests()
    {
        _workingDirectory = Path.Combine(Path.GetTempPath(), "jellyfin-audio-prober-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(_workingDirectory);

        var mediaEncoder = new Mock<IMediaEncoder>();
        mediaEncoder.Setup(m => m.GetMediaInfo(It.IsAny<MediaInfoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaBrowser.Model.MediaInfo.MediaInfo { MediaStreams = [] });

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(m => m.GetLibraryOptions(It.IsAny<BaseItem>()))
            .Returns(new LibraryOptions());

        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(m => m.GetPathProtocol(It.IsAny<string>()))
            .Returns(MediaProtocol.File);

        var applicationPaths = new Mock<IServerApplicationPaths>();
        applicationPaths.Setup(m => m.InternalMetadataPath).Returns(_workingDirectory);
        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(m => m.ApplicationPaths).Returns(applicationPaths.Object);

        // prep BaseItem for calls made that expect managers
        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        BaseItem.ConfigurationManager = configurationManager.Object;

        _audioFileProber = new AudioFileProber(
            NullLogger<AudioFileProber>.Instance,
            mediaSourceManager.Object,
            mediaEncoder.Object,
            libraryManager.Object,
            new LyricResolver(
                NullLogger<LyricResolver>.Instance,
                Mock.Of<ILocalizationManager>(),
                mediaEncoder.Object,
                Mock.Of<IFileSystem>(),
                new NamingOptions()),
            Mock.Of<ILyricManager>(),
            Mock.Of<IMediaStreamRepository>(),
            Mock.Of<IChapterManager>());
    }

    [Fact]
    public async Task Probe_TagsCorrectedOnDisk_UpdatesArtistsOnScan()
    {
        var audio = await ProbeNewFile("Artist A feat. Artist B", "Artist A feat. Artist B");

        Assert.Equal(["Artist A feat. Artist B"], audio.Artists);
        Assert.Equal(["Artist A feat. Artist B"], audio.AlbumArtists);

        // The tags are corrected outside of Jellyfin, which is what a rescan is expected to pick up
        WriteArtistTags(audio.Path, "Artist A", "Artist A");
        await _audioFileProber.Probe(audio, GetRefreshOptions(MetadataRefreshMode.Default), CancellationToken.None);

        Assert.Equal(["Artist A"], audio.Artists);
        Assert.Equal(["Artist A"], audio.AlbumArtists);
    }

    [Fact]
    public async Task Probe_ArtistTagRemovedOnDisk_ClearsArtistsOnScan()
    {
        var audio = await ProbeNewFile("Artist A feat. Artist B", "Artist A");

        WriteArtistTags(audio.Path, string.Empty, "Artist A");
        await _audioFileProber.Probe(audio, GetRefreshOptions(MetadataRefreshMode.Default), CancellationToken.None);

        // Without an artist tag the album artist is all that is left to go by, as it would be on a first scan
        Assert.Empty(audio.Artists);
        Assert.Equal(["Artist A"], audio.AlbumArtists);
    }

    [Fact]
    public async Task Probe_ReplaceAllMetadata_UpdatesArtists()
    {
        var audio = await ProbeNewFile("Artist A feat. Artist B", "Artist A");

        WriteArtistTags(audio.Path, "Artist A", "Artist A");
        var options = GetRefreshOptions(MetadataRefreshMode.FullRefresh);
        options.ReplaceAllMetadata = true;
        await _audioFileProber.Probe(audio, options, CancellationToken.None);

        Assert.Equal(["Artist A"], audio.Artists);
    }

    [Theory]
    [InlineData(MetadataRefreshMode.ValidationOnly)]
    [InlineData(MetadataRefreshMode.FullRefresh)]
    public async Task Probe_NotReplacingMetadata_KeepsExistingArtists(MetadataRefreshMode refreshMode)
    {
        var audio = await ProbeNewFile("Artist A feat. Artist B", "Artist A");

        // Searching for missing metadata fills gaps, it does not overwrite what the item already has
        audio.Artists = ["Edited In Jellyfin"];
        WriteArtistTags(audio.Path, "Artist A", "Artist A");
        await _audioFileProber.Probe(audio, GetRefreshOptions(refreshMode), CancellationToken.None);

        Assert.Equal(["Edited In Jellyfin"], audio.Artists);
    }

    [Fact]
    public async Task Probe_LockedItem_KeepsExistingArtists()
    {
        var audio = await ProbeNewFile("Artist A feat. Artist B", "Artist A");

        audio.IsLocked = true;
        WriteArtistTags(audio.Path, "Artist A", "Artist A");
        await _audioFileProber.Probe(audio, GetRefreshOptions(MetadataRefreshMode.Default), CancellationToken.None);

        Assert.Equal(["Artist A feat. Artist B"], audio.Artists);
    }

    public void Dispose()
    {
        if (Directory.Exists(_workingDirectory))
        {
            Directory.Delete(_workingDirectory, true);
        }
    }

    private static MetadataRefreshOptions GetRefreshOptions(MetadataRefreshMode refreshMode)
    {
        var directoryService = new Mock<IDirectoryService>();
        directoryService.Setup(m => m.GetFilePaths(It.IsAny<string>(), It.IsAny<bool>()))
            .Returns([]);

        return new MetadataRefreshOptions(directoryService.Object)
        {
            MetadataRefreshMode = refreshMode
        };
    }

    private static void WriteArtistTags(string path, string artist, string albumArtist)
    {
        var track = new ATL.Track(path)
        {
            Artist = artist,
            AlbumArtist = albumArtist
        };

        Assert.True(track.Save());
    }

    private async Task<Audio> ProbeNewFile(string artist, string albumArtist)
    {
        var path = Path.Combine(_workingDirectory, Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + ".mp3");
        File.Copy(TestFileName, path);
        WriteArtistTags(path, artist, albumArtist);

        var audio = new Audio { Path = path };
        await _audioFileProber.Probe(audio, GetRefreshOptions(MetadataRefreshMode.Default), CancellationToken.None);

        return audio;
    }
}
