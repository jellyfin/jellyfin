using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.LiveTv.Listings;
using Jellyfin.LiveTv.Timers;
using Jellyfin.LiveTv.TunerHosts;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.LiveTv.Tests;

public class DefaultLiveTvServiceCacheRefreshTests
{
    [Fact]
    public async Task GetChannelsAndProgramsAsync_LocalSourcesChangeWithinCacheWindow_RefreshesGuideAndChannelCaches()
    {
        await using var testContext = new DefaultLiveTvServiceCacheRefreshTestContext();
        var startDate = new DateTime(2022, 11, 4, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddDays(1);

        testContext.WriteM3u(
            """
            #EXTM3U
            #EXTINF:-1 tvg-id="3297" tvg-name="Old Channel" tvg-chno="1",Old Channel
            https://example.com/stream-3297.m3u8
            """);

        testContext.WriteXml(
            """
            <tv>
              <channel id="3297">
                <display-name>Old Channel</display-name>
                <icon src="https://example.com/channel-3297.png" />
              </channel>
              <programme channel="3297" start="20221104130000 +0000" stop="20221104133000 +0000">
                <title lang="en">Morning Edition</title>
                <category lang="en">news</category>
              </programme>
            </tv>
            """);

        var firstChannels = (await testContext.Service.GetChannelsAsync(CancellationToken.None)).OrderBy(channel => channel.Number).ToList();
        var firstChannel = Assert.Single(firstChannels);

        Assert.Equal("3297", firstChannel.TunerChannelId);
        Assert.Equal("https://example.com/channel-3297.png", firstChannel.ImageUrl);
        Assert.True(File.Exists(testContext.ChannelCacheFilePath));
        Assert.True(File.Exists(testContext.XmlCacheFilePath));

        var firstProgram = (await testContext.Service.GetProgramsAsync(firstChannel.Id, startDate, endDate, CancellationToken.None)).Single();
        Assert.Equal("Morning Edition", firstProgram.Name);
        Assert.True(firstProgram.IsNews);
        Assert.False(firstProgram.IsSports);

        var firstCachedChannels = await testContext.ReadChannelCacheAsync();
        Assert.Single(firstCachedChannels);

        testContext.WriteM3u(
            """
            #EXTM3U
            #EXTINF:-1 tvg-id="3297" tvg-name="Updated Channel" tvg-chno="1",Updated Channel
            https://example.com/stream-3297.m3u8
            #EXTINF:-1 tvg-id="4401" tvg-name="Brand New Channel" tvg-chno="2",Brand New Channel
            https://example.com/stream-4401.m3u8
            """);

        testContext.WriteXml(
            """
            <tv>
              <channel id="3297">
                <display-name>Updated Channel</display-name>
                <icon src="https://example.com/channel-3297-updated.png" />
              </channel>
              <channel id="4401">
                <display-name>Brand New Channel</display-name>
                <icon src="https://example.com/channel-4401.png" />
              </channel>
              <programme channel="3297" start="20221104130000 +0000" stop="20221104133000 +0000">
                <title lang="en">Championship Live</title>
                <category lang="en">sports</category>
              </programme>
              <programme channel="4401" start="20221104133000 +0000" stop="20221104140000 +0000">
                <title lang="en">Daily Update</title>
                <category lang="en">news</category>
              </programme>
            </tv>
            """,
            File.GetLastWriteTimeUtc(testContext.XmlCacheFilePath).AddSeconds(5));

        var secondChannels = (await testContext.Service.GetChannelsAsync(CancellationToken.None))
            .OrderBy(channel => channel.Number)
            .ToList();

        Assert.Collection(
            secondChannels,
            firstUpdatedChannel =>
            {
                Assert.Equal("3297", firstUpdatedChannel.TunerChannelId);
                Assert.Equal("https://example.com/channel-3297-updated.png", firstUpdatedChannel.ImageUrl);
            },
            secondUpdatedChannel =>
            {
                Assert.Equal("4401", secondUpdatedChannel.TunerChannelId);
                Assert.Equal("https://example.com/channel-4401.png", secondUpdatedChannel.ImageUrl);
            });

        var updatedFirstProgram = (await testContext.Service.GetProgramsAsync(secondChannels[0].Id, startDate, endDate, CancellationToken.None)).Single();
        Assert.Equal("Championship Live", updatedFirstProgram.Name);
        Assert.False(updatedFirstProgram.IsNews);
        Assert.True(updatedFirstProgram.IsSports);

        var newChannelProgram = (await testContext.Service.GetProgramsAsync(secondChannels[1].Id, startDate, endDate, CancellationToken.None)).Single();
        Assert.Equal("Daily Update", newChannelProgram.Name);
        Assert.True(newChannelProgram.IsNews);

        var cachedChannels = await testContext.ReadChannelCacheAsync();
        Assert.Equal(2, cachedChannels.Count);
        Assert.Contains(cachedChannels, channel => string.Equals(channel.TunerChannelId, "4401", StringComparison.Ordinal));
    }

    private sealed class DefaultLiveTvServiceCacheRefreshTestContext : IAsyncDisposable
    {
        private readonly string _rootPath;
        private readonly string _m3uPath;
        private readonly string _xmlPath;

        public DefaultLiveTvServiceCacheRefreshTestContext()
        {
            _rootPath = Path.Combine(Path.GetTempPath(), "jellyfin-livetv-cache-tests", Guid.NewGuid().ToString("N"));
            _m3uPath = Path.Combine(_rootPath, "guide.m3u");
            _xmlPath = Path.Combine(_rootPath, "guide.xml");

            Directory.CreateDirectory(_rootPath);
            Directory.CreateDirectory(Path.Combine(_rootPath, "cache"));
            Directory.CreateDirectory(Path.Combine(_rootPath, "data"));

            var serverConfiguration = new ServerConfiguration
            {
                PreferredMetadataLanguage = "en"
            };

            var liveTvOptions = new LiveTvOptions
            {
                TunerHosts =
                [
                    new TunerHostInfo
                    {
                        Id = "m3u-tuner-under-test",
                        Type = "m3u",
                        Url = _m3uPath
                    }
                ],
                ListingProviders =
                [
                    new ListingsProviderInfo
                    {
                        Id = "provider-under-test",
                        Type = "xmltv",
                        Path = _xmlPath,
                        EnableAllTuners = true
                    }
                ]
            };

            var commonApplicationPaths = new Mock<IApplicationPaths>();
            commonApplicationPaths.SetupGet(paths => paths.DataPath).Returns(Path.Combine(_rootPath, "data"));

            var serverApplicationPaths = new Mock<IServerApplicationPaths>();
            serverApplicationPaths.SetupGet(paths => paths.CachePath).Returns(Path.Combine(_rootPath, "cache"));

            var config = new Mock<IServerConfigurationManager>();
            config.SetupGet(manager => manager.CommonApplicationPaths).Returns(commonApplicationPaths.Object);
            config.SetupGet(manager => manager.CommonConfiguration).Returns(serverConfiguration);
            config.SetupGet(manager => manager.ApplicationPaths).Returns(serverApplicationPaths.Object);
            config.SetupGet(manager => manager.Configuration).Returns(serverConfiguration);
            config.Setup(manager => manager.GetConfiguration("livetv")).Returns(liveTvOptions);

            var provider = new XmlTvListingsProvider(
                config.Object,
                Mock.Of<IHttpClientFactory>(),
                NullLogger<XmlTvListingsProvider>.Instance);

            var tunerHost = new M3UTunerHost(
                config.Object,
                Mock.Of<IMediaSourceManager>(),
                NullLogger<M3UTunerHost>.Instance,
                Mock.Of<IFileSystem>(),
                Mock.Of<IHttpClientFactory>(),
                Mock.Of<IServerApplicationHost>(),
                Mock.Of<INetworkManager>(),
                Mock.Of<IStreamHelper>());

            var tunerHostManager = new Mock<ITunerHostManager>();
            tunerHostManager.SetupGet(manager => manager.TunerHosts).Returns([tunerHost]);

            var listingsManager = new ListingsManager(
                NullLogger<ListingsManager>.Instance,
                config.Object,
                Mock.Of<ITaskManager>(),
                tunerHostManager.Object,
                [provider]);

            var libraryManager = Mock.Of<ILibraryManager>();
            var liveTvDtoService = new LiveTvDtoService(
                Mock.Of<IDtoService>(),
                Mock.Of<IImageProcessor>(),
                NullLogger<LiveTvDtoService>.Instance,
                Mock.Of<IApplicationHost>(),
                libraryManager);

            var timerManager = new TimerManager(NullLogger<TimerManager>.Instance, config.Object);
            var seriesTimerManager = new SeriesTimerManager(NullLogger<SeriesTimerManager>.Instance, config.Object);

            Service = new DefaultLiveTvService(
                NullLogger<DefaultLiveTvService>.Instance,
                config.Object,
                tunerHostManager.Object,
                listingsManager,
                Mock.Of<IRecordingsManager>(),
                libraryManager,
                liveTvDtoService,
                timerManager,
                seriesTimerManager);
        }

        public DefaultLiveTvService Service { get; }

        public string ChannelCacheFilePath => Path.Combine(_rootPath, "cache", "m3u-tuner-under-test_channels");

        public string XmlCacheFilePath => Path.Combine(_rootPath, "cache", "xmltv", "provider-under-test.xml");

        public void WriteM3u(string content)
        {
            File.WriteAllText(_m3uPath, content);
        }

        public void WriteXml(string content, DateTime? lastWriteTimeUtc = null)
        {
            File.WriteAllText(_xmlPath, content);

            if (lastWriteTimeUtc.HasValue)
            {
                File.SetLastWriteTimeUtc(_xmlPath, lastWriteTimeUtc.Value);
            }
        }

        public async Task<List<ChannelInfo>> ReadChannelCacheAsync()
        {
            await using var stream = File.OpenRead(ChannelCacheFilePath);
            return (await JsonSerializer.DeserializeAsync<List<ChannelInfo>>(stream, cancellationToken: CancellationToken.None))!;
        }

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(_rootPath))
            {
                Directory.Delete(_rootPath, true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
