using System;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    public class MediaInfoHelperTests
    {
        private const string LiveStreamFilesPath = "/LiveTv/LiveStreamFiles/abc/stream.ts";

        private static MediaInfoHelper CreateHelper(
            IMediaSourceManager? mediaSourceManager = null,
            IServerApplicationHost? appHost = null,
            string baseUrl = "")
        {
            var serverConfigurationManager = new Mock<IServerConfigurationManager>();
            serverConfigurationManager
                .Setup(x => x.GetConfiguration(It.IsAny<string>()))
                .Returns(new NetworkConfiguration { BaseUrl = baseUrl });

            return new MediaInfoHelper(
                Mock.Of<IUserManager>(),
                Mock.Of<ILibraryManager>(),
                mediaSourceManager ?? Mock.Of<IMediaSourceManager>(),
                Mock.Of<IMediaEncoder>(),
                serverConfigurationManager.Object,
                Mock.Of<ILogger<MediaInfoHelper>>(),
                Mock.Of<INetworkManager>(),
                Mock.Of<IDeviceManager>(),
                appHost ?? Mock.Of<IServerApplicationHost>());
        }

        private static MediaSourceInfo CreateSource(Guid itemId, int bitrate, bool supportsDirectPlay = true)
        {
            return new MediaSourceInfo
            {
                Id = itemId.ToString("N", CultureInfo.InvariantCulture),
                Protocol = MediaProtocol.File,
                Bitrate = bitrate,
                SupportsDirectPlay = supportsDirectPlay,
                SupportsDirectStream = true,
                SupportsTranscoding = true
            };
        }

        [Fact]
        public void SortMediaSources_PreferredItemExceedsBitrate_StaysDefault()
        {
            // The version the user was watching (the queried item) must stay the default
            // even when a sibling version fits the bitrate limit better, since the resume
            // position belongs to that exact version.
            var preferredItemId = Guid.NewGuid();
            var preferredSource = CreateSource(preferredItemId, bitrate: 80_000_000, supportsDirectPlay: false);
            var siblingSource = CreateSource(Guid.NewGuid(), bitrate: 8_000_000);

            var result = new PlaybackInfoResponse
            {
                MediaSources = [siblingSource, preferredSource]
            };

            CreateHelper().SortMediaSources(result, maxBitrate: 20_000_000, preferredItemId);

            Assert.Equal(preferredSource.Id, result.MediaSources[0].Id);
        }

        [Fact]
        public void SortMediaSources_NoPreferredItem_OrdersByPlayability()
        {
            var directPlay = CreateSource(Guid.NewGuid(), bitrate: 8_000_000);
            var transcodeOnly = CreateSource(Guid.NewGuid(), bitrate: 8_000_000, supportsDirectPlay: false);
            transcodeOnly.SupportsDirectStream = false;

            var result = new PlaybackInfoResponse
            {
                MediaSources = [transcodeOnly, directPlay]
            };

            CreateHelper().SortMediaSources(result, maxBitrate: 20_000_000);

            Assert.Equal(directPlay.Id, result.MediaSources[0].Id);
        }

        [Fact]
        public void SortMediaSources_PreferredIdNotInSources_KeepsPlayabilityOrder()
        {
            var directPlay = CreateSource(Guid.NewGuid(), bitrate: 8_000_000);
            var transcodeOnly = CreateSource(Guid.NewGuid(), bitrate: 8_000_000, supportsDirectPlay: false);
            transcodeOnly.SupportsDirectStream = false;

            var result = new PlaybackInfoResponse
            {
                MediaSources = [transcodeOnly, directPlay]
            };

            CreateHelper().SortMediaSources(result, maxBitrate: 20_000_000, Guid.NewGuid());

            Assert.Equal(directPlay.Id, result.MediaSources[0].Id);
        }

        [Fact]
        public async Task GetPlaybackInfo_ExistingLiveStream_RewritesReturnedCloneOnly()
        {
            const string LocalPath = "http://172.19.0.3:8096" + LiveStreamFilesPath;

            var sharedLiveSource = new MediaSourceInfo
            {
                Id = "abc",
                Protocol = MediaProtocol.Http,
                Path = LocalPath,
                LiveStreamId = "livestream-1"
            };

            var mediaSourceManager = new Mock<IMediaSourceManager>();
            mediaSourceManager
                .Setup(x => x.GetLiveStream(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sharedLiveSource);

            var appHost = new Mock<IServerApplicationHost>();
            appHost.Setup(x => x.GetSmartApiUrl(It.IsAny<HttpRequest>())).Returns("https://media.example.com");

            var helper = CreateHelper(mediaSourceManager: mediaSourceManager.Object, appHost: appHost.Object);

            var result = await helper.GetPlaybackInfo(new Movie(), null, Mock.Of<HttpRequest>(), liveStreamId: "live-1").ConfigureAwait(true);

            Assert.Equal("https://media.example.com" + LiveStreamFilesPath, result.MediaSources[0].Path);

            // The shared instance handed back by GetLiveStream must remain untouched; only the clone in the response may be rewritten.
            Assert.Equal(LocalPath, sharedLiveSource.Path);
        }

        [Fact]
        public async Task OpenMediaSource_RewritesReturnedLiveStreamPath()
        {
            var mediaSource = new MediaSourceInfo
            {
                Id = "abc",
                Protocol = MediaProtocol.Http,
                Path = "http://127.0.0.1:8096" + LiveStreamFilesPath,
                LiveStreamId = "livestream-1"
            };

            var helper = CreateOpenMediaSourceHelper(mediaSource, "https://public.example.com");

            var response = await helper.OpenMediaSource(new DefaultHttpContext(), new LiveStreamRequest()).ConfigureAwait(true);

            Assert.Equal("https://public.example.com" + LiveStreamFilesPath, response.MediaSource.Path);
        }

        [Fact]
        public async Task OpenMediaSource_ExternalDockerBridgeBehindReverseProxy_UsesPublishedUrl()
        {
            const string LocalPath = "http://172.23.0.5:8096" + LiveStreamFilesPath;

            // Represents the instance MediaSourceManager keeps for its own bookkeeping; the helper never sees it
            // and must not be able to affect it.
            var localSource = new MediaSourceInfo
            {
                Id = "abc",
                Protocol = MediaProtocol.Http,
                Path = LocalPath,
                LiveStreamId = "livestream-1"
            };

            var mediaSourceManager = new Mock<IMediaSourceManager>();
            mediaSourceManager
                .Setup(x => x.OpenLiveStream(It.IsAny<LiveStreamRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    // Mirrors production: MediaSourceManager.OpenLiveStream hands back its own instance, so what the
                    // helper mutates must be a deserialized copy, never localSource itself.
                    var clone = JsonSerializer.Deserialize<MediaSourceInfo>(JsonSerializer.SerializeToUtf8Bytes(localSource))!;
                    return new LiveStreamResponse(clone);
                });

            var appHost = new Mock<IServerApplicationHost>();
            appHost.Setup(x => x.GetSmartApiUrl(It.IsAny<HttpRequest>())).Returns("https://jellyfin.example.com");

            var helper = CreateHelper(mediaSourceManager: mediaSourceManager.Object, appHost: appHost.Object);

            var response = await helper.OpenMediaSource(new DefaultHttpContext(), new LiveStreamRequest()).ConfigureAwait(true);

            Assert.Equal("https://jellyfin.example.com" + LiveStreamFilesPath, response.MediaSource.Path);

            // The mock now actually derives its response from localSource, so this assertion is meaningful:
            // rewriting the returned clone must never mutate the object localSource represents.
            Assert.Equal(LocalPath, localSource.Path);
        }

        [Fact]
        public async Task OpenMediaSource_ForeignHostWithLiveStreamFilesRoute_PathUnchanged()
        {
            // A plugin or remote source can expose a path that happens to match the /LiveTv/LiveStreamFiles/
            // route shape without actually being hosted by this server. Only opened streams (which always
            // carry a LiveStreamId) are eligible for rewriting.
            const string ForeignPath = "https://other-server:8096" + LiveStreamFilesPath;

            var mediaSource = new MediaSourceInfo
            {
                Id = "abc",
                Protocol = MediaProtocol.Http,
                Path = ForeignPath
            };

            var helper = CreateOpenMediaSourceHelper(mediaSource, "https://media.example.com");

            var response = await helper.OpenMediaSource(new DefaultHttpContext(), new LiveStreamRequest()).ConfigureAwait(true);

            Assert.Equal(ForeignPath, response.MediaSource.Path);
        }

        [Theory]
        [InlineData(MediaProtocol.Http, "http://192.168.1.50:5004/live/channel1.ts")]
        [InlineData(MediaProtocol.File, "/media/livetv/buffer/abc/stream.ts")]
        [InlineData(MediaProtocol.Http, "http://172.19.0.3:8096/Videos/abc/stream.ts")]
        public async Task OpenMediaSource_NotAPublishableLiveStreamFilesPath_PathUnchanged(MediaProtocol protocol, string path)
        {
            var mediaSource = new MediaSourceInfo
            {
                Id = "abc",
                Protocol = protocol,
                Path = path
            };

            var helper = CreateOpenMediaSourceHelper(mediaSource, "https://media.example.com");

            var response = await helper.OpenMediaSource(new DefaultHttpContext(), new LiveStreamRequest()).ConfigureAwait(true);

            Assert.Equal(path, response.MediaSource.Path);
        }

        [Fact]
        public async Task OpenMediaSource_BaseUrlConfigured_RewritesWithBaseUrlPrefix()
        {
            var mediaSource = new MediaSourceInfo
            {
                Id = "abc",
                Protocol = MediaProtocol.Http,
                Path = "http://172.19.0.3:8096/jellyfin" + LiveStreamFilesPath,
                LiveStreamId = "livestream-1"
            };

            var helper = CreateOpenMediaSourceHelper(mediaSource, "https://media.example.com/jellyfin", "/jellyfin");

            var response = await helper.OpenMediaSource(new DefaultHttpContext(), new LiveStreamRequest()).ConfigureAwait(true);

            Assert.Equal("https://media.example.com/jellyfin" + LiveStreamFilesPath, response.MediaSource.Path);
        }

        [Fact]
        public async Task OpenMediaSource_BaseUrlSegmentMismatch_PathUnchanged()
        {
            const string LocalPath = "http://172.19.0.3:8096/jellyfin2" + LiveStreamFilesPath;

            var mediaSource = new MediaSourceInfo
            {
                Id = "abc",
                Protocol = MediaProtocol.Http,
                Path = LocalPath
            };

            var helper = CreateOpenMediaSourceHelper(mediaSource, "https://media.example.com/jellyfin", "/jellyfin");

            var response = await helper.OpenMediaSource(new DefaultHttpContext(), new LiveStreamRequest()).ConfigureAwait(true);

            Assert.Equal(LocalPath, response.MediaSource.Path);
        }

        [Theory]
        [InlineData(
            "https://media.example.com",
            "http://172.19.0.3:8096" + LiveStreamFilesPath,
            MediaProtocol.Http,
            "",
            "https://media.example.com" + LiveStreamFilesPath)]
        [InlineData(
            "https://media.example.com/",
            "http://172.19.0.3:8096" + LiveStreamFilesPath + "?token=1",
            MediaProtocol.Http,
            "",
            "https://media.example.com" + LiveStreamFilesPath + "?token=1")]
        [InlineData(
            "https://media.example.com",
            "http://172.19.0.3:8096" + LiveStreamFilesPath + "#fragment",
            MediaProtocol.Http,
            "",
            "https://media.example.com" + LiveStreamFilesPath)]
        [InlineData(
            "https://media.example.com",
            "http://172.19.0.3:8096/jellyfin2" + LiveStreamFilesPath,
            MediaProtocol.Http,
            "/jellyfin",
            null)]
        [InlineData(
            "https://media.example.com",
            "/media/livetv/buffer/abc/stream.ts",
            MediaProtocol.File,
            "",
            null)]
        [InlineData(
            "https://media.example.com",
            "not a uri",
            MediaProtocol.Http,
            "",
            null)]
        public void GetPublishedLiveStreamPath_VariousInputs_ReturnsExpected(string smartApiUrl, string localPath, MediaProtocol protocol, string baseUrl, string? expected)
        {
            var result = MediaInfoHelper.GetPublishedLiveStreamPath(smartApiUrl, localPath, protocol, baseUrl);

            Assert.Equal(expected, result);
        }

        private static MediaInfoHelper CreateOpenMediaSourceHelper(MediaSourceInfo mediaSource, string smartApiUrl, string baseUrl = "")
        {
            var mediaSourceManager = new Mock<IMediaSourceManager>();
            mediaSourceManager
                .Setup(x => x.OpenLiveStream(It.IsAny<LiveStreamRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LiveStreamResponse(mediaSource));

            var appHost = new Mock<IServerApplicationHost>();
            appHost.Setup(x => x.GetSmartApiUrl(It.IsAny<HttpRequest>())).Returns(smartApiUrl);

            return CreateHelper(mediaSourceManager: mediaSourceManager.Object, appHost: appHost.Object, baseUrl: baseUrl);
        }
    }
}
