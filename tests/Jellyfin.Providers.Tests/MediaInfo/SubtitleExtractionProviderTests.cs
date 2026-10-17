using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Providers.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

[Collection(nameof(SubtitleExtractionProviderTestsCollection))]
public sealed class SubtitleExtractionProviderTests : IDisposable
{
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IMediaSourceManager? _previousMediaSourceManager;
    private readonly IMediaSegmentManager? _previousMediaSegmentManager;
    private readonly IRecordingsManager? _previousRecordingsManager;

    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly Mock<IMediaSourceManager> _mediaSourceManager = new();
    private readonly Mock<ISubtitleEncoder> _subtitleEncoder = new();
    private readonly LibraryOptions _libraryOptions = new() { ExtractSubtitlesDuringLibraryScan = true };
    private readonly SubtitleExtractionProvider _provider;

    public SubtitleExtractionProviderTests()
    {
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousMediaSourceManager = BaseItem.MediaSourceManager;
        _previousMediaSegmentManager = BaseItem.MediaSegmentManager;
        _previousRecordingsManager = Video.RecordingsManager;

        BaseItem.LibraryManager = _libraryManager.Object;
        BaseItem.MediaSourceManager = _mediaSourceManager.Object;
        BaseItem.MediaSegmentManager = Mock.Of<IMediaSegmentManager>();
        Video.RecordingsManager = Mock.Of<IRecordingsManager>();

        _libraryManager.Setup(m => m.GetLibraryOptions(It.IsAny<BaseItem>()))
            .Returns(_libraryOptions);
        _mediaSourceManager.Setup(m => m.GetPathProtocol(It.IsAny<string>()))
            .Returns(MediaProtocol.File);

        _provider = new SubtitleExtractionProvider(_libraryManager.Object, _subtitleEncoder.Object);
    }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager;
        BaseItem.MediaSourceManager = _previousMediaSourceManager;
        BaseItem.MediaSegmentManager = _previousMediaSegmentManager;
        Video.RecordingsManager = _previousRecordingsManager;
    }

    [Theory]
    [InlineData("subrip")]
    [InlineData("ass")]
    [InlineData("PGSSUB")]
    public async Task FetchAsync_EmbeddedExtractableSubtitle_ExtractsOwnMediaSource(string codec)
    {
        var movie = CreateMovie(EmbeddedSubtitle(codec));

        await _provider.FetchAsync(movie, new MetadataRefreshOptions(Mock.Of<IDirectoryService>()), CancellationToken.None);

        var expectedId = movie.Id.ToString("N", CultureInfo.InvariantCulture);
        _subtitleEncoder.Verify(
            e => e.ExtractAllExtractableSubtitles(It.Is<MediaSourceInfo>(s => s.Id == expectedId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FetchAsync_OptionDisabled_DoesNotExtract()
    {
        _libraryOptions.ExtractSubtitlesDuringLibraryScan = false;
        var movie = CreateMovie(EmbeddedSubtitle("subrip"));

        await _provider.FetchAsync(movie, new MetadataRefreshOptions(Mock.Of<IDirectoryService>()), CancellationToken.None);

        VerifyNotExtracted();
    }

    [Fact]
    public async Task FetchAsync_NoEmbeddedSubtitles_DoesNotExtract()
    {
        var external = EmbeddedSubtitle("subrip");
        external.IsExternal = true;
        external.Path = "/media/movie.en.srt";
        var dvb = EmbeddedSubtitle("dvb_subtitle");
        dvb.SupportsExternalStream = false;
        var movie = CreateMovie(
            new MediaStream { Type = MediaStreamType.Video, Codec = "h264" },
            external,
            dvb);

        await _provider.FetchAsync(movie, new MetadataRefreshOptions(Mock.Of<IDirectoryService>()), CancellationToken.None);

        VerifyNotExtracted();
    }

    [Fact]
    public async Task FetchAsync_DiscStructure_DoesNotExtract()
    {
        var movie = CreateMovie(EmbeddedSubtitle("PGSSUB"));
        movie.VideoType = VideoType.BluRay;

        await _provider.FetchAsync(movie, new MetadataRefreshOptions(Mock.Of<IDirectoryService>()), CancellationToken.None);

        VerifyNotExtracted();
    }

    [Fact]
    public async Task FetchAsync_Shortcut_DoesNotExtract()
    {
        var movie = CreateMovie(EmbeddedSubtitle("subrip"));
        movie.IsShortcut = true;

        await _provider.FetchAsync(movie, new MetadataRefreshOptions(Mock.Of<IDirectoryService>()), CancellationToken.None);

        VerifyNotExtracted();
    }

    [Fact]
    public async Task FetchAsync_RemoteVideo_DoesNotExtract()
    {
        _mediaSourceManager.Setup(m => m.GetPathProtocol(It.IsAny<string>()))
            .Returns(MediaProtocol.Http);
        var movie = CreateMovie(EmbeddedSubtitle("subrip"));

        await _provider.FetchAsync(movie, new MetadataRefreshOptions(Mock.Of<IDirectoryService>()), CancellationToken.None);

        VerifyNotExtracted();
    }

    private static MediaStream EmbeddedSubtitle(string codec)
    {
        // SupportsExternalStream is set by the media source manager for text, PGS and VobSub streams
        return new MediaStream
        {
            Type = MediaStreamType.Subtitle,
            Codec = codec,
            SupportsExternalStream = true
        };
    }

    private Movie CreateMovie(params MediaStream[] streams)
    {
        var movie = new Movie
        {
            Id = Guid.NewGuid(),
            Path = "/media/movie.mkv",
            VideoType = VideoType.VideoFile
        };

        for (var i = 0; i < streams.Length; i++)
        {
            streams[i].Index = i;
        }

        _mediaSourceManager.Setup(m => m.GetMediaStreams(movie.Id))
            .Returns(streams);

        return movie;
    }

    private void VerifyNotExtracted()
    {
        _subtitleEncoder.Verify(
            e => e.ExtractAllExtractableSubtitles(It.IsAny<MediaSourceInfo>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
