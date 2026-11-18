using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.MediaInfoDtos;
using Jellyfin.Data.Queries;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Extensions.Json;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

public sealed class VideoPlaybackMediaInfoTests : IClassFixture<VideoPlaybackMediaInfoTests.PlaybackFactory>
{
    private readonly PlaybackFactory _factory;
    private static string? _accessToken;

    public VideoPlaybackMediaInfoTests(PlaybackFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PlaybackInfo_NewEncodingRetainsLogicalPlaybackAndDoesNotReserveQuota()
    {
        using var client = await CreateClientAsync();
        var movie = CreateMovie();
        var first = await PostPlaybackInfoAsync(client, movie.Id, "first-player");
        var replacement = await PostPlaybackInfoAsync(client, movie.Id, "first-player");
        var second = await PostPlaybackInfoAsync(client, movie.Id, "second-player");

        Assert.Equal("first-player", first.PlaybackInstanceId);
        Assert.Equal(first.PlaybackInstanceId, replacement.PlaybackInstanceId);
        Assert.NotEqual(first.PlaySessionId, replacement.PlaySessionId);
        var coordinator = _factory.Services.GetRequiredService<IVideoPlaybackCoordinator>();
        var firstOwner = await ResolveOwnerAsync(client, movie.Id, first.PlaySessionId!);
        var replacementOwner = await ResolveOwnerAsync(client, movie.Id, replacement.PlaySessionId!);
        var secondOwner = await ResolveOwnerAsync(client, movie.Id, second.PlaySessionId!);
        Assert.Equal(firstOwner, replacementOwner);

        Assert.Equal(VideoPlaybackStatus.Success, coordinator.TryAcquire(firstOwner, first.PlaySessionId!, 1, out var request));
        using (request)
        {
            Assert.Equal(VideoPlaybackStatus.Success, coordinator.TryAcquire(replacementOwner, replacement.PlaySessionId!, 1, out var continuation));
            using (continuation)
            {
                Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(secondOwner, second.PlaySessionId!, 1, out var denied));
                Assert.Null(denied);
            }
        }
    }

    [Fact]
    public async Task PlaybackInfo_LegacyRequestsKeepDistinctEncodingIdsAndStableFallback()
    {
        using var client = await CreateClientAsync();
        var movie = CreateMovie();
        var first = await GetPlaybackInfoAsync(client, movie.Id);
        var second = await GetPlaybackInfoAsync(client, movie.Id);

        Assert.Null(first.ErrorCode);
        Assert.NotEmpty(first.MediaSources);
        Assert.NotNull(first.PlaySessionId);
        Assert.NotEqual(first.PlaySessionId, second.PlaySessionId);
        Assert.NotNull(first.PlaybackInstanceId);
        Assert.Equal(first.PlaybackInstanceId, second.PlaybackInstanceId);
        Assert.Equal(await ResolveOwnerAsync(client, movie.Id, first.PlaySessionId!), await ResolveOwnerAsync(client, movie.Id, second.PlaySessionId!));
    }

    [Fact]
    public async Task PlaybackInfo_QueryPlaybackInstanceIdTakesPrecedenceOverBody()
    {
        using var client = await CreateClientAsync();
        var movie = CreateMovie();
        using var response = await client.PostAsJsonAsync(
            $"Items/{movie.Id}/PlaybackInfo?playbackInstanceId=query-player",
            new PlaybackInfoDto { PlaybackInstanceId = "body-player" },
            JsonDefaults.Options,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var info = await response.Content.ReadFromJsonAsync<PlaybackInfoResponse>(JsonDefaults.Options, TestContext.Current.CancellationToken);
        Assert.Equal("query-player", info!.PlaybackInstanceId);
    }

    [Fact]
    public async Task PlaybackInfo_AdminUserOverrideDoesNotChangeAdmissionOwner()
    {
        using var client = await CreateClientAsync();
        var authenticatedUser = await AuthHelper.GetUserDtoAsync(client);
        var otherUser = await _factory.Services.GetRequiredService<IUserManager>().CreateUserAsync($"playback-owner-{Guid.NewGuid():N}");
        var movie = CreateMovie();
        using var response = await client.PostAsJsonAsync(
            $"Items/{movie.Id}/PlaybackInfo",
            new PlaybackInfoDto { UserId = otherUser.Id, PlaybackInstanceId = "admin-owner-check" },
            JsonDefaults.Options,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var info = await response.Content.ReadFromJsonAsync<PlaybackInfoResponse>(JsonDefaults.Options, TestContext.Current.CancellationToken);
        var owner = await ResolveOwnerAsync(client, movie.Id, info!.PlaySessionId!);
        Assert.Equal(authenticatedUser.Id, owner.UserId);
        Assert.NotEqual(otherUser.Id, owner.UserId);
    }

    [Fact]
    public async Task PlaybackInfo_SameLogicalIdCannotBeReboundToAnotherItem()
    {
        using var client = await CreateClientAsync();
        var firstMovie = CreateMovie();
        var secondMovie = CreateMovie();
        var logicalId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var first = await PostPlaybackInfoAsync(client, firstMovie.Id, logicalId);

        using var response = await client.PostAsJsonAsync(
            $"Items/{secondMovie.Id}/PlaybackInfo",
            new PlaybackInfoDto { PlaybackInstanceId = logicalId },
            JsonDefaults.Options,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(logicalId, (await ResolveOwnerAsync(client, firstMovie.Id, first.PlaySessionId!)).PlaybackInstanceId);
    }

    [Fact]
    public async Task PlaybackInfo_SameLogicalIdCannotBeReboundToAnotherAuthenticatedDevice()
    {
        using var client = await CreateClientAsync();
        var movie = CreateMovie();
        var logicalId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var first = await PostPlaybackInfoAsync(client, movie.Id, logicalId);
        var user = await AuthHelper.GetUserDtoAsync(client);
        var otherDeviceRecord = await _factory.Services.GetRequiredService<IDeviceManager>().CreateDevice(
            new Device(user.Id, "Playback tests", "1.0", "Other player", "other-playback-device"));
        using var otherDevice = _factory.CreateClient();
        otherDevice.DefaultRequestHeaders.AddAuthHeader(otherDeviceRecord.AccessToken);

        using var response = await otherDevice.PostAsJsonAsync(
            $"Items/{movie.Id}/PlaybackInfo",
            new PlaybackInfoDto { PlaybackInstanceId = logicalId },
            JsonDefaults.Options,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(logicalId, (await ResolveOwnerAsync(client, movie.Id, first.PlaySessionId!)).PlaybackInstanceId);
    }

    [Fact]
    public async Task PlaybackInfo_ChangedClientHeadersDoNotChangeAuthenticatedOwner()
    {
        using var client = await CreateClientAsync();
        var movie = CreateMovie();
        var logicalId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var first = await PostPlaybackInfoAsync(client, movie.Id, logicalId);
        using var changedHeaders = _factory.CreateClient();
        changedHeaders.DefaultRequestHeaders.Add(
            AuthHelper.AuthHeaderName,
            AuthHelper.DummyAuthHeader.Replace("69420", "changed-client-device", StringComparison.Ordinal) + $", Token={_accessToken}");

        var replacement = await PostPlaybackInfoAsync(changedHeaders, movie.Id, logicalId);

        Assert.Equal(await ResolveOwnerAsync(client, movie.Id, first.PlaySessionId!), await ResolveOwnerAsync(client, movie.Id, replacement.PlaySessionId!));
    }

    [Fact]
    public async Task PlaybackInfoSchema_DescribesBoundedOptionalIdAndQuotaError()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("api-docs/openapi.json", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var requestId = schemas.GetProperty("PlaybackInfoDto").GetProperty("properties").GetProperty("PlaybackInstanceId");
        var responseId = schemas.GetProperty("PlaybackInfoResponse").GetProperty("properties").GetProperty("PlaybackInstanceId");

        Assert.Equal("string", requestId.GetProperty("type").GetString());
        Assert.Equal(128, requestId.GetProperty("maxLength").GetInt32());
        Assert.Equal(128, responseId.GetProperty("maxLength").GetInt32());
        Assert.Contains(nameof(PlaybackErrorCode.MaxActiveVideoStreamsReached), schemas.GetProperty("PlaybackErrorCode").GetProperty("enum").EnumerateArray().Select(value => value.GetString()));
    }

    private async Task<HttpClient> CreateClientAsync()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));
        return client;
    }

    private Movie CreateMovie()
    {
        var id = Guid.NewGuid();
        var movie = new Movie
        {
            Id = id,
            Name = "Playback coordination test",
            Path = $"/test-media/{id:N}.mp4"
        };
        _factory.Services.GetRequiredService<ILibraryManager>().CreateItem(movie, null);
        return movie;
    }

    private static async Task<PlaybackInfoResponse> PostPlaybackInfoAsync(HttpClient client, Guid itemId, string playbackInstanceId)
    {
        using var response = await client.PostAsJsonAsync(
            $"Items/{itemId}/PlaybackInfo",
            new PlaybackInfoDto { PlaybackInstanceId = playbackInstanceId },
            JsonDefaults.Options,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PlaybackInfoResponse>(JsonDefaults.Options, TestContext.Current.CancellationToken))!;
    }

    private static async Task<PlaybackInfoResponse> GetPlaybackInfoAsync(HttpClient client, Guid itemId)
    {
        using var response = await client.GetAsync($"Items/{itemId}/PlaybackInfo", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PlaybackInfoResponse>(JsonDefaults.Options, TestContext.Current.CancellationToken))!;
    }

    private async Task<VideoPlaybackContext> ResolveOwnerAsync(HttpClient client, Guid itemId, string playSessionId)
    {
        var user = await AuthHelper.GetUserDtoAsync(client);
        var device = Assert.Single(_factory.Services.GetRequiredService<IDeviceManager>().GetDevices(new DeviceQuery { UserId = user.Id, AccessToken = _accessToken }).Items);
        var ownerId = device.Id.ToString(CultureInfo.InvariantCulture);
        var coordinator = _factory.Services.GetRequiredService<IVideoPlaybackCoordinator>();
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ResolvePlaySession(user.Id, ownerId, itemId, playSessionId, out var playback));
        return playback;
    }

    public sealed class PlaybackFactory : JellyfinApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                var sources = new Mock<IMediaSourceManager>();
                sources.Setup(value => value.GetPlaybackMediaSources(It.IsAny<BaseItem>(), It.IsAny<User>(), true, true, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new[]
                    {
                        new MediaSourceInfo
                        {
                            Id = "test-video-source",
                            Path = "/test-media/source.mp4",
                            Container = "mp4",
                            Protocol = MediaProtocol.File,
                            SupportsDirectPlay = true,
                            SupportsDirectStream = true,
                            SupportsTranscoding = true
                        }
                    });
                services.AddSingleton(sources.Object);
            });
        }
    }
}
