using Jellyfin.LiveTv.TunerHosts;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Jellyfin.LiveTv.Tests
{
    public class M3UTunerHostTests
    {
        [Fact]
        public void CreateMediaSourceInfo_DefaultsUserAgent_WhenNoneConfigured()
        {
            var host = CreateHost();
            var info = new TunerHostInfo { Url = "http://example.com/list.m3u" };
            var channel = new ChannelInfo { Path = "http://example.com/stream" };

            var mediaSource = host.CreateMediaSourceInfoPublic(info, channel);

            Assert.True(mediaSource.RequiredHttpHeaders.ContainsKey(HeaderNames.UserAgent));
            Assert.False(mediaSource.RequiredHttpHeaders.ContainsKey(HeaderNames.Referer));
        }

        [Fact]
        public void CreateMediaSourceInfo_AddsReferer_WhenConfigured()
        {
            var host = CreateHost();
            var info = new TunerHostInfo
            {
                Url = "http://example.com/list.m3u",
                Referer = "http://provider.example/portal"
            };
            var channel = new ChannelInfo { Path = "http://example.com/stream" };

            var mediaSource = host.CreateMediaSourceInfoPublic(info, channel);

            Assert.Equal("http://provider.example/portal", mediaSource.RequiredHttpHeaders[HeaderNames.Referer]);
        }

        [Fact]
        public void CreateMediaSourceInfo_AddsCustomHeaders_WhenConfigured()
        {
            var host = CreateHost();
            var info = new TunerHostInfo
            {
                Url = "http://example.com/list.m3u",
                CustomHttpHeaders =
                [
                    new NameValuePair("Origin", "http://provider.example"),
                    new NameValuePair("X-Provider-Token", "abc123")
                ]
            };
            var channel = new ChannelInfo { Path = "http://example.com/stream" };

            var mediaSource = host.CreateMediaSourceInfoPublic(info, channel);

            Assert.Equal("http://provider.example", mediaSource.RequiredHttpHeaders["Origin"]);
            Assert.Equal("abc123", mediaSource.RequiredHttpHeaders["X-Provider-Token"]);
        }

        private static TestableM3UTunerHost CreateHost()
        {
            var mediaSourceManager = new Mock<IMediaSourceManager>();
            mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>())).Returns(MediaProtocol.Http);

            return new TestableM3UTunerHost(
                Mock.Of<IServerConfigurationManager>(),
                mediaSourceManager.Object,
                NullLogger<M3UTunerHost>.Instance,
                Mock.Of<IFileSystem>(),
                Mock.Of<System.Net.Http.IHttpClientFactory>(),
                Mock.Of<IServerApplicationHost>(),
                Mock.Of<INetworkManager>(),
                Mock.Of<IStreamHelper>());
        }

        private sealed class TestableM3UTunerHost : M3UTunerHost
        {
            public TestableM3UTunerHost(
                IServerConfigurationManager config,
                IMediaSourceManager mediaSourceManager,
                ILogger<M3UTunerHost> logger,
                IFileSystem fileSystem,
                System.Net.Http.IHttpClientFactory httpClientFactory,
                IServerApplicationHost appHost,
                INetworkManager networkManager,
                IStreamHelper streamHelper)
                : base(config, mediaSourceManager, logger, fileSystem, httpClientFactory, appHost, networkManager, streamHelper)
            {
            }

            public MediaSourceInfo CreateMediaSourceInfoPublic(TunerHostInfo info, ChannelInfo channel)
                => CreateMediaSourceInfo(info, channel);
        }
    }
}
