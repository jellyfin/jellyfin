using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Queries;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.Implementations.Metrics;
using Jellyfin.Server.Implementations.Tests.Item;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Metrics;

[Collection(MetricsTestCollection.Name)]
public sealed class MetricsCollectionServiceTests : SqliteDbTestFixture
{
    [Fact]
    public async Task HostedService_RecordsPlaybackStartsAndUnsubscribesWhenStopped()
    {
        EventHandler<PlaybackProgressEventArgs>? playbackStartHandler = null;
        var sessionManager = new Mock<ISessionManager>();
        sessionManager.SetupGet(x => x.Sessions).Returns([]);
        sessionManager
            .SetupAdd(x => x.PlaybackStart += It.IsAny<EventHandler<PlaybackProgressEventArgs>>())
            .Callback<EventHandler<PlaybackProgressEventArgs>>(handler => playbackStartHandler += handler);
        sessionManager
            .SetupRemove(x => x.PlaybackStart -= It.IsAny<EventHandler<PlaybackProgressEventArgs>>())
            .Callback<EventHandler<PlaybackProgressEventArgs>>(handler => playbackStartHandler -= handler);
        var deviceManager = new Mock<IDeviceManager>();
        deviceManager.Setup(x => x.GetDevices(It.IsAny<DeviceQuery>()))
            .Returns(new QueryResult<Jellyfin.Database.Implementations.Entities.Security.Device>(null, 0, []));

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(x => x.GetVirtualFolders()).Returns([]);
        var itemCountService = new Mock<IItemCountService>();
        itemCountService.Setup(x => x.GetItemCounts(It.IsAny<InternalItemsQuery>())).Returns(new ItemCounts());
        var userManager = new Mock<IUserManager>();
        userManager.Setup(x => x.GetUsers()).Returns([]);
        userManager.Setup(x => x.GetAuthenticationProviders()).Returns([]);

        var sessionCollector = new SessionMetricsCollector(sessionManager.Object, deviceManager.Object);
        var libraryCollector = new LibraryMetricsCollector(libraryManager.Object, itemCountService.Object, CreateDbContextFactory());
        var userCollector = new UserMetricsCollector(userManager.Object);
        var authenticationCollector = new AuthenticationMetricsCollector(userManager.Object);
        var service = new MetricsCollectionService(
            sessionManager.Object,
            sessionCollector,
            libraryCollector,
            userCollector,
            authenticationCollector,
            NullLogger<MetricsCollectionService>.Instance);

        await service.StartAsync(CancellationToken.None);
        try
        {
            var before = JellyfinMetrics.PlayMethodsTotal.WithLabels("transcode", "unknown").Value;
            var args = new PlaybackProgressEventArgs
            {
                Session = new SessionInfo(sessionManager.Object, NullLogger.Instance)
                {
                    PlayState = new PlayerStateInfo { PlayMethod = PlayMethod.Transcode }
                }
            };

            playbackStartHandler!(sessionManager.Object, args);
            Assert.Equal(before + 1, JellyfinMetrics.PlayMethodsTotal.WithLabels("transcode", "unknown").Value);

            await service.StopAsync(CancellationToken.None);
            playbackStartHandler?.Invoke(sessionManager.Object, args);

            Assert.Equal(before + 1, JellyfinMetrics.PlayMethodsTotal.WithLabels("transcode", "unknown").Value);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
            service.Dispose();
        }
    }
}
