using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.MediaInfoDtos;
using Jellyfin.Api.Models.UserDtos;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

public sealed class VideoPlaybackStreamingTests : IClassFixture<VideoPlaybackStreamingTests.PlaybackFactory>
{
    private readonly PlaybackFactory _factory;
    private static string? _startupToken;

    public VideoPlaybackStreamingTests(PlaybackFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("Videos/{0}/stream?static=true&playSessionId={1}")]
    [InlineData("Videos/{0}/stream?static=false&container=ts&videoCodec=copy&audioCodec=copy&playSessionId={1}")]
    [InlineData("Videos/{0}/stream?static=false&container=ts&videoCodec=h264&audioCodec=aac&allowVideoStreamCopy=false&allowAudioStreamCopy=false&playSessionId={1}")]
    [InlineData("Videos/{0}/hls1/main/0.ts?runtimeTicks=600000000&actualSegmentLengthTicks=60000000&segmentContainer=ts&segmentLength=6&videoCodec=h264&audioCodec=aac&playSessionId={1}")]
    [InlineData("Items/{0}/File?playSessionId={1}")]
    public async Task Stream_SecondDeviceIsDeniedBeforeEncoderChanges_FirstDeviceCanSeekAndChangeEncoding(string deniedPath)
    {
        var clients = await CreatePlaybackClientsAsync().ConfigureAwait(true);
        using var first = clients.First;
        using var second = clients.Second;
        var movie = _factory.CreateMovie();
        var original = await RegisterAsync(first, movie.Id, "player-a").ConfigureAwait(true);
        var competing = await RegisterAsync(second, movie.Id, "player-b").ConfigureAwait(true);
        using (var response = await first.GetAsync(GetStaticPath(movie.Id, original.PlaySessionId!), TestContext.Current.CancellationToken).ConfigureAwait(true))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(PlaybackFactory.MediaBytes, await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
        }

        using (var denied = await second.GetAsync(
            string.Format(CultureInfo.InvariantCulture, deniedPath, movie.Id, competing.PlaySessionId),
            TestContext.Current.CancellationToken).ConfigureAwait(true))
        {
            await AssertLimitReachedAsync(denied).ConfigureAwait(true);
        }

        // A new encoding for the first logical player must continue using its original admission.
        var replacement = await RegisterAsync(first, movie.Id, "player-a").ConfigureAwait(true);
        Assert.NotEqual(original.PlaySessionId, replacement.PlaySessionId);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(index => AssertRangeAsync(
            first,
            movie.Id,
            (index % 2) == 0 ? original.PlaySessionId! : replacement.PlaySessionId!))).ConfigureAwait(true);

        using var stillDenied = await second.GetAsync(GetStaticPath(movie.Id, competing.PlaySessionId!), TestContext.Current.CancellationToken).ConfigureAwait(true);
        await AssertLimitReachedAsync(stillDenied).ConfigureAwait(true);
        _factory.VerifyNoEncoderChanges();
    }

    [Fact]
    public async Task Stream_HeadDoesNotConsumeQuotaOrDisplaceFirstPlayer()
    {
        var clients = await CreatePlaybackClientsAsync().ConfigureAwait(true);
        using var first = clients.First;
        using var second = clients.Second;
        var movie = _factory.CreateMovie();
        var original = await RegisterAsync(first, movie.Id, "player-a").ConfigureAwait(true);
        var competing = await RegisterAsync(second, movie.Id, "player-b").ConfigureAwait(true);
        using (var headRequest = new HttpRequestMessage(HttpMethod.Head, GetStaticPath(movie.Id, competing.PlaySessionId!)))
        using (var head = await second.SendAsync(headRequest, TestContext.Current.CancellationToken).ConfigureAwait(true))
        {
            Assert.Equal(HttpStatusCode.OK, head.StatusCode);
            Assert.Empty(await head.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
        }

        await AssertRangeAsync(first, movie.Id, original.PlaySessionId!).ConfigureAwait(true);
        using var denied = await second.GetAsync(GetStaticPath(movie.Id, competing.PlaySessionId!), TestContext.Current.CancellationToken).ConfigureAwait(true);
        await AssertLimitReachedAsync(denied).ConfigureAwait(true);
        _factory.VerifyNoEncoderChanges();
    }

    [Fact]
    public async Task Stream_EncodingAliasCannotBeBorrowedByAnotherAuthenticatedDevice()
    {
        var clients = await CreatePlaybackClientsAsync().ConfigureAwait(true);
        using var first = clients.First;
        using var second = clients.Second;
        var movie = _factory.CreateMovie();
        var original = await RegisterAsync(first, movie.Id, "player-a").ConfigureAwait(true);

        using var impersonation = await second.GetAsync(GetStaticPath(movie.Id, original.PlaySessionId!), TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.Forbidden, impersonation.StatusCode);
        Assert.False(impersonation.Headers.Contains("X-Jellyfin-Error-Code"));
        await AssertRangeAsync(first, movie.Id, original.PlaySessionId!).ConfigureAwait(true);
        _factory.VerifyNoEncoderChanges();
    }

    [Theory]
    [InlineData("stop")]
    [InlineData("legacy-stop")]
    [InlineData("ping")]
    public async Task PlaybackControl_AnotherTokenCopiesDeviceHeader_CannotStopOrPingFirstPlayer(string operation)
    {
        var clients = await CreatePlaybackClientsAsync().ConfigureAwait(true);
        using var first = clients.First;
        using var second = clients.Second;
        var movie = _factory.CreateMovie();
        var original = await RegisterAsync(first, movie.Id, "player-a").ConfigureAwait(true);
        _factory.RegisterTranscodingJob(movie, original.PlaySessionId!);
        await AssertRangeAsync(first, movie.Id, original.PlaySessionId!).ConfigureAwait(true);

        var forgedHeader = Assert.Single(second.DefaultRequestHeaders.GetValues(AuthHelper.AuthHeaderName))
            .Replace("video-device-b", "video-device-a", StringComparison.Ordinal);
        second.DefaultRequestHeaders.Remove(AuthHelper.AuthHeaderName);
        second.DefaultRequestHeaders.Add(AuthHelper.AuthHeaderName, forgedHeader);
        using var request = operation switch
        {
            "stop" => new HttpRequestMessage(HttpMethod.Post, "Sessions/Playing/Stopped")
            {
                Content = JsonContent.Create(
                    new PlaybackStopInfo { ItemId = movie.Id, PlaySessionId = original.PlaySessionId },
                    options: JsonDefaults.Options)
            },
            "legacy-stop" => new HttpRequestMessage(HttpMethod.Delete, $"PlayingItems/{movie.Id}?playSessionId={original.PlaySessionId}"),
            _ => new HttpRequestMessage(HttpMethod.Post, $"Sessions/Playing/Ping?playSessionId={original.PlaySessionId}")
        };

        using var response = await second.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        _factory.VerifyNoPlaybackControlChanges();
        await AssertRangeAsync(first, movie.Id, original.PlaySessionId!).ConfigureAwait(true);
    }

    [Fact]
    public async Task CachedHls_ParallelSegmentsReuseAdmission_SecondPlayerCannotDisplaceOrBorrowFirst()
    {
        var clients = await CreatePlaybackClientsAsync().ConfigureAwait(true);
        using var first = clients.First;
        using var second = clients.Second;
        var movie = _factory.CreateMovie();
        var original = await RegisterAsync(first, movie.Id, "player-a").ConfigureAwait(true);
        var competing = await RegisterAsync(second, movie.Id, "player-b").ConfigureAwait(true);
        var firstPath = _factory.CreateCachedHlsSegment(movie, original.PlaySessionId!, "video-device-a");
        var secondPath = _factory.CreateCachedHlsSegment(movie, competing.PlaySessionId!, "video-device-b");

        await AssertCachedSegmentAsync(first, firstPath).ConfigureAwait(true);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => AssertCachedSegmentAsync(first, firstPath))).ConfigureAwait(true);

        using (var denied = await second.GetAsync(secondPath, TestContext.Current.CancellationToken).ConfigureAwait(true))
        {
            await AssertLimitReachedAsync(denied).ConfigureAwait(true);
        }

        using (var borrowed = await second.GetAsync(firstPath, TestContext.Current.CancellationToken).ConfigureAwait(true))
        {
            Assert.Equal(HttpStatusCode.Forbidden, borrowed.StatusCode);
            Assert.False(borrowed.Headers.Contains("X-Jellyfin-Error-Code"));
        }

        await AssertCachedSegmentAsync(first, firstPath).ConfigureAwait(true);
        _factory.VerifyNoEncoderChanges();
    }

    [Theory]
    [InlineData("Videos/{0}/stream?static=true")]
    [InlineData("Videos/{0}/hls/playlist/stream.m3u8")]
    [InlineData("Videos/{0}/hls/playlist/playlist0.ts")]
    public async Task VideoAndCachedHls_AnonymousRequest_IsUnauthorized(string route)
    {
        var clients = await CreatePlaybackClientsAsync().ConfigureAwait(true);
        using var first = clients.First;
        using var second = clients.Second;
        using var anonymous = _factory.CreateClient();

        using var response = await anonymous.GetAsync(
            string.Format(CultureInfo.InvariantCulture, route, Guid.NewGuid()),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<(HttpClient First, HttpClient Second)> CreatePlaybackClientsAsync()
    {
        using var startupClient = _factory.CreateClient();
        _startupToken ??= await AuthHelper.CompleteStartupAsync(startupClient).ConfigureAwait(true);
        var users = _factory.Services.GetRequiredService<IUserManager>();
        var user = await users.CreateUserAsync($"video-quota-{Guid.NewGuid():N}").ConfigureAwait(true);
        var policy = users.GetUserDto(user).Policy!;
        policy.MaxActiveVideoStreams = 1;
        await users.UpdatePolicyAsync(user.Id, policy).ConfigureAwait(true);
        await users.ChangePassword(user.Id, "playback-test-password").ConfigureAwait(true);

        var first = await AuthenticateDeviceAsync(user.Username, "video-device-a").ConfigureAwait(true);
        try
        {
            var second = await AuthenticateDeviceAsync(user.Username, "video-device-b").ConfigureAwait(true);
            return (first, second);
        }
        catch
        {
            first.Dispose();
            throw;
        }
    }

    private async Task<HttpClient> AuthenticateDeviceAsync(string username, string deviceId)
    {
        var client = _factory.CreateClient();
        try
        {
            var header = AuthHelper.DummyAuthHeader.Replace("69420", deviceId, StringComparison.Ordinal);
            using var request = new HttpRequestMessage(HttpMethod.Post, "Users/AuthenticateByName")
            {
                Content = JsonContent.Create(
                    new AuthenticateUserByName { Username = username, Pw = "playback-test-password" },
                    options: JsonDefaults.Options)
            };
            request.Headers.Add(AuthHelper.AuthHeaderName, header);
            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var authentication = await response.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonDefaults.Options, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.NotNull(authentication);
            client.DefaultRequestHeaders.Add(AuthHelper.AuthHeaderName, header + $", Token={authentication.AccessToken}");
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    private static async Task<PlaybackInfoResponse> RegisterAsync(HttpClient client, Guid itemId, string playbackInstanceId)
    {
        using var response = await client.PostAsJsonAsync(
            $"Items/{itemId}/PlaybackInfo",
            new PlaybackInfoDto { PlaybackInstanceId = playbackInstanceId },
            JsonDefaults.Options,
            TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var playback = await response.Content.ReadFromJsonAsync<PlaybackInfoResponse>(JsonDefaults.Options, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(playback);
        Assert.NotNull(playback.PlaySessionId);
        return playback;
    }

    private static string GetStaticPath(Guid itemId, string playSessionId)
        => $"Videos/{itemId}/stream?static=true&playSessionId={playSessionId}";

    private static async Task AssertRangeAsync(HttpClient client, Guid itemId, string playSessionId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, GetStaticPath(itemId, playSessionId));
        request.Headers.Range = new RangeHeaderValue(4, 11);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
        Assert.Equal(PlaybackFactory.MediaBytes[4..12], await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    private static async Task AssertCachedSegmentAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(PlaybackFactory.MediaBytes, await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    private static async Task AssertLimitReachedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(nameof(PlaybackErrorCode.MaxActiveVideoStreamsReached), Assert.Single(response.Headers.GetValues("X-Jellyfin-Error-Code")));
        var error = await response.Content.ReadFromJsonAsync<PlaybackInfoResponse>(JsonDefaults.Options, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(PlaybackErrorCode.MaxActiveVideoStreamsReached, error!.ErrorCode);
    }

    private sealed class AuthenticationResponse
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    public sealed class PlaybackFactory : JellyfinApplicationFactory
    {
        public static readonly byte[] MediaBytes = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        private readonly string _mediaPath = Path.Combine(Path.GetTempPath(), $"jellyfin-video-quota-{Guid.NewGuid():N}");
        private readonly Mock<ITranscodeManager> _transcodes = new();
        private readonly ConcurrentDictionary<string, TranscodingJob> _jobs = new(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, TranscodingJob> _playlistJobs = new(StringComparer.Ordinal);
        private readonly ConcurrentBag<string> _cachedPaths = new();

        public Movie CreateMovie()
        {
            Directory.CreateDirectory(_mediaPath);
            var id = Guid.NewGuid();
            var path = Path.Combine(_mediaPath, $"{id:N}.mp4");
            File.WriteAllBytes(path, MediaBytes);
            var movie = new Movie { Id = id, Name = "Video playback quota test", Path = path };
            Services.GetRequiredService<ILibraryManager>().CreateItem(movie, null);
            return movie;
        }

        public void VerifyNoEncoderChanges()
        {
            _transcodes.Verify(value => value.KillTranscodingJobs(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<string, bool>>()), Times.Never);
            _transcodes.Verify(
                value => value.StartFfMpeg(
                    It.IsAny<StreamState>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<TranscodingJobType>(),
                    It.IsAny<CancellationTokenSource>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        public void RegisterTranscodingJob(Movie movie, string playSessionId)
        {
            var job = new TranscodingJob(NullLogger<TranscodingJob>.Instance)
            {
                PlaySessionId = playSessionId,
                DeviceId = "video-device-a",
                MediaSource = CreateSource(movie)
            };
            Assert.True(_jobs.TryAdd(playSessionId, job));
        }

        public string CreateCachedHlsSegment(Movie movie, string playSessionId, string deviceId)
        {
            var transcodePath = Services.GetRequiredService<IServerConfigurationManager>().GetTranscodePath();
            Directory.CreateDirectory(transcodePath);
            var playlistId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            var playlistPath = Path.Combine(transcodePath, playlistId + ".m3u8");
            var segmentId = playlistId + "0";
            var segmentPath = Path.Combine(transcodePath, segmentId + ".ts");
            File.WriteAllBytes(segmentPath, MediaBytes);
            File.WriteAllText(playlistPath, $"#EXTM3U\n#EXTINF:6,\n{segmentId}.ts\n#EXT-X-ENDLIST\n");
            _cachedPaths.Add(playlistPath);
            _cachedPaths.Add(segmentPath);
            var job = new TranscodingJob(NullLogger<TranscodingJob>.Instance)
            {
                PlaySessionId = playSessionId,
                DeviceId = deviceId,
                Path = playlistPath,
                Type = TranscodingJobType.Hls,
                MediaSource = CreateSource(movie)
            };
            Assert.True(_jobs.TryAdd(playSessionId, job));
            Assert.True(_playlistJobs.TryAdd(playlistPath, job));
            return $"Videos/{movie.Id}/hls/{playlistId}/{segmentId}.ts";
        }

        public void VerifyNoPlaybackControlChanges()
        {
            VerifyNoEncoderChanges();
            _transcodes.Verify(value => value.PingTranscodingJob(It.IsAny<string>(), It.IsAny<bool?>()), Times.Never);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                var sources = new Mock<IMediaSourceManager>();
                sources.Setup(value => value.GetPlaybackMediaSources(
                    It.IsAny<BaseItem>(),
                    It.IsAny<User>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                    .Returns((BaseItem item, User _, bool _, bool _, CancellationToken _) => Task.FromResult<IReadOnlyList<MediaSourceInfo>>([CreateSource(item)]));
                _transcodes.Setup(value => value.GetTranscodingJob(It.IsAny<string>()))
                    .Returns((string playSessionId) => _jobs.TryGetValue(playSessionId, out var job) ? job : null);
                _transcodes.Setup(value => value.GetTranscodingJob(It.IsAny<string>(), TranscodingJobType.Hls))
                    .Returns((string path, TranscodingJobType _) => _playlistJobs.TryGetValue(path, out var job) ? job : null);
                _transcodes.Setup(value => value.OnTranscodeBeginRequest(It.IsAny<string>(), TranscodingJobType.Hls))
                    .Returns((string path, TranscodingJobType _) => _playlistJobs.TryGetValue(path, out var job) ? job : null);
                services.AddSingleton(sources.Object);
                services.AddSingleton(_transcodes.Object);
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                foreach (var job in _jobs.Values)
                {
                    job.Dispose();
                }

                _jobs.Clear();
                _playlistJobs.Clear();
                foreach (var path in _cachedPaths)
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
            }

            if (disposing && Directory.Exists(_mediaPath))
            {
                Directory.Delete(_mediaPath, true);
            }
        }

        private static MediaSourceInfo CreateSource(BaseItem item)
            => new()
            {
                Id = item.Id.ToString("N", CultureInfo.InvariantCulture),
                Path = item.Path,
                Container = "mp4",
                Protocol = MediaProtocol.File,
                VideoType = VideoType.VideoFile,
                RunTimeTicks = TimeSpan.FromMinutes(1).Ticks,
                SupportsDirectPlay = true,
                SupportsDirectStream = true,
                SupportsTranscoding = true,
                DefaultAudioStreamIndex = 1,
                MediaStreams =
                [
                    new MediaStream { Index = 0, Type = MediaStreamType.Video, Codec = "h264", Width = 16, Height = 16, BitRate = 100_000, RealFrameRate = 24 },
                    new MediaStream { Index = 1, Type = MediaStreamType.Audio, Codec = "aac", SampleRate = 48_000, Channels = 2, BitRate = 128_000 }
                ]
            };
    }
}
