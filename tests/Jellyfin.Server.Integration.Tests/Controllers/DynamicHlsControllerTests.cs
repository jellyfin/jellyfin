using System;
using System.Net;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

public sealed class DynamicHlsControllerTests : IClassFixture<JellyfinApplicationFactory>
{
    private const string SupportedAudioCodecs = "aac,ac3,alac,dts,eac3,flac,mp3,opus,truehd";
    private readonly JellyfinApplicationFactory _factory;
    private static string? _accessToken;

    public DynamicHlsControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("AudioCodec")]
    [InlineData("VideoCodec")]
    [InlineData("SubtitleCodec")]
    [InlineData("Container")]
    [InlineData("SegmentContainer")]
    public async Task GetLiveHlsStream_UsesSharedValidationLimit(string parameter)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));
        var url = $"Videos/{Guid.NewGuid()}/live.m3u8";

        using var valid = await client.GetAsync(QueryHelpers.AddQueryString(url, parameter, new string('a', 256)), TestContext.Current.CancellationToken);
        using var invalid = await client.GetAsync(QueryHelpers.AddQueryString(url, parameter, new string('a', 257)), TestContext.Current.CancellationToken);

        // Valid parameters reach media lookup; no library item or FFmpeg process is needed.
        Assert.Equal(HttpStatusCode.NotFound, valid.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Theory]
    [InlineData("vorbis", SupportedAudioCodecs)]
    [InlineData("truehd", "truehd")]
    public async Task GetMasterHlsPlaylist_GeneratedCodecList_PassesValidation(string sourceCodec, string expectedCodecs)
    {
        var source = new MediaSourceInfo
        {
            Id = "test-source",
            Container = "mkv",
            Bitrate = 2_000_000,
            Protocol = MediaProtocol.File,
            SupportsTranscoding = true,
            MediaStreams =
            [
                new MediaStream { Type = MediaStreamType.Video, Index = 0, Codec = "h264" },
                new MediaStream { Type = MediaStreamType.Audio, Index = 1, Codec = sourceCodec, Channels = 2 }
            ]
        };
        var options = new MediaOptions
        {
            ItemId = Guid.NewGuid(),
            DeviceId = "test-device",
            MediaSources = [source],
            MediaSourceId = source.Id,
            AudioStreamIndex = 1,
            AllowAudioStreamCopy = true,
            AllowVideoStreamCopy = true,
            EnableDirectPlay = false,
            EnableDirectStream = false,
            Profile = new DeviceProfile
            {
                TranscodingProfiles =
                [
                    new TranscodingProfile
                    {
                        Type = DlnaProfileType.Video,
                        Context = EncodingContext.Streaming,
                        Protocol = MediaStreamProtocol.hls,
                        Container = "mp4",
                        VideoCodec = "h264,hevc,vp9,av1",
                        AudioCodec = SupportedAudioCodecs
                    }
                ]
            }
        };
        var stream = new StreamBuilder(Mock.Of<ITranscoderSupport>(), NullLogger.Instance).GetOptimalVideoStream(options);
        Assert.NotNull(stream);
        var url = stream.ToUrl(null, null, null);
        var query = QueryHelpers.ParseQuery(new Uri("http://localhost" + url).Query);
        // Preserve codec priority when transcoding and the selected codec when copying.
        Assert.Equal(expectedCodecs, query["AudioCodec"]);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));
        using var response = await client.GetAsync(url, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
