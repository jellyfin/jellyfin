using System;
using AutoFixture;
using AutoFixture.AutoMoq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Providers.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

public class FFProbeVideoInfoTests
{
    private readonly FFProbeVideoInfo _fFProbeVideoInfo;

    public FFProbeVideoInfoTests()
    {
        var serverConfiguration = new ServerConfiguration()
        {
            DummyChapterDuration = (int)TimeSpan.FromMinutes(5).TotalSeconds
        };
        var serverConfig = new Mock<IServerConfigurationManager>();
        serverConfig.Setup(c => c.Configuration)
            .Returns(serverConfiguration);

        IFixture fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
        fixture.Inject(serverConfig);
        _fFProbeVideoInfo = fixture.Create<FFProbeVideoInfo>();
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void CreateDummyChapters_InvalidRuntime_ThrowsArgumentException(long? runtime)
    {
        Assert.Throws<ArgumentException>(
            () => _fFProbeVideoInfo.CreateDummyChapters(new Video()
            {
                RunTimeTicks = runtime
            }));
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(0L, 0)]
    [InlineData(1L, 1)]
    [InlineData(TimeSpan.TicksPerMinute * 3, 1)]
    [InlineData(TimeSpan.TicksPerMinute * 5, 1)]
    [InlineData((TimeSpan.TicksPerMinute * 5) + 1, 1)]
    [InlineData(TimeSpan.TicksPerMinute * 50, 10)]
    public void CreateDummyChapters_ValidRuntime_CorrectChaptersCount(long? runtime, int chaptersCount)
    {
        var chapters = _fFProbeVideoInfo.CreateDummyChapters(new Video()
        {
            RunTimeTicks = runtime
        });

        Assert.Equal(chaptersCount, chapters.Length);
    }

    [Theory]
    [InlineData(1L)]
    [InlineData(TimeSpan.TicksPerMinute * 3)]
    [InlineData(TimeSpan.TicksPerMinute * 5)]
    [InlineData((TimeSpan.TicksPerMinute * 5) + 1)]
    [InlineData((TimeSpan.TicksPerMinute * 50) + 1)]
    public void CreateDummyChapters_PositiveRuntime_NoChapterBeyondRuntime(long runtime)
    {
        var chapters = _fFProbeVideoInfo.CreateDummyChapters(new Video()
        {
            RunTimeTicks = runtime
        });

        Assert.All(chapters, chapter => Assert.True(chapter.StartPositionTicks < runtime));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FetchEmbeddedInfo_NoExtra_AppliesContainerDates(bool replaceAllMetadata)
    {
        var video = new Video();

        _fFProbeVideoInfo.FetchEmbeddedInfo(video, CreateMediaInfoWithDates(), CreateRefreshOptions(replaceAllMetadata), new LibraryOptions());

        Assert.Equal(2016, video.ProductionYear);
        Assert.Equal(new DateTime(2016, 5, 4, 0, 0, 0, DateTimeKind.Utc), video.PremiereDate);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FetchEmbeddedInfo_Extra_IgnoresContainerDates(bool replaceAllMetadata)
    {
        var video = new Video
        {
            ExtraType = ExtraType.Trailer,
            ProductionYear = 1982,
            PremiereDate = new DateTime(1982, 6, 25, 0, 0, 0, DateTimeKind.Utc)
        };

        _fFProbeVideoInfo.FetchEmbeddedInfo(video, CreateMediaInfoWithDates(), CreateRefreshOptions(replaceAllMetadata), new LibraryOptions());

        Assert.Equal(1982, video.ProductionYear);
        Assert.Equal(new DateTime(1982, 6, 25, 0, 0, 0, DateTimeKind.Utc), video.PremiereDate);
    }

    [Fact]
    public void FetchEmbeddedInfo_ExtraWithoutDates_StaysWithoutDates()
    {
        var video = new Video
        {
            ExtraType = ExtraType.Trailer
        };

        _fFProbeVideoInfo.FetchEmbeddedInfo(video, CreateMediaInfoWithDates(), CreateRefreshOptions(false), new LibraryOptions());

        Assert.Null(video.ProductionYear);
        Assert.Null(video.PremiereDate);
    }

    [Fact]
    public void AreStreamLayoutsCompatible_MatchingEmbeddedStreams_ReturnsTrue()
    {
        var ffprobeInfo = CreateMediaInfo(
            new MediaStream { Index = 0, Type = MediaStreamType.Video },
            new MediaStream { Index = 1, Type = MediaStreamType.Audio },
            new MediaStream { Index = 2, Type = MediaStreamType.Subtitle });
        var bdInfo = CreateBlurayDiscInfo(
            new MediaStream { Index = 0, Type = MediaStreamType.Video },
            new MediaStream { Index = 1, Type = MediaStreamType.Audio },
            new MediaStream { Index = 2, Type = MediaStreamType.Subtitle });

        Assert.True(FFProbeVideoInfo.AreStreamLayoutsCompatible(ffprobeInfo, bdInfo));
    }

    [Fact]
    public void AreStreamLayoutsCompatible_DifferentStreamTypes_ReturnsFalse()
    {
        var ffprobeInfo = CreateMediaInfo(
            new MediaStream { Index = 0, Type = MediaStreamType.Video },
            new MediaStream { Index = 1, Type = MediaStreamType.Subtitle });
        var bdInfo = CreateBlurayDiscInfo(
            new MediaStream { Index = 0, Type = MediaStreamType.Video },
            new MediaStream { Index = 1, Type = MediaStreamType.Audio });

        Assert.False(FFProbeVideoInfo.AreStreamLayoutsCompatible(ffprobeInfo, bdInfo));
    }

    [Fact]
    public void AreStreamLayoutsCompatible_ExternalStream_ReturnsFalse()
    {
        var ffprobeInfo = CreateMediaInfo(
            new MediaStream { Index = 0, Type = MediaStreamType.Video },
            new MediaStream { Index = 1, Type = MediaStreamType.Subtitle, IsExternal = true });
        var bdInfo = CreateBlurayDiscInfo(
            new MediaStream { Index = 0, Type = MediaStreamType.Video },
            new MediaStream { Index = 1, Type = MediaStreamType.Subtitle });

        Assert.False(FFProbeVideoInfo.AreStreamLayoutsCompatible(ffprobeInfo, bdInfo));
    }

    [Fact]
    public void AreStreamLayoutsCompatible_ExtraFFprobeAudioStream_ReturnsTrue()
    {
        var ffprobeInfo = CreateMediaInfo(
            new MediaStream { Index = 0, Type = MediaStreamType.Video },
            new MediaStream { Index = 1, Type = MediaStreamType.Audio, Codec = "truehd", Channels = 8 },
            new MediaStream { Index = 2, Type = MediaStreamType.Audio, Codec = "ac3", Channels = 6 },
            new MediaStream { Index = 3, Type = MediaStreamType.Audio, Codec = "ac3", Channels = 6 },
            new MediaStream { Index = 4, Type = MediaStreamType.Audio, Codec = "dts", Channels = 6 },
            new MediaStream { Index = 5, Type = MediaStreamType.Subtitle });
        var bdInfo = CreateBlurayDiscInfo(
            new MediaStream { Index = 0, Type = MediaStreamType.Video },
            new MediaStream { Index = 1, Type = MediaStreamType.Audio, Codec = "Atmos", Channels = 8 },
            new MediaStream { Index = 2, Type = MediaStreamType.Audio, Codec = "AC3", Channels = 6 },
            new MediaStream { Index = 3, Type = MediaStreamType.Audio, Codec = "dts", Channels = 6 },
            new MediaStream { Index = 4, Type = MediaStreamType.Subtitle });

        Assert.True(FFProbeVideoInfo.AreStreamLayoutsCompatible(ffprobeInfo, bdInfo));
    }

    [Fact]
    public void AreStreamLayoutsCompatible_DifferentSubtitleCount_ReturnsFalse()
    {
        var ffprobeInfo = CreateMediaInfo(
            new MediaStream { Index = 0, Type = MediaStreamType.Video },
            new MediaStream { Index = 1, Type = MediaStreamType.Subtitle },
            new MediaStream { Index = 2, Type = MediaStreamType.Subtitle });
        var bdInfo = CreateBlurayDiscInfo(
            new MediaStream { Index = 0, Type = MediaStreamType.Video },
            new MediaStream { Index = 1, Type = MediaStreamType.Subtitle });

        Assert.False(FFProbeVideoInfo.AreStreamLayoutsCompatible(ffprobeInfo, bdInfo));
    }

    [Fact]
    public void ApplyBdInfoLanguages_ExtraFFprobeAudioStream_MapsMatchingStreamsOnly()
    {
        var truehdStream = new MediaStream { Index = 1, Type = MediaStreamType.Audio, Codec = "truehd", Channels = 8 };
        var firstAc3Stream = new MediaStream { Index = 2, Type = MediaStreamType.Audio, Codec = "ac3", Channels = 6 };
        var extraAc3Stream = new MediaStream { Index = 3, Type = MediaStreamType.Audio, Codec = "ac3", Channels = 6 };
        var dtsStream = new MediaStream { Index = 4, Type = MediaStreamType.Audio, Codec = "dts", Channels = 6 };
        var subtitleStream = new MediaStream { Index = 5, Type = MediaStreamType.Subtitle };
        var ffprobeInfo = CreateMediaInfo(
            new MediaStream { Index = 0, Type = MediaStreamType.Video },
            truehdStream,
            firstAc3Stream,
            extraAc3Stream,
            dtsStream,
            subtitleStream);
        var bdInfo = CreateBlurayDiscInfo(
            new MediaStream { Index = 0, Type = MediaStreamType.Video },
            new MediaStream { Index = 1, Type = MediaStreamType.Audio, Codec = "Atmos", Channels = 8, Language = "eng" },
            new MediaStream { Index = 2, Type = MediaStreamType.Audio, Codec = "AC3", Channels = 6, Language = "fra" },
            new MediaStream { Index = 3, Type = MediaStreamType.Audio, Codec = "dts", Channels = 6, Language = "spa" },
            new MediaStream { Index = 4, Type = MediaStreamType.Subtitle, Language = "zho" });

        FFProbeVideoInfo.ApplyBdInfoLanguages(ffprobeInfo, bdInfo);

        Assert.Equal("eng", truehdStream.Language);
        Assert.Equal("fra", firstAc3Stream.Language);
        Assert.Null(extraAc3Stream.Language);
        Assert.Equal("spa", dtsStream.Language);
        Assert.Equal("zho", subtitleStream.Language);
    }

    [Fact]
    public void ApplyBdInfoLanguages_MissingLanguages_FillsAudioAndSubtitleLanguagesOnly()
    {
        var videoStream = new MediaStream { Index = 0, Type = MediaStreamType.Video };
        var audioStream = new MediaStream { Index = 1, Type = MediaStreamType.Audio };
        var subtitleStream = new MediaStream { Index = 2, Type = MediaStreamType.Subtitle, Language = "fra" };
        var ffprobeInfo = CreateMediaInfo(videoStream, audioStream, subtitleStream);
        var bdInfo = CreateBlurayDiscInfo(
            new MediaStream { Index = 0, Type = MediaStreamType.Video, Language = "eng" },
            new MediaStream { Index = 1, Type = MediaStreamType.Audio, Language = "eng" },
            new MediaStream { Index = 2, Type = MediaStreamType.Subtitle, Language = "zho" });

        FFProbeVideoInfo.ApplyBdInfoLanguages(ffprobeInfo, bdInfo);

        Assert.Null(videoStream.Language);
        Assert.Equal("eng", audioStream.Language);
        Assert.Equal("fra", subtitleStream.Language);
    }

    private static MediaBrowser.Model.MediaInfo.MediaInfo CreateMediaInfo(params MediaStream[] streams)
        => new() { MediaStreams = streams };

    private static BlurayDiscInfo CreateBlurayDiscInfo(params MediaStream[] streams)
        => new() { MediaStreams = streams };

    private static MediaBrowser.Model.MediaInfo.MediaInfo CreateMediaInfoWithDates()
        => new()
        {
            ProductionYear = 2016,
            PremiereDate = new DateTime(2016, 5, 4, 0, 0, 0, DateTimeKind.Utc)
        };

    private static MetadataRefreshOptions CreateRefreshOptions(bool replaceAllMetadata)
        => new(Mock.Of<IDirectoryService>())
        {
            ReplaceAllMetadata = replaceAllMetadata
        };
}
