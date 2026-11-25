using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.MediaInfoDtos;
using Jellyfin.Api.Models.UserDtos;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

// Opt in with JELLYFIN_REAL_PLAYBACK_TESTS=1 and an independently generated four-second
// H.264/AAC reference file. Only the source metadata manager is controlled: authentication,
// policy, database, admission registry, encoder capabilities and FFmpeg jobs are real.
public sealed class VideoPlaybackRealEncoderTests : IClassFixture<VideoPlaybackRealEncoderTests.RealEncoderFactory>
{
    private static readonly JsonSerializerOptions _evidenceJsonOptions = new() { WriteIndented = true };
    private static string? _startupToken;
    private readonly RealEncoderFactory _factory;
    private readonly ITestOutputHelper _output;

    public VideoPlaybackRealEncoderTests(RealEncoderFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    public static bool IsEnabled => string.Equals(Environment.GetEnvironmentVariable("JELLYFIN_REAL_PLAYBACK_TESTS"), "1", StringComparison.Ordinal);

    [Theory(Skip = "Requires the opt-in FFmpeg reference clip and real encoder environment.", SkipUnless = nameof(IsEnabled))]
    [InlineData("direct-play")]
    [InlineData("direct-stream")]
    [InlineData("progressive")]
    [InlineData("hls")]
    public async Task PlayableOutput_SecondDeviceDenied_FirstSurvivesPauseSeekAndEncodingChange(string mode)
    {
        var clients = await CreateClientsAsync(1).ConfigureAwait(true);
        using var first = clients.First;
        using var second = clients.Second;
        var movie = _factory.CreateMovie();
        var firstPlayback = await RegisterAsync(first, movie.Id, "player-a").ConfigureAwait(true);
        var secondPlayback = await RegisterAsync(second, movie.Id, "player-b").ConfigureAwait(true);
        var artifactDirectory = _factory.CreateArtifactDirectory(mode);
        var path = GetPath(movie.Id, firstPlayback.PlaySessionId!, mode);
        string outputPath;
        if (string.Equals(mode, "hls", StringComparison.Ordinal))
        {
            outputPath = await DownloadHlsAsync(first, path, artifactDirectory).ConfigureAwait(true);
        }
        else
        {
            using var response = await first.GetAsync(path, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            outputPath = Path.Combine(artifactDirectory, string.Equals(mode, "direct-play", StringComparison.Ordinal) ? "server.mp4" : "server.ts");
            await File.WriteAllBytesAsync(outputPath, await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken).ConfigureAwait(true), TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        await DecodeAsync(outputPath, artifactDirectory).ConfigureAwait(true);
        if (string.Equals(mode, "direct-play", StringComparison.Ordinal))
        {
            Assert.Equal(SHA256.HashData(await File.ReadAllBytesAsync(_factory.SourcePath, TestContext.Current.CancellationToken).ConfigureAwait(true)), SHA256.HashData(await File.ReadAllBytesAsync(outputPath, TestContext.Current.CancellationToken).ConfigureAwait(true)));
        }

        using (var denied = await second.GetAsync(GetPath(movie.Id, secondPlayback.PlaySessionId!, mode), TestContext.Current.CancellationToken).ConfigureAwait(true))
        {
            AssertLimitReached(denied);
        }

        using (var paused = await first.PostAsJsonAsync(
            "Sessions/Playing/Progress",
            new PlaybackProgressInfo { ItemId = movie.Id, PlaySessionId = firstPlayback.PlaySessionId, IsPaused = true, CanSeek = true, PositionTicks = TimeSpan.FromSeconds(1).Ticks },
            JsonDefaults.Options,
            TestContext.Current.CancellationToken).ConfigureAwait(true))
        {
            Assert.Equal(HttpStatusCode.NoContent, paused.StatusCode);
        }

        using (var stillDenied = await second.GetAsync(GetPath(movie.Id, secondPlayback.PlaySessionId!, "direct-play"), TestContext.Current.CancellationToken).ConfigureAwait(true))
        {
            AssertLimitReached(stillDenied);
        }

        var replacement = await RegisterAsync(first, movie.Id, "player-a").ConfigureAwait(true);
        if (mode is "progressive" or "hls")
        {
            var qualityDirectory = _factory.CreateArtifactDirectory(mode + "-quality-change");
            var qualityPath = GetPath(movie.Id, replacement.PlaySessionId!, mode)
                .Replace("width=160&height=90", "width=128&height=72", StringComparison.Ordinal)
                .Replace("videoBitRate=180000", "videoBitRate=120000", StringComparison.Ordinal);
            string qualityOutput;
            if (mode == "hls")
            {
                qualityOutput = await DownloadHlsAsync(first, qualityPath, qualityDirectory).ConfigureAwait(true);
            }
            else
            {
                using var qualityResponse = await first.GetAsync(qualityPath, TestContext.Current.CancellationToken).ConfigureAwait(true);
                Assert.Equal(HttpStatusCode.OK, qualityResponse.StatusCode);
                qualityOutput = Path.Combine(qualityDirectory, "server.ts");
                await File.WriteAllBytesAsync(qualityOutput, await qualityResponse.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken).ConfigureAwait(true), TestContext.Current.CancellationToken).ConfigureAwait(true);
            }

            await DecodeAsync(qualityOutput, qualityDirectory).ConfigureAwait(true);
            using var oldStop = await first.PostAsJsonAsync(
                "Sessions/Playing/Stopped",
                new PlaybackStopInfo { ItemId = movie.Id, PlaySessionId = firstPlayback.PlaySessionId },
                JsonDefaults.Options,
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.Equal(HttpStatusCode.NoContent, oldStop.StatusCode);
            using var stillOccupied = await second.GetAsync(GetPath(movie.Id, secondPlayback.PlaySessionId!, mode), TestContext.Current.CancellationToken).ConfigureAwait(true);
            AssertLimitReached(stillOccupied);
        }

        await AssertRangeAsync(first, movie.Id, replacement.PlaySessionId!).ConfigureAwait(true);
        using (var stopped = await first.PostAsJsonAsync(
            "Sessions/Playing/Stopped",
            new PlaybackStopInfo { ItemId = movie.Id, PlaySessionId = replacement.PlaySessionId },
            JsonDefaults.Options,
            TestContext.Current.CancellationToken).ConfigureAwait(true))
        {
            Assert.Equal(HttpStatusCode.NoContent, stopped.StatusCode);
        }

        using var admitted = await second.GetAsync(GetPath(movie.Id, secondPlayback.PlaySessionId!, "direct-play"), TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, admitted.StatusCode);
        _output.WriteLine($"Real {mode} output and FFmpeg decode evidence: {artifactDirectory}");
    }

    [Fact(Skip = "Requires the opt-in FFmpeg reference clip and real encoder environment.", SkipUnless = nameof(IsEnabled))]
    public async Task UnlimitedPolicy_TwoAuthenticatedDevicesReceivePlayableReferenceConcurrently()
    {
        var clients = await CreateClientsAsync(0).ConfigureAwait(true);
        using var first = clients.First;
        using var second = clients.Second;
        var movie = _factory.CreateMovie();
        var firstPlayback = await RegisterAsync(first, movie.Id, "player-a").ConfigureAwait(true);
        var secondPlayback = await RegisterAsync(second, movie.Id, "player-b").ConfigureAwait(true);
        var responses = await Task.WhenAll(
            first.GetAsync(GetPath(movie.Id, firstPlayback.PlaySessionId!, "direct-play"), TestContext.Current.CancellationToken),
            second.GetAsync(GetPath(movie.Id, secondPlayback.PlaySessionId!, "direct-play"), TestContext.Current.CancellationToken)).ConfigureAwait(true);
        try
        {
            foreach (var response in responses)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(await File.ReadAllBytesAsync(_factory.SourcePath, TestContext.Current.CancellationToken).ConfigureAwait(true), await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
            }
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        await AssertRangeAsync(first, movie.Id, firstPlayback.PlaySessionId!).ConfigureAwait(true);
        await AssertRangeAsync(second, movie.Id, secondPlayback.PlaySessionId!).ConfigureAwait(true);
    }

    [Fact(Skip = "Requires the opt-in FFmpeg reference clip and real encoder environment.", SkipUnless = nameof(IsEnabled))]
    public async Task AudioStaticRoute_VideoSourceCannotBypassQuotaOrBorrowAnotherOwnerAlias()
    {
        var clients = await CreateClientsAsync(1).ConfigureAwait(true);
        using var first = clients.First;
        using var second = clients.Second;
        var movie = _factory.CreateMovie();
        var firstPlayback = await RegisterAsync(first, movie.Id, "player-a").ConfigureAwait(true);
        var secondPlayback = await RegisterAsync(second, movie.Id, "player-b").ConfigureAwait(true);
        await AssertRangeAsync(first, movie.Id, firstPlayback.PlaySessionId!).ConfigureAwait(true);
        using (var denied = await second.GetAsync($"Audio/{movie.Id}/stream?static=true&playSessionId={secondPlayback.PlaySessionId}", TestContext.Current.CancellationToken).ConfigureAwait(true))
        {
            AssertLimitReached(denied);
        }

        using (var borrowed = await second.GetAsync($"Audio/{movie.Id}/stream?static=true&playSessionId={firstPlayback.PlaySessionId}", TestContext.Current.CancellationToken).ConfigureAwait(true))
        {
            Assert.Equal(HttpStatusCode.Forbidden, borrowed.StatusCode);
            Assert.False(borrowed.Headers.Contains("X-Jellyfin-Error-Code"));
        }

        await AssertRangeAsync(first, movie.Id, firstPlayback.PlaySessionId!).ConfigureAwait(true);
    }

    private async Task<(HttpClient First, HttpClient Second)> CreateClientsAsync(int limit)
    {
        Assert.True(File.Exists(_factory.SourcePath), "Set JELLYFIN_REAL_VIDEO_SOURCE to the generated four-second H.264/AAC reference clip.");
        using var startup = _factory.CreateClient();
        _startupToken ??= await AuthHelper.CompleteStartupAsync(startup).ConfigureAwait(true);
        var encoder = _factory.Services.GetRequiredService<IMediaEncoder>();
        Assert.False(string.IsNullOrEmpty(encoder.EncoderPath));
        var configuration = _factory.Services.GetRequiredService<IServerConfigurationManager>();
        var encoding = configuration.GetEncodingOptions();
        encoding.EncodingThreadCount = 1;
        encoding.HardwareAccelerationType = HardwareAccelerationType.none;
        encoding.EnableHardwareEncoding = false;
        encoding.EnableThrottling = false;
        encoding.EnableSegmentDeletion = false;
        configuration.SaveConfiguration("encoding", encoding);
        var users = _factory.Services.GetRequiredService<IUserManager>();
        var user = await users.CreateUserAsync($"real-video-{Guid.NewGuid():N}").ConfigureAwait(true);
        var policy = users.GetUserDto(user).Policy!;
        policy.MaxActiveVideoStreams = limit;
        await users.UpdatePolicyAsync(user.Id, policy).ConfigureAwait(true);
        await users.ChangePassword(user.Id, "real-video-test-password").ConfigureAwait(true);
        return (await AuthenticateAsync(user.Username, "real-video-a").ConfigureAwait(true), await AuthenticateAsync(user.Username, "real-video-b").ConfigureAwait(true));
    }

    private async Task<HttpClient> AuthenticateAsync(string username, string deviceId)
    {
        var client = _factory.CreateClient();
        var header = AuthHelper.DummyAuthHeader.Replace("69420", deviceId, StringComparison.Ordinal);
        using var request = new HttpRequestMessage(HttpMethod.Post, "Users/AuthenticateByName")
        {
            Content = JsonContent.Create(new AuthenticateUserByName { Username = username, Pw = "real-video-test-password" }, options: JsonDefaults.Options)
        };
        request.Headers.Add(AuthHelper.AuthHeaderName, header);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authentication = await response.Content.ReadFromJsonAsync<AuthenticationResult>(JsonDefaults.Options, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(authentication);
        client.DefaultRequestHeaders.Add(AuthHelper.AuthHeaderName, header + $", Token={authentication.AccessToken}");
        return client;
    }

    private static async Task<PlaybackInfoResponse> RegisterAsync(HttpClient client, Guid itemId, string playbackInstanceId)
    {
        using var response = await client.PostAsJsonAsync($"Items/{itemId}/PlaybackInfo", new PlaybackInfoDto { PlaybackInstanceId = playbackInstanceId }, JsonDefaults.Options, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PlaybackInfoResponse>(JsonDefaults.Options, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(result);
        Assert.NotNull(result.PlaySessionId);
        return result;
    }

    private static string GetPath(Guid itemId, string playSessionId, string mode)
    {
        var scope = $"playSessionId={playSessionId}&deviceId=real-video-a&cpuCoreLimit=1";
        return mode switch
        {
            "direct-play" => $"Videos/{itemId}/stream?static=true&{scope}",
            "direct-stream" => $"Videos/{itemId}/stream?static=false&container=ts&videoCodec=copy&audioCodec=copy&{scope}",
            "progressive" => $"Videos/{itemId}/stream?static=false&container=ts&videoCodec=h264&audioCodec=aac&videoBitRate=180000&audioBitRate=64000&width=160&height=90&allowVideoStreamCopy=false&allowAudioStreamCopy=false&{scope}",
            _ => $"Videos/{itemId}/main.m3u8?videoCodec=h264&audioCodec=aac&videoBitRate=180000&audioBitRate=64000&width=160&height=90&allowVideoStreamCopy=false&allowAudioStreamCopy=false&segmentContainer=ts&segmentLength=2&minSegments=1&{scope}"
        };
    }

    private static async Task AssertRangeAsync(HttpClient client, Guid itemId, string playSessionId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, GetPath(itemId, playSessionId, "direct-play"));
        request.Headers.Range = new RangeHeaderValue(512, 2047);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);
        Assert.Equal(1536, (await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).Length);
    }

    private static void AssertLimitReached(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(nameof(PlaybackErrorCode.MaxActiveVideoStreamsReached), Assert.Single(response.Headers.GetValues("X-Jellyfin-Error-Code")));
    }

    private static async Task<string> DownloadHlsAsync(HttpClient client, string path, string directory)
    {
        using var playlistResponse = await client.GetAsync(path, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(HttpStatusCode.OK, playlistResponse.StatusCode);
        var playlist = await playlistResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var lines = playlist.Split('\n');
        var segments = new List<Uri>();
        for (var index = 0; index < lines.Length; index++)
        {
            if (!string.IsNullOrWhiteSpace(lines[index]) && !lines[index].StartsWith('#'))
            {
                segments.Add(new Uri(playlistResponse.RequestMessage!.RequestUri!, lines[index].Trim()));
                lines[index] = $"segment-{segments.Count - 1}.ts";
            }
        }

        Assert.NotEmpty(segments);
        for (var index = 0; index < segments.Count; index++)
        {
            await File.WriteAllBytesAsync(Path.Combine(directory, $"segment-{index}.ts"), await client.GetByteArrayAsync(segments[index], TestContext.Current.CancellationToken).ConfigureAwait(true), TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        var cached = await Task.WhenAll(Enumerable.Range(0, 8).Select(index => client.GetByteArrayAsync(segments[index % segments.Count], TestContext.Current.CancellationToken))).ConfigureAwait(true);
        Assert.All(cached, bytes => Assert.NotEmpty(bytes));
        var localPlaylist = Path.Combine(directory, "server.m3u8");
        await File.WriteAllTextAsync(localPlaylist, string.Join('\n', lines), new UTF8Encoding(false), TestContext.Current.CancellationToken).ConfigureAwait(true);
        await File.WriteAllTextAsync(Path.Combine(directory, "server-original.m3u8"), playlist, new UTF8Encoding(false), TestContext.Current.CancellationToken).ConfigureAwait(true);
        return localPlaylist;
    }

    private static async Task DecodeAsync(string path, string directory)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Environment.GetEnvironmentVariable("JELLYFIN_REAL_FFMPEG") ?? "/usr/bin/ffmpeg",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        string[] arguments = ["-hide_banner", "-v", "error", "-threads", "1", "-i", path, "-map", "0:v:0", "-map", "0:a:0", "-f", "null", "-"];
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        var elapsed = Stopwatch.StartNew();
        Assert.True(process.Start());
        var output = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var error = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = new { process.StartInfo.FileName, Arguments = arguments, process.ExitCode, ElapsedMilliseconds = elapsed.Elapsed.TotalMilliseconds, StandardOutput = await output.ConfigureAwait(true), StandardError = await error.ConfigureAwait(true) };
        await File.WriteAllTextAsync(Path.Combine(directory, "decode.json"), JsonSerializer.Serialize(result, _evidenceJsonOptions), TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(0, process.ExitCode);
    }

    public sealed class RealEncoderFactory : JellyfinApplicationFactory
    {
        public RealEncoderFactory()
            : this(Path.Combine(Environment.GetEnvironmentVariable("JELLYFIN_REAL_PLAYBACK_EVIDENCE") ?? Path.GetTempPath(), $"real-playback-{Guid.NewGuid():N}"))
        {
        }

        private RealEncoderFactory(string evidenceDirectory)
            : base(Path.Combine(evidenceDirectory, "server"))
        {
            EvidenceDirectory = evidenceDirectory;
        }

        public string SourcePath => Environment.GetEnvironmentVariable("JELLYFIN_REAL_VIDEO_SOURCE") ?? Path.Combine(AppContext.BaseDirectory, "synthetic-h264-aac-320x180.mp4");

        public string EvidenceDirectory { get; }

        public Movie CreateMovie()
        {
            var movie = new Movie { Id = Guid.NewGuid(), Name = "Self-generated FFmpeg playback reference", Path = SourcePath, RunTimeTicks = TimeSpan.FromSeconds(4).Ticks };
            Services.GetRequiredService<ILibraryManager>().CreateItem(movie, null);
            return movie;
        }

        public string CreateArtifactDirectory(string mode)
        {
            var path = Path.Combine(EvidenceDirectory, mode + "-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(path);
            return path;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FFmpeg:novalidation"] = "false",
                ["ffmpeg"] = Environment.GetEnvironmentVariable("JELLYFIN_REAL_FFMPEG") ?? "/usr/bin/ffmpeg"
            }));
            builder.ConfigureTestServices(services =>
            {
                var sources = new Mock<IMediaSourceManager>();
                sources.Setup(value => value.GetPlaybackMediaSources(It.IsAny<BaseItem>(), It.IsAny<User>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                    .Returns((BaseItem item, User _, bool _, bool _, CancellationToken _) => Task.FromResult<IReadOnlyList<MediaSourceInfo>>([CreateSource(item)]));
                services.AddSingleton(sources.Object);
            });
        }

        private static MediaSourceInfo CreateSource(BaseItem item)
            => new()
            {
                Id = item.Id.ToString("N", CultureInfo.InvariantCulture),
                Path = item.Path,
                Container = "mp4",
                Protocol = MediaProtocol.File,
                VideoType = VideoType.VideoFile,
                RunTimeTicks = TimeSpan.FromSeconds(4).Ticks,
                Bitrate = 690_402,
                Size = 345_201,
                SupportsDirectPlay = true,
                SupportsDirectStream = true,
                SupportsTranscoding = true,
                DefaultAudioStreamIndex = 1,
                MediaStreams =
                [
                    new MediaStream { Index = 0, Type = MediaStreamType.Video, Codec = "h264", Profile = "Constrained Baseline", Level = 30, Width = 320, Height = 180, BitRate = 618_390, RealFrameRate = 24, AverageFrameRate = 24, PixelFormat = "yuv420p", BitDepth = 8 },
                    new MediaStream { Index = 1, Type = MediaStreamType.Audio, Codec = "aac", Profile = "LC", SampleRate = 48_000, Channels = 1, BitRate = 64_796 }
                ]
            };
    }
}
