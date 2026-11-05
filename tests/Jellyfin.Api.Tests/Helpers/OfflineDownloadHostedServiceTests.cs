using System;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

public static class OfflineDownloadHostedServiceTests
{
    [Fact]
    public static async Task UnavailableOfflineCacheDoesNotFailHostStartup()
    {
        if (Environment.GetEnvironmentVariable("JELLYFIN_OFFLINE_CACHE_PATH") is not null)
        {
            Assert.Skip("This isolated test must not use a developer's configured external cache.");
        }

        var root = Path.Combine(Path.GetTempPath(), "jellyfin-offline-host-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var notDirectory = Path.Combine(root, "file-not-cache");
        try
        {
            await File.WriteAllTextAsync(notDirectory, "unrelated sentinel", TestContext.Current.CancellationToken);
            var paths = new Mock<IServerApplicationPaths>();
            paths.SetupGet(p => p.CachePath).Returns(notDirectory);
            var configuration = new Mock<IServerConfigurationManager>();
            configuration.SetupGet(c => c.ApplicationPaths).Returns(paths.Object);
            var lifetime = new Mock<IHostApplicationLifetime>();
            var service = new OfflineDownloadHostedService(configuration.Object, lifetime.Object, NullLogger<OfflineDownloadHostedService>.Instance);
            await service.StartAsync(TestContext.Current.CancellationToken);
            await service.StopAsync(TestContext.Current.CancellationToken);
            Assert.Equal("unrelated sentinel", await File.ReadAllTextAsync(notDirectory, TestContext.Current.CancellationToken));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
