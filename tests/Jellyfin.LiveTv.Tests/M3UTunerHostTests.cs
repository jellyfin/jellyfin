using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.LiveTv.TunerHosts;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Jellyfin.LiveTv.Tests
{
    public class M3UTunerHostTests
    {
        [Theory]
        // A manifest is not a byte stream, so it must never be offered for direct play.
        [InlineData("http://example.com/live/1234.m3u8", false)]
        [InlineData("http://example.com/live/1234.m3u8?token=abc", false)]
        [InlineData("http://example.com/live/1234.mpd", false)]
        // Byte streams are unaffected.
        [InlineData("http://example.com/live/1234.ts", true)]
        [InlineData("http://example.com/live/1234", true)]
        public async Task GetChannelStreamMediaSources_ManifestPath_DisablesDirectPlay(string path, bool expectDirectPlay)
        {
            var mediaSourceManager = new Mock<IMediaSourceManager>();
            mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>())).Returns(MediaProtocol.Http);

            var host = new TestableM3UTunerHost(
                Mock.Of<IServerConfigurationManager>(),
                mediaSourceManager.Object,
                Mock.Of<ILogger<M3UTunerHost>>(),
                Mock.Of<IFileSystem>(),
                Mock.Of<IHttpClientFactory>(),
                Mock.Of<IServerApplicationHost>(),
                Mock.Of<INetworkManager>(),
                Mock.Of<IStreamHelper>());

            var sources = await host.GetMediaSources(
                new TunerHostInfo { TunerCount = 0, EnableStreamLooping = false },
                new ChannelInfo { Path = path });

            Assert.Equal(expectDirectPlay, sources[0].SupportsDirectPlay);
        }

        [Fact]
        public async Task GetChannelStream_HeadProbe_UsesTunerUserAgent()
        {
            HttpRequestMessage? sent = null;
            var messageHandler = new Mock<HttpMessageHandler>();
            messageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((m, _) => sent = m)
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
            using var client = new HttpClient(messageHandler.Object);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Jellyfin-Server/1.0");
            var http = new Mock<IHttpClientFactory>();
            http.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
            var config = new Mock<IServerConfigurationManager>();
            config.Setup(x => x.GetConfiguration("encoding")).Returns(new EncodingOptions { TranscodingTempPath = "/tmp" });
            config.SetupGet(x => x.CommonApplicationPaths).Returns(Mock.Of<IApplicationPaths>());
            var mediaSourceManager = new Mock<IMediaSourceManager>();
            mediaSourceManager.Setup(x => x.GetPathProtocol(It.IsAny<string>())).Returns(MediaProtocol.Http);

            var host = new TestableM3UTunerHost(
                config.Object,
                mediaSourceManager.Object,
                Mock.Of<ILogger<M3UTunerHost>>(),
                Mock.Of<IFileSystem>(),
                http.Object,
                Mock.Of<IServerApplicationHost>(),
                Mock.Of<INetworkManager>(),
                Mock.Of<IStreamHelper>());

            await host.GetStream(new TunerHostInfo { UserAgent = "Tuner/1.0" }, new ChannelInfo { Path = "http://example.com/live/1234" });

            Assert.Equal(HttpMethod.Head, sent?.Method);
            Assert.Equal("Tuner/1.0", sent?.Headers.UserAgent.ToString());
        }

        private sealed class TestableM3UTunerHost : M3UTunerHost
        {
            public TestableM3UTunerHost(
                IServerConfigurationManager config,
                IMediaSourceManager mediaSourceManager,
                ILogger<M3UTunerHost> logger,
                IFileSystem fileSystem,
                IHttpClientFactory httpClientFactory,
                IServerApplicationHost appHost,
                INetworkManager networkManager,
                IStreamHelper streamHelper)
                : base(config, mediaSourceManager, logger, fileSystem, httpClientFactory, appHost, networkManager, streamHelper)
            {
            }

            public Task<ILiveStream> GetStream(TunerHostInfo tuner, ChannelInfo channel)
                => GetChannelStream(tuner, channel, "stream", new List<ILiveStream>(), CancellationToken.None);

            public Task<List<MediaSourceInfo>> GetMediaSources(TunerHostInfo tuner, ChannelInfo channel)
                => GetChannelStreamMediaSources(tuner, channel, CancellationToken.None);
        }
    }
}
