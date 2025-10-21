using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
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
        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>()))
                .Returns((string x) => MediaProtocol.File);
        BaseItem.MediaSourceManager = mediaSourceManager.Object;

        var video = new Video()
        {
            Path = primaryPath
        };

        var videoAlt = new Video()
        {
            Path = altPath,
        };

        video.LocalAlternateVersions = [videoAlt.Path];

        Assert.Equal(name, video.GetMediaSourceName(video));
        Assert.Equal(altName, video.GetMediaSourceName(videoAlt));
    }

    [Fact]
    public void GetMediaSources_DefaultDescending_OrdersHighToLow()
    {
        var (video, _) = SetupVideoTest(null, null);

        var sources = video.GetMediaSources(false);

        Assert.Equal(3, sources.Count);
        Assert.Equal(2160, sources[0].VideoStream?.Height);
        Assert.Equal(1080, sources[1].VideoStream?.Height);
        Assert.Equal(720, sources[2].VideoStream?.Height);
    }

    [Fact]
    public void GetMediaSources_AscendingSortOrder_OrdersLowToHigh()
    {
        var (video, _) = SetupVideoTest(MediaSourceSortOrder.Ascending, null);

        var sources = video.GetMediaSources(false);

        Assert.Equal(3, sources.Count);
        Assert.Equal(720, sources[0].VideoStream?.Height);
        Assert.Equal(1080, sources[1].VideoStream?.Height);
        Assert.Equal(2160, sources[2].VideoStream?.Height);
    }

    [Fact]
    public void GetMediaSources_PreferredHeight1080_Places1080First()
    {
        var (video, _) = SetupVideoTest(MediaSourceSortOrder.Descending, 1080);

        var sources = video.GetMediaSources(false);

        Assert.Equal(3, sources.Count);
        Assert.Equal(1080, sources[0].VideoStream?.Height);
        Assert.Equal(2160, sources[1].VideoStream?.Height);
        Assert.Equal(720, sources[2].VideoStream?.Height);
    }

    [Fact]
    public void GetMediaSources_PreferredHeight720WithAscending_Places720FirstThenAscending()
    {
        var (video, _) = SetupVideoTest(MediaSourceSortOrder.Ascending, 720);

        var sources = video.GetMediaSources(false);

        Assert.Equal(3, sources.Count);
        Assert.Equal(720, sources[0].VideoStream?.Height);
        Assert.Equal(1080, sources[1].VideoStream?.Height);
        Assert.Equal(2160, sources[2].VideoStream?.Height);
    }

    [Fact]
    public void GetMediaSources_PreferredHeight2160WithAscending_Places2160FirstThenAscending()
    {
        var (video, _) = SetupVideoTest(MediaSourceSortOrder.Ascending, 2160);

        var sources = video.GetMediaSources(false);

        Assert.Equal(3, sources.Count);
        Assert.Equal(2160, sources[0].VideoStream?.Height);
        Assert.Equal(720, sources[1].VideoStream?.Height);
        Assert.Equal(1080, sources[2].VideoStream?.Height);
    }

    private static (TestableVideo Video, Mock<IMediaSourceManager> MediaSourceManager) SetupVideoTest(MediaSourceSortOrder? sortOrder, int? preferredHeight)
    {
        var serverConfiguration = new ServerConfiguration
        {
            MediaSourceOptions = new MediaSourceOptions
            {
                SortOrder = sortOrder ?? MediaSourceSortOrder.Descending,
                PreferredVideoHeight = preferredHeight
            }
        };

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(x => x.Configuration).Returns(serverConfiguration);

        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>()))
            .Returns(MediaProtocol.File);
        mediaSourceManager.Setup(x => x.GetMediaStreams(It.IsAny<Guid>()))
            .Returns((Guid id) =>
            {
                // Return appropriate video stream based on the video ID
                var streams = new List<MediaStream>();
                var idString = id.ToString();
                if (idString.EndsWith("0720", StringComparison.Ordinal))
                {
                    streams.Add(new MediaStream { Type = MediaStreamType.Video, Height = 720, Width = 1280 });
                }
                else if (idString.EndsWith("1080", StringComparison.Ordinal))
                {
                    streams.Add(new MediaStream { Type = MediaStreamType.Video, Height = 1080, Width = 1920 });
                }
                else if (idString.EndsWith("2160", StringComparison.Ordinal))
                {
                    streams.Add(new MediaStream { Type = MediaStreamType.Video, Height = 2160, Width = 3840 });
                }

                return streams;
            });
        mediaSourceManager.Setup(x => x.GetMediaAttachments(It.IsAny<Guid>()))
            .Returns(Array.Empty<MediaAttachment>());

        var recordingsManager = new Mock<IRecordingsManager>();
        recordingsManager.Setup(x => x.GetActiveRecordingInfo(It.IsAny<string>()))
            .Returns((ActiveRecordingInfo?)null);

        var mediaSegmentManager = new Mock<IMediaSegmentManager>();
        mediaSegmentManager.Setup(x => x.IsTypeSupported(It.IsAny<BaseItem>()))
            .Returns(false);
        mediaSegmentManager.Setup(x => x.HasSegments(It.IsAny<Guid>()))
            .Returns(false);

        BaseItem.ConfigurationManager = configurationManager.Object;
        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        BaseItem.MediaSegmentManager = mediaSegmentManager.Object;
        Video.RecordingsManager = recordingsManager.Object;

        var video = new TestableVideo();

        return (video, mediaSourceManager);
    }

    private class TestableVideo : Video
    {
        public TestableVideo()
        {
            // Create three versions with different resolutions
            Id = Guid.Parse("00000000-0000-0000-0000-000000002160");
            Path = "/media/movie-4k.mkv";

            LocalAlternateVersions =
            [
                "/media/movie-1080p.mkv",
                "/media/movie-720p.mkv"
            ];
        }

        protected override IEnumerable<(BaseItem Item, MediaSourceType MediaSourceType)> GetAllItemsForMediaSources()
        {
            // Return the main item (4K)
            yield return (this, MediaSourceType.Default);

            // Return alternate versions
            var video1080 = new Video
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000001080"),
                Path = "/media/movie-1080p.mkv"
            };
            yield return (video1080, MediaSourceType.Default);

            var video720 = new Video
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000720"),
                Path = "/media/movie-720p.mkv"
            };
            yield return (video720, MediaSourceType.Default);
        }
    }
}
