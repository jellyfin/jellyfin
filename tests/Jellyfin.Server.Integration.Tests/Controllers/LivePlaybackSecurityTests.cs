using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.MediaInfoDtos;
using Jellyfin.Api.Models.UserDtos;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions.Json;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

public sealed class LivePlaybackSecurityTests : IClassFixture<LivePlaybackSecurityTests.PlaybackFactory>
{
    private readonly PlaybackFactory _factory;
    private static string? _startupToken;

    public LivePlaybackSecurityTests(PlaybackFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RawInputs_RequireInternalCapabilityEvenForAnAuthenticatedUnlimitedUser(bool recording, bool authenticated)
    {
        var actors = await CreateActorsAsync(0);
        using var first = actors.First;
        using var second = actors.Second;
        using var anonymous = _factory.CreateClient();
        var item = _factory.CreateLiveItem();
        _factory.ActivateInput(item);

        using var response = await (authenticated ? first : anonymous).GetAsync(_factory.GetRawUrl(item, recording), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RawInputs_InternalCapabilityAllowsServerInputWithoutAClientToken(bool recording)
    {
        await EnsureStartupAsync();
        using var client = _factory.CreateClient();
        var item = _factory.CreateLiveItem();
        _factory.ActivateInput(item);
        var unsigned = _factory.GetRawUrl(item, recording);
        var signed = _factory.Services.GetRequiredService<IInternalLiveTvAccess>().GetInputUrl(unsigned);

        Assert.NotEqual(unsigned, signed);
        await ReadPayloadAsync(client, signed);
    }

    [Fact]
    public async Task RawInputs_CapabilityCannotBeTransferredToAnotherOpenedResource()
    {
        await EnsureStartupAsync();
        using var client = _factory.CreateClient();
        var first = _factory.CreateLiveItem();
        var second = _factory.CreateLiveItem();
        _factory.ActivateInput(first);
        _factory.ActivateInput(second);
        var signed = _factory.Services.GetRequiredService<IInternalLiveTvAccess>().GetInputUrl(_factory.GetRawUrl(first, false));
        var forged = signed.Replace(first.UniqueId, second.UniqueId, StringComparison.Ordinal);

        using var response = await client.GetAsync(forged, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OpenAndProxy_SecondDeviceIsDeniedBeforeOpeningAProvider_FirstPlayerContinues()
    {
        var actors = await CreateActorsAsync(1);
        using var first = actors.First;
        using var second = actors.Second;
        var item = _factory.CreateLiveItem();
        var firstMetadata = await MetadataAsync(first, item, "first-player");
        var opened = await OpenAsync(first, item, firstMetadata.PlaySessionId!);
        var calls = _factory.OpenCalls;
        var repeated = await OpenAsync(first, item, firstMetadata.PlaySessionId!);
        Assert.Equal(calls, _factory.OpenCalls);
        Assert.Equal(1, _factory.Consumers(item));
        Assert.Equal(opened.MediaSource.LiveStreamId, repeated.MediaSource.LiveStreamId);
        AssertPublishedProxy(opened, item);
        Assert.DoesNotContain(IInternalLiveTvAccess.TokenQueryParameter, JsonSerializer.Serialize(opened), StringComparison.Ordinal);
        Assert.DoesNotContain(IInternalLiveTvAccess.TokenQueryParameter, _factory.GetOriginalSource(item).Path, StringComparison.Ordinal);

        await ReadPayloadAsync(first, opened.MediaSource.Path);
        var competing = await MetadataAsync(second, item, "second-player");
        using var denied = await second.PostAsJsonAsync("LiveStreams/Open", OpenRequest(item, competing.PlaySessionId!), JsonDefaults.Options, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(nameof(PlaybackErrorCode.MaxActiveVideoStreamsReached), Assert.Single(denied.Headers.GetValues("X-Jellyfin-Error-Code")));
        Assert.Equal(calls, _factory.OpenCalls);
        await ReadPayloadAsync(first, opened.MediaSource.Path);
    }

    [Fact]
    public async Task Close_AnotherTokenCannotReleaseTheFirstConsumer_AndRepeatedOwnedCloseIsIdempotent()
    {
        var actors = await CreateActorsAsync(0);
        using var first = actors.First;
        using var second = actors.Second;
        var item = _factory.CreateLiveItem();
        var firstMetadata = await MetadataAsync(first, item, "first-player");
        var openedFirst = await OpenAsync(first, item, firstMetadata.PlaySessionId!);

        using (var denied = await second.PostAsync($"LiveStreams/Close?liveStreamId={item.LiveStreamId}&playSessionId={firstMetadata.PlaySessionId}", null, TestContext.Current.CancellationToken))
        {
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        }

        Assert.Equal(1, _factory.Consumers(item));
        await ReadPayloadAsync(first, openedFirst.MediaSource.Path);
        var secondMetadata = await MetadataAsync(second, item, "second-player");
        var openedSecond = await OpenAsync(second, item, secondMetadata.PlaySessionId!);
        Assert.Equal(2, _factory.Consumers(item));
        var closePath = $"LiveStreams/Close?liveStreamId={item.LiveStreamId}&playSessionId={firstMetadata.PlaySessionId}";
        using (var closed = await first.PostAsync(closePath, null, TestContext.Current.CancellationToken))
        using (var repeated = await first.PostAsync(closePath, null, TestContext.Current.CancellationToken))
        {
            Assert.Equal(HttpStatusCode.NoContent, closed.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, repeated.StatusCode);
        }

        Assert.Equal(1, _factory.Consumers(item));
        await ReadPayloadAsync(second, openedSecond.MediaSource.Path);
    }

    [Theory]
    [InlineData("Videos")]
    [InlineData("Audio")]
    public async Task LiveSource_CannotBeSelectedThroughAnUnrelatedAudioItem(string route)
    {
        var actors = await CreateActorsAsync(0);
        using var first = actors.First;
        using var second = actors.Second;
        var item = _factory.CreateLiveItem();
        var audio = _factory.CreateAudio();
        var metadata = await MetadataAsync(first, item, "player");
        var opened = await OpenAsync(first, item, metadata.PlaySessionId!);

        using var denied = await first.GetAsync($"{route}/{audio.Id}/stream?static=true&liveStreamId={item.LiveStreamId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        await ReadPayloadAsync(first, opened.MediaSource.Path);
    }

    [Fact]
    public async Task LiveSource_AnotherTokenCannotBorrowTheInputWithItsOwnEncodingAlias()
    {
        var actors = await CreateActorsAsync(0);
        using var first = actors.First;
        using var second = actors.Second;
        var item = _factory.CreateLiveItem();
        var firstMetadata = await MetadataAsync(first, item, "first-player");
        var opened = await OpenAsync(first, item, firstMetadata.PlaySessionId!);
        var otherMetadata = await MetadataAsync(second, item, "other-player");

        using var denied = await second.GetAsync($"Videos/{item.Item.Id}/stream.ts?static=true&liveStreamId={item.LiveStreamId}&playSessionId={otherMetadata.PlaySessionId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.False(denied.Headers.Contains("X-Jellyfin-Error-Code"));
        await ReadPayloadAsync(first, opened.MediaSource.Path);
    }

    [Fact]
    public async Task Metadata_QualityChangeReusesItsInputWithoutOpeningAnotherProviderConsumer()
    {
        var actors = await CreateActorsAsync(1);
        using var first = actors.First;
        using var second = actors.Second;
        var item = _factory.CreateLiveItem();
        var initial = await MetadataAsync(first, item, "same-player");
        var opened = await OpenAsync(first, item, initial.PlaySessionId!);
        await ReadPayloadAsync(first, opened.MediaSource.Path);
        var calls = _factory.OpenCalls;
        var replacement = await MetadataAsync(first, item, "same-player", item.LiveStreamId);

        Assert.NotEqual(initial.PlaySessionId, replacement.PlaySessionId);
        Assert.Equal(initial.PlaybackInstanceId, replacement.PlaybackInstanceId);
        var source = Assert.Single(replacement.MediaSources);
        Assert.False(source.RequiresOpening);
        await ReadPayloadAsync(first, source.Path);
        Assert.Equal(calls, _factory.OpenCalls);
        Assert.Equal(1, _factory.Consumers(item));
    }

    [Fact]
    public async Task Open_LegacyTokenOnlyResolvesAnAccessibleItemAndPublishesAnAuthenticatedProxy()
    {
        var actors = await CreateActorsAsync(1);
        using var first = actors.First;
        using var second = actors.Second;
        var item = _factory.CreateLiveItem();
        using var response = await first.PostAsJsonAsync("LiveStreams/Open", new OpenLiveStreamDto { OpenToken = item.OpenToken }, JsonDefaults.Options, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var opened = await response.Content.ReadFromJsonAsync<LiveStreamResponse>(JsonDefaults.Options, TestContext.Current.CancellationToken);
        Assert.NotNull(opened);
        AssertPublishedProxy(opened, item);
        await ReadPayloadAsync(first, opened.MediaSource.Path);
    }

    [Fact]
    public async Task Open_AdvertisedTokenOfAnotherItemCannotOpenTheRequestedContent()
    {
        var actors = await CreateActorsAsync(0);
        using var first = actors.First;
        using var second = actors.Second;
        var item = _factory.CreateLiveItem();
        var other = _factory.CreateLiveItem();
        var metadata = await MetadataAsync(first, item, "player");
        var calls = _factory.OpenCalls;
        var request = OpenRequest(item, metadata.PlaySessionId!);
        request.OpenToken = other.OpenToken;

        using var denied = await first.PostAsJsonAsync("LiveStreams/Open", request, JsonDefaults.Options, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal(calls, _factory.OpenCalls);
    }

    [Fact]
    public async Task Open_ProfileFailureAfterOpeningReleasesTheOwnedProviderConsumer()
    {
        var actors = await CreateActorsAsync(1);
        using var first = actors.First;
        using var second = actors.Second;
        var item = _factory.CreateLiveItem();
        var metadata = await MetadataAsync(first, item, "failed-profile");
        var request = OpenRequest(item, metadata.PlaySessionId!);
        request.DeviceProfile = new DeviceProfile();
        _factory.InvalidateOpenedAttachments(item);
        var calls = _factory.OpenCalls;

        using var response = await first.PostAsJsonAsync("LiveStreams/Open", request, JsonDefaults.Options, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(calls + 1, _factory.OpenCalls);
        Assert.Equal(0, _factory.Consumers(item));
    }

    [Fact]
    public async Task Open_ApiKeyProfileWithoutAUserReleasesTheUnmanagedProviderConsumer()
    {
        await EnsureStartupAsync();
        using var client = _factory.CreateClient();
        var authentication = _factory.Services.GetRequiredService<IAuthenticationManager>();
        var key = await authentication.CreateApiKey($"live-open-failure-{Guid.NewGuid():N}");
        client.DefaultRequestHeaders.AddAuthHeader(key.AccessToken);
        try
        {
            var item = _factory.CreateLiveItem();
            var request = new OpenLiveStreamDto { ItemId = item.Item.Id, OpenToken = item.OpenToken, DeviceProfile = new DeviceProfile() };
            var calls = _factory.OpenCalls;

            using var response = await client.PostAsJsonAsync("LiveStreams/Open", request, JsonDefaults.Options, TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(calls + 1, _factory.OpenCalls);
            Assert.Equal(0, _factory.Consumers(item));
        }
        finally
        {
            await authentication.DeleteApiKey(key.AccessToken);
        }
    }

    private async Task EnsureStartupAsync()
    {
        using var client = _factory.CreateClient();
        _startupToken ??= await AuthHelper.CompleteStartupAsync(client);
    }

    private async Task<(HttpClient First, HttpClient Second)> CreateActorsAsync(int limit)
    {
        await EnsureStartupAsync();
        var users = _factory.Services.GetRequiredService<IUserManager>();
        var user = await users.CreateUserAsync($"live-security-{Guid.NewGuid():N}");
        var policy = users.GetUserDto(user).Policy;
        policy.MaxActiveVideoStreams = limit;
        await users.UpdatePolicyAsync(user.Id, policy);
        await users.ChangePassword(user.Id, "live-test-password");
        var first = await AuthenticateAsync(user.Username, "live-device-a");
        try
        {
            return (first, await AuthenticateAsync(user.Username, "live-device-b"));
        }
        catch
        {
            first.Dispose();
            throw;
        }
    }

    private async Task<HttpClient> AuthenticateAsync(string username, string deviceId)
    {
        var client = _factory.CreateClient();
        var header = AuthHelper.DummyAuthHeader.Replace("69420", deviceId, StringComparison.Ordinal);
        using var request = new HttpRequestMessage(HttpMethod.Post, "Users/AuthenticateByName")
        {
            Content = JsonContent.Create(new AuthenticateUserByName { Username = username, Pw = "live-test-password" }, options: JsonDefaults.Options)
        };
        request.Headers.Add(AuthHelper.AuthHeaderName, header);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authentication = await response.Content.ReadFromJsonAsync<AuthenticationResult>(JsonDefaults.Options, TestContext.Current.CancellationToken);
        Assert.NotNull(authentication);
        client.DefaultRequestHeaders.Add(AuthHelper.AuthHeaderName, header + $", Token={authentication.AccessToken}");
        return client;
    }

    private static async Task<PlaybackInfoResponse> MetadataAsync(HttpClient client, LiveItem item, string instance, string? liveStreamId = null)
    {
        using var response = await client.PostAsJsonAsync(
            $"Items/{item.Item.Id}/PlaybackInfo",
            new PlaybackInfoDto { PlaybackInstanceId = instance, LiveStreamId = liveStreamId },
            JsonDefaults.Options,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var metadata = await response.Content.ReadFromJsonAsync<PlaybackInfoResponse>(JsonDefaults.Options, TestContext.Current.CancellationToken);
        Assert.NotNull(metadata);
        Assert.NotNull(metadata.PlaySessionId);
        return metadata;
    }

    private static OpenLiveStreamDto OpenRequest(LiveItem item, string alias)
        => new() { ItemId = item.Item.Id, OpenToken = item.OpenToken, PlaySessionId = alias };

    private static async Task<LiveStreamResponse> OpenAsync(HttpClient client, LiveItem item, string alias)
    {
        using var response = await client.PostAsJsonAsync("LiveStreams/Open", OpenRequest(item, alias), JsonDefaults.Options, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var opened = await response.Content.ReadFromJsonAsync<LiveStreamResponse>(JsonDefaults.Options, TestContext.Current.CancellationToken);
        Assert.NotNull(opened);
        return opened;
    }

    private static void AssertPublishedProxy(LiveStreamResponse response, LiveItem item)
    {
        Assert.Contains($"/Videos/{item.Item.Id:N}/stream", response.MediaSource.Path, StringComparison.Ordinal);
        Assert.Contains("ApiKey=", response.MediaSource.Path, StringComparison.Ordinal);
        Assert.Contains("PlaySessionId=", response.MediaSource.Path, StringComparison.Ordinal);
        Assert.DoesNotContain("/LiveTv/LiveStreamFiles/", response.MediaSource.Path, StringComparison.Ordinal);
        Assert.DoesNotContain(IInternalLiveTvAccess.TokenQueryParameter, response.MediaSource.Path, StringComparison.Ordinal);
    }

    private static async Task ReadPayloadAsync(HttpClient client, string path)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation.Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellation.Token);
        var bytes = new byte[PlaybackFactory.Payload.Length];
        await stream.ReadExactlyAsync(bytes, cancellation.Token);
        Assert.Equal(PlaybackFactory.Payload, bytes);
        await cancellation.CancelAsync();
    }

    private sealed class AuthenticationResult
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    public sealed record LiveItem(Movie Item, string OpenToken, string LiveStreamId, string UniqueId);

    public sealed class PlaybackFactory : JellyfinApplicationFactory
    {
        public static readonly byte[] Payload = Enumerable.Range(0, 16).Select(value => (byte)value).ToArray();
        private readonly ConcurrentDictionary<Guid, Input> _inputs = new();
        private readonly string _mediaPath = Path.Combine(Path.GetTempPath(), $"jellyfin-live-security-{Guid.NewGuid():N}");
        private int _openCalls;

        public int OpenCalls => Volatile.Read(ref _openCalls);

        public LiveItem CreateLiveItem()
        {
            Directory.CreateDirectory(_mediaPath);
            var id = Guid.NewGuid();
            var path = Path.Combine(_mediaPath, id.ToString("N", CultureInfo.InvariantCulture) + ".ts");
            File.WriteAllBytes(path, Payload);
            var movie = new Movie { Id = id, Name = "Live input security fixture", Path = path };
            Services.GetRequiredService<ILibraryManager>().CreateItem(movie, null);
            var item = new LiveItem(movie, $"channel_{id:N}", $"live_{id:N}", $"raw_{id:N}");
            Assert.True(_inputs.TryAdd(id, new Input(item, CreateOpenedSource(item))));
            return item;
        }

        public Audio CreateAudio()
        {
            var item = new Audio { Id = Guid.NewGuid(), Name = "Unrelated audio item", Path = Path.Combine(_mediaPath, "audio.mp3") };
            Services.GetRequiredService<ILibraryManager>().CreateItem(item, null);
            return item;
        }

        public void ActivateInput(LiveItem item) => _inputs[item.Item.Id].Activate();

        public void InvalidateOpenedAttachments(LiveItem item) => _inputs[item.Item.Id].OpenedAttachmentsValid = false;

        public int Consumers(LiveItem item) => _inputs[item.Item.Id].Consumers;

        public MediaSourceInfo GetOriginalSource(LiveItem item) => _inputs[item.Item.Id].Source;

        public string GetRawUrl(LiveItem item, bool recording)
        {
            var local = Services.GetRequiredService<IServerApplicationHost>().GetApiUrlForLocalAccess().TrimEnd('/');
            return recording
                ? local + $"/LiveTv/LiveRecordings/{item.UniqueId}/stream"
                : local + $"/LiveTv/LiveStreamFiles/{item.UniqueId}/stream.ts";
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                var sources = new Mock<IMediaSourceManager>();
                sources.Setup(value => value.GetPlaybackMediaSources(It.IsAny<BaseItem>(), It.IsAny<User>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                    .Returns((BaseItem item, User _, bool _, bool _, CancellationToken _) => Task.FromResult<IReadOnlyList<MediaSourceInfo>>([CreateUnopenedSource(item)]));
                sources.Setup(value => value.OpenLiveStream(It.IsAny<LiveStreamRequest>(), It.IsAny<CancellationToken>()))
                    .Returns((LiveStreamRequest request, CancellationToken _) =>
                    {
                        var input = _inputs[request.ItemId];
                        Interlocked.Increment(ref _openCalls);
                        input.AddConsumer();
                        var source = Clone(input.Source);
                        if (!input.OpenedAttachmentsValid)
                        {
                            // Simulate invalid provider metadata reaching profile mapping after Open.
                            source.MediaAttachments = null!;
                        }

                        return Task.FromResult(new LiveStreamResponse(source));
                    });
                sources.Setup(value => value.GetLiveStreamInfo(It.IsAny<string>()))
                    .Returns((string id) => FindInput(id)?.Stream.Object!);
                sources.Setup(value => value.GetLiveStreamInfoByUniqueId(It.IsAny<string>()))
                    .Returns((string id) => _inputs.Values.FirstOrDefault(input => input.Consumers > 0 && string.Equals(input.Item.UniqueId, id, StringComparison.OrdinalIgnoreCase))?.Stream.Object!);
                sources.Setup(value => value.GetLiveStream(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns((string id, CancellationToken _) => Task.FromResult(FindInput(id)!.Source));
                sources.Setup(value => value.GetLiveStreamWithDirectStreamProvider(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns((string id, CancellationToken _) =>
                    {
                        var input = FindInput(id)!;
                        return Task.FromResult(Tuple.Create(input.Source, input.Provider.Object));
                    });
                sources.Setup(value => value.CloseLiveStream(It.IsAny<string>()))
                    .Returns((string id) =>
                    {
                        var input = FindInput(id);
                        if (input is not null)
                        {
                            input.RemoveConsumer();
                        }

                        return Task.CompletedTask;
                    });
                var recordings = new Mock<IRecordingsManager>();
                recordings.Setup(value => value.CreateRecordingFolders()).Returns(Task.CompletedTask);
                recordings.Setup(value => value.GetActiveRecordingPath(It.IsAny<string>()))
                    .Returns((string id) => _inputs.Values.FirstOrDefault(input => string.Equals(input.Item.UniqueId, id, StringComparison.OrdinalIgnoreCase))?.Item.Item.Path);
                services.AddSingleton(sources.Object);
                services.AddSingleton(recordings.Object);
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && Directory.Exists(_mediaPath))
            {
                Directory.Delete(_mediaPath, true);
            }
        }

        private Input? FindInput(string id)
            => _inputs.Values.FirstOrDefault(input => input.Consumers > 0 && string.Equals(input.Item.LiveStreamId, id, StringComparison.OrdinalIgnoreCase));

        private MediaSourceInfo CreateUnopenedSource(BaseItem item)
        {
            if (_inputs.TryGetValue(item.Id, out var input))
            {
                var source = Clone(input.Source);
                source.LiveStreamId = null;
                source.RequiresOpening = true;
                source.Path = Services.GetRequiredService<IServerApplicationHost>().GetApiUrlForLocalAccess();
                return source;
            }

            return new MediaSourceInfo { Id = item.Id.ToString("N", CultureInfo.InvariantCulture), Path = item.Path, Protocol = MediaProtocol.File, Container = "mp3" };
        }

        private MediaSourceInfo CreateOpenedSource(LiveItem item)
            => new()
            {
                Id = item.Item.Id.ToString("N", CultureInfo.InvariantCulture),
                OpenToken = item.OpenToken,
                LiveStreamId = item.LiveStreamId,
                Path = GetRawUrl(item, false),
                EncoderPath = GetRawUrl(item, false),
                EncoderProtocol = MediaProtocol.Http,
                Protocol = MediaProtocol.Http,
                Container = "ts",
                VideoType = VideoType.VideoFile,
                RunTimeTicks = TimeSpan.FromSeconds(4).Ticks,
                SupportsDirectPlay = true,
                SupportsDirectStream = true,
                SupportsTranscoding = true,
                RequiresClosing = true,
                DefaultAudioStreamIndex = 1,
                MediaStreams =
                [
                    new MediaStream { Index = 0, Type = MediaStreamType.Video, Codec = "h264", Width = 16, Height = 16, BitRate = 100_000, RealFrameRate = 24 },
                    new MediaStream { Index = 1, Type = MediaStreamType.Audio, Codec = "aac", SampleRate = 48_000, Channels = 2, BitRate = 128_000 }
                ]
            };

        private static MediaSourceInfo Clone(MediaSourceInfo source)
            => JsonSerializer.Deserialize<MediaSourceInfo>(JsonSerializer.SerializeToUtf8Bytes(source))!;

        private sealed class Input
        {
            private int _consumers;

            public Input(LiveItem item, MediaSourceInfo source)
            {
                Item = item;
                Source = source;
                Stream = new Mock<ILiveStream>();
                Provider = Stream.As<IDirectStreamProvider>();
                Stream.SetupGet(value => value.MediaSource).Returns(Source);
                Stream.SetupGet(value => value.UniqueId).Returns(item.UniqueId);
                Stream.Setup(value => value.GetStream()).Returns(() => new MemoryStream(Payload, false));
                Provider.Setup(value => value.GetStream()).Returns(() => new MemoryStream(Payload, false));
            }

            public int Consumers => Volatile.Read(ref _consumers);

            public bool OpenedAttachmentsValid { get; set; } = true;

            public LiveItem Item { get; }

            public MediaSourceInfo Source { get; }

            public Mock<ILiveStream> Stream { get; }

            public Mock<IDirectStreamProvider> Provider { get; }

            public void Activate() => Interlocked.Exchange(ref _consumers, 1);

            public void AddConsumer() => Interlocked.Increment(ref _consumers);

            public void RemoveConsumer() => Interlocked.Decrement(ref _consumers);
        }
    }
}
