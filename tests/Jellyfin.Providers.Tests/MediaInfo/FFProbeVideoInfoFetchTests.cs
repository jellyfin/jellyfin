using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Common;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.MediaInfo;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

public class FFProbeVideoInfoFetchTests
{
    private readonly Mock<ILibraryManager> _mockLibraryManager;
    private readonly Mock<IServerConfigurationManager> _mockConfig;
    private readonly FFProbeVideoInfo _ffProbeVideoInfo;

    public FFProbeVideoInfoFetchTests()
    {
        _mockLibraryManager = new Mock<ILibraryManager>();
        _mockConfig = new Mock<IServerConfigurationManager>();

        var logger = new Mock<ILogger<FFProbeVideoInfo>>().Object;
        var mockMediaSourceManager = new Mock<IMediaSourceManager>();
        var mockMediaEncoder = new Mock<IMediaEncoder>();
        var mockBlurayExaminer = new Mock<IBlurayExaminer>();
        var mockLocalization = new Mock<ILocalizationManager>();
        var mockChapterManager = new Mock<IChapterManager>();
        var mockSubtitleManager = new Mock<ISubtitleManager>();

        // Resolvers are classes, we mock them providing nulls for constructor args as we don't need them
        var mockAudioResolver = new Mock<AudioResolver>(
            new Mock<ILogger<AudioResolver>>().Object,
            mockLocalization.Object,
            mockMediaEncoder.Object,
            new Mock<IFileSystem>().Object,
            new NamingOptions());

        var mockSubtitleResolver = new Mock<SubtitleResolver>(
            new Mock<ILogger<SubtitleResolver>>().Object,
            mockLocalization.Object,
            mockMediaEncoder.Object,
            new Mock<IFileSystem>().Object,
            new NamingOptions());

        var mockMediaAttachmentRepository = new Mock<IMediaAttachmentRepository>();
        var mockMediaStreamRepository = new Mock<IMediaStreamRepository>();

        _ffProbeVideoInfo = new FFProbeVideoInfo(
            logger,
            mockMediaSourceManager.Object,
            mockMediaEncoder.Object,
            mockBlurayExaminer.Object,
            mockLocalization.Object,
            mockChapterManager.Object,
            _mockConfig.Object,
            mockSubtitleManager.Object,
            _mockLibraryManager.Object,
            mockAudioResolver.Object,
            mockSubtitleResolver.Object,
            mockMediaAttachmentRepository.Object,
            mockMediaStreamRepository.Object);
    }

    [Fact]
    public void Fetch_HomeVideos_AppliesEmbeddedMetadata_AndLocksFields()
    {
        var video = new Movie { Path = "test.mkv" };
        var mediaInfo = new MediaBrowser.Model.MediaInfo.MediaInfo
        {
            Name = "Embedded Title",
            Overview = "Embedded Overview"
        };
        var libraryOptions = new LibraryOptions
        {
            EmbeddedMetadataPriority = EmbeddedMetadataPriority.ForHomeVideosOnly
        };
        var refreshOptions = new MetadataRefreshOptions(new DirectoryService(new Mock<IFileSystem>().Object));

        _mockLibraryManager.Setup(m => m.GetLibraryOptions(video)).Returns(libraryOptions);
        _mockLibraryManager.Setup(m => m.GetContentType(video)).Returns(CollectionType.homevideos);

        var method = typeof(FFProbeVideoInfo).GetMethod("FetchEmbeddedInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method!.Invoke(_ffProbeVideoInfo, [video, mediaInfo, refreshOptions, libraryOptions, CollectionType.homevideos]);

        Assert.Equal("Embedded Title", video.Name);
        Assert.Equal("Embedded Overview", video.Overview);
        Assert.Contains(MetadataField.Name, video.LockedFields);
        Assert.Contains(MetadataField.Overview, video.LockedFields);
    }

    [Fact]
    public void Fetch_Movies_DoesNotApplyEmbeddedTitle_ByDefault()
    {
        var video = new Movie { Path = "test.mkv" };
        var mediaInfo = new MediaBrowser.Model.MediaInfo.MediaInfo { Name = "Embedded Title" };
        var libraryOptions = new LibraryOptions
        {
            EmbeddedMetadataPriority = EmbeddedMetadataPriority.ForHomeVideosOnly
        };
        var refreshOptions = new MetadataRefreshOptions(new DirectoryService(new Mock<IFileSystem>().Object));

        var method = typeof(FFProbeVideoInfo).GetMethod("FetchEmbeddedInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method!.Invoke(_ffProbeVideoInfo, [video, mediaInfo, refreshOptions, libraryOptions, CollectionType.movies]);

        Assert.True(string.IsNullOrEmpty(video.Name), "Name should be null or empty");
        Assert.DoesNotContain(MetadataField.Name, video.LockedFields);
    }

    [Fact]
    public void Fetch_AlwaysPriority_AppliesEmbeddedTitle_AcrossTypes()
    {
        var video = new Movie { Path = "test.mkv" };
        var mediaInfo = new MediaBrowser.Model.MediaInfo.MediaInfo { Name = "Embedded Title" };
        var libraryOptions = new LibraryOptions
        {
            EmbeddedMetadataPriority = EmbeddedMetadataPriority.Always
        };
        var refreshOptions = new MetadataRefreshOptions(new DirectoryService(new Mock<IFileSystem>().Object));

        var method = typeof(FFProbeVideoInfo).GetMethod("FetchEmbeddedInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method!.Invoke(_ffProbeVideoInfo, [video, mediaInfo, refreshOptions, libraryOptions, CollectionType.movies]);

        Assert.Equal("Embedded Title", video.Name);
        Assert.Contains(MetadataField.Name, video.LockedFields);
    }

    [Fact]
    public void Fetch_NeverPriority_RespectsLegacyEnableEmbeddedTitles()
    {
        var video = new Movie { Path = "test.mkv" };
        var mediaInfo = new MediaBrowser.Model.MediaInfo.MediaInfo { Name = "Embedded Title" };
        var libraryOptions = new LibraryOptions
        {
            EmbeddedMetadataPriority = EmbeddedMetadataPriority.Never,
            EnableEmbeddedTitles = true
        };
        var refreshOptions = new MetadataRefreshOptions(new DirectoryService(new Mock<IFileSystem>().Object));

        var method = typeof(FFProbeVideoInfo).GetMethod("FetchEmbeddedInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method!.Invoke(_ffProbeVideoInfo, [video, mediaInfo, refreshOptions, libraryOptions, CollectionType.movies]);

        Assert.Equal("Embedded Title", video.Name);
        Assert.Contains(MetadataField.Name, video.LockedFields);
    }

    [Fact]
    public void Fetch_NeverPriority_DoesNotApplyWhenDisabled()
    {
        var video = new Movie { Path = "test.mkv" };
        var mediaInfo = new MediaBrowser.Model.MediaInfo.MediaInfo { Name = "Embedded Title" };
        var libraryOptions = new LibraryOptions
        {
            EmbeddedMetadataPriority = EmbeddedMetadataPriority.Never,
            EnableEmbeddedTitles = false
        };
        var refreshOptions = new MetadataRefreshOptions(new DirectoryService(new Mock<IFileSystem>().Object));

        var method = typeof(FFProbeVideoInfo).GetMethod("FetchEmbeddedInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method!.Invoke(_ffProbeVideoInfo, [video, mediaInfo, refreshOptions, libraryOptions, CollectionType.homevideos]);

        Assert.True(string.IsNullOrEmpty(video.Name), "Name should be null or empty");
        Assert.DoesNotContain(MetadataField.Name, video.LockedFields);
    }

    [Fact]
    public void Fetch_ExistingMetadata_NotOverwrittenByEmbedded_UnlessReplaceData()
    {
        var video = new Movie { Path = "test.mkv", Name = "Existing Name" };
        var mediaInfo = new MediaBrowser.Model.MediaInfo.MediaInfo { Name = "Embedded Title" };
        var libraryOptions = new LibraryOptions
        {
            EmbeddedMetadataPriority = EmbeddedMetadataPriority.Always
        };
        var refreshOptions = new MetadataRefreshOptions(new DirectoryService(new Mock<IFileSystem>().Object))
        {
            ReplaceAllMetadata = false
        };

        var method = typeof(FFProbeVideoInfo).GetMethod("FetchEmbeddedInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method!.Invoke(_ffProbeVideoInfo, [video, mediaInfo, refreshOptions, libraryOptions, CollectionType.movies]);

        // Should NOT overwrite existing name if it's not empty, even if priority is Always
        Assert.Equal("Existing Name", video.Name);
        Assert.DoesNotContain(MetadataField.Name, video.LockedFields);
    }

    [Fact]
    public void Fetch_ExistingMetadata_OverwrittenByEmbedded_WhenReplaceData()
    {
        var video = new Movie { Path = "test.mkv", Name = "Existing Name" };
        var mediaInfo = new MediaBrowser.Model.MediaInfo.MediaInfo { Name = "Embedded Title" };
        var libraryOptions = new LibraryOptions
        {
            EmbeddedMetadataPriority = EmbeddedMetadataPriority.Always
        };
        var refreshOptions = new MetadataRefreshOptions(new DirectoryService(new Mock<IFileSystem>().Object))
        {
            ReplaceAllMetadata = true
        };

        var method = typeof(FFProbeVideoInfo).GetMethod("FetchEmbeddedInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method!.Invoke(_ffProbeVideoInfo, [video, mediaInfo, refreshOptions, libraryOptions, CollectionType.movies]);

        // Should overwrite existing name if ReplaceAllMetadata is true
        Assert.Equal("Embedded Title", video.Name);
        Assert.Contains(MetadataField.Name, video.LockedFields);
    }
}
