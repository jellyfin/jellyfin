using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.LiveTv.Listings;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.LiveTv.Tests.Listings;

public class XmlTvListingsProviderCacheTests
{
    [Fact]
    public async Task GetProgramsAsync_LocalXmlChangesWithinCacheWindow_ReturnsUpdatedProgramData()
    {
        await using var testContext = new XmlTvListingsProviderCacheTestContext();
        var startDate = new DateTime(2022, 11, 4, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddDays(1);

        testContext.WriteXml(
            """
            <tv date="20221104">
              <programme channel="3297" start="20221104130000 +0000" stop="20221104133000 +0000">
                <title lang="en">Morning Edition</title>
                <category lang="en">news</category>
              </programme>
            </tv>
            """);

        var firstProgram = (await testContext.Provider.GetProgramsAsync(testContext.ProviderInfo, "3297", startDate, endDate, CancellationToken.None))
            .Single();

        Assert.Equal("Morning Edition", firstProgram.Name);
        Assert.True(firstProgram.IsNews);
        Assert.False(firstProgram.IsSports);
        Assert.True(File.Exists(testContext.CacheFilePath));

        testContext.WriteXml(
            """
            <tv date="20221104">
              <programme channel="3297" start="20221104130000 +0000" stop="20221104133000 +0000">
                <title lang="en">Championship Live</title>
                <category lang="en">sports</category>
              </programme>
            </tv>
            """,
            File.GetLastWriteTimeUtc(testContext.CacheFilePath).AddSeconds(5));

        var secondProgram = (await testContext.Provider.GetProgramsAsync(testContext.ProviderInfo, "3297", startDate, endDate, CancellationToken.None))
            .Single();

        Assert.Equal("Championship Live", secondProgram.Name);
        Assert.False(secondProgram.IsNews);
        Assert.True(secondProgram.IsSports);
    }

    [Fact]
    public async Task GetChannels_LocalXmlChangesWithinCacheWindow_ReturnsUpdatedChannelData()
    {
        await using var testContext = new XmlTvListingsProviderCacheTestContext();

        testContext.WriteXml(
            """
            <tv>
              <channel id="3297">
                <display-name>Old Channel</display-name>
              </channel>
            </tv>
            """);

        var firstChannels = await testContext.Provider.GetChannels(testContext.ProviderInfo, CancellationToken.None);

        Assert.Single(firstChannels);
        Assert.Equal("3297", firstChannels[0].Id);
        Assert.Equal("Old Channel", firstChannels[0].Name);
        Assert.True(File.Exists(testContext.CacheFilePath));

        testContext.WriteXml(
            """
            <tv>
              <channel id="3297">
                <display-name>Updated Channel</display-name>
              </channel>
              <channel id="4401">
                <display-name>Brand New Channel</display-name>
              </channel>
            </tv>
            """,
            File.GetLastWriteTimeUtc(testContext.CacheFilePath).AddSeconds(5));

        var secondChannels = await testContext.Provider.GetChannels(testContext.ProviderInfo, CancellationToken.None);

        Assert.Equal(2, secondChannels.Count);
        Assert.Collection(
            secondChannels.OrderBy(channel => channel.Id),
            firstChannel =>
            {
                Assert.Equal("3297", firstChannel.Id);
                Assert.Equal("Updated Channel", firstChannel.Name);
            },
            secondChannel =>
            {
                Assert.Equal("4401", secondChannel.Id);
                Assert.Equal("Brand New Channel", secondChannel.Name);
            });
    }

    [Fact]
    public async Task GetProgramsAsync_LocalGzippedXmlChangesWithinCacheWindow_ReturnsUpdatedProgramDataAndCacheContents()
    {
        await using var testContext = new XmlTvListingsProviderCacheTestContext("guide.xml.gz");
        var startDate = new DateTime(2022, 11, 4, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddDays(1);

        testContext.WriteGzipXml(
            """
            <tv date="20221104">
              <programme channel="3297" start="20221104130000 +0000" stop="20221104133000 +0000">
                <title lang="en">Morning Edition</title>
                <category lang="en">news</category>
              </programme>
            </tv>
            """);

        var firstProgram = (await testContext.Provider.GetProgramsAsync(testContext.ProviderInfo, "3297", startDate, endDate, CancellationToken.None))
            .Single();

        Assert.Equal("Morning Edition", firstProgram.Name);
        Assert.Contains("Morning Edition", await File.ReadAllTextAsync(testContext.CacheFilePath, CancellationToken.None), StringComparison.Ordinal);

        testContext.WriteGzipXml(
            """
            <tv date="20221104">
              <programme channel="3297" start="20221104130000 +0000" stop="20221104133000 +0000">
                <title lang="en">Championship Live</title>
                <category lang="en">sports</category>
              </programme>
            </tv>
            """,
            File.GetLastWriteTimeUtc(testContext.CacheFilePath).AddSeconds(5));

        var secondProgram = (await testContext.Provider.GetProgramsAsync(testContext.ProviderInfo, "3297", startDate, endDate, CancellationToken.None))
            .Single();

        Assert.Equal("Championship Live", secondProgram.Name);
        Assert.True(secondProgram.IsSports);

        var cacheContents = await File.ReadAllTextAsync(testContext.CacheFilePath, CancellationToken.None);
        Assert.Contains("Championship Live", cacheContents, StringComparison.Ordinal);
        Assert.DoesNotContain("Morning Edition", cacheContents, StringComparison.Ordinal);
    }

    private sealed class XmlTvListingsProviderCacheTestContext : IAsyncDisposable
    {
        private readonly string _rootPath;
        private readonly string _sourceFilePath;

        public XmlTvListingsProviderCacheTestContext(string sourceFileName = "guide.xml")
        {
            _rootPath = Path.Combine(Path.GetTempPath(), "jellyfin-xmltv-cache-tests", Guid.NewGuid().ToString("N"));
            _sourceFilePath = Path.Combine(_rootPath, sourceFileName);

            Directory.CreateDirectory(_rootPath);

            var serverConfiguration = new ServerConfiguration
            {
                PreferredMetadataLanguage = "en"
            };

            var appPaths = new Mock<IServerApplicationPaths>();
            appPaths.SetupGet(paths => paths.CachePath).Returns(Path.Combine(_rootPath, "cache"));

            var config = new Mock<IServerConfigurationManager>();
            config.SetupGet(manager => manager.Configuration).Returns(serverConfiguration);
            config.SetupGet(manager => manager.ApplicationPaths).Returns(appPaths.Object);

            Provider = new XmlTvListingsProvider(
                config.Object,
                Mock.Of<IHttpClientFactory>(),
                NullLogger<XmlTvListingsProvider>.Instance);

            ProviderInfo = new ListingsProviderInfo
            {
                Id = "provider-under-test",
                Path = _sourceFilePath
            };
        }

        public XmlTvListingsProvider Provider { get; }

        public ListingsProviderInfo ProviderInfo { get; }

        public string CacheFilePath => Path.Combine(_rootPath, "cache", "xmltv", ProviderInfo.Id + ".xml");

        public void WriteXml(string content, DateTime? lastWriteTimeUtc = null)
        {
            File.WriteAllText(_sourceFilePath, content);

            if (lastWriteTimeUtc.HasValue)
            {
                File.SetLastWriteTimeUtc(_sourceFilePath, lastWriteTimeUtc.Value);
            }
        }

        public void WriteGzipXml(string content, DateTime? lastWriteTimeUtc = null)
        {
            using (var fileStream = File.Create(_sourceFilePath))
            using (var gzipStream = new GZipStream(fileStream, CompressionLevel.SmallestSize))
            using (var writer = new StreamWriter(gzipStream))
            {
                writer.Write(content);
            }

            if (lastWriteTimeUtc.HasValue)
            {
                File.SetLastWriteTimeUtc(_sourceFilePath, lastWriteTimeUtc.Value);
            }
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
