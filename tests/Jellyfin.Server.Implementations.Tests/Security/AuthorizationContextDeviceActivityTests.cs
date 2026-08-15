using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Queries;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Security;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Security;

public sealed class AuthorizationContextDeviceActivityTests : IDisposable
{
    private const string Token = "0123456789abcdef";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;

    public AuthorizationContextDeviceActivityTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task GetAuthorizationInfo_ParallelRequests_WritesDeviceOnce()
    {
        // The manager hands out the instance it caches, so every request in a burst reads the same
        // stale timestamp. Before the claim was made atomic each of them wrote the row, and the
        // writes queued up behind each other on the database write lock.
        var device = new Device(Guid.NewGuid(), "Jellyfin Web", "1.0.0", "Chrome", "device-1")
        {
            DateLastActivity = DateTime.UtcNow.AddMinutes(-10)
        };

        const int Requests = 8;
        const int Bursts = 25;
        var updates = 0;

        // Hold every request until all of them have read the device, so they all reach the staleness
        // check with the same timestamp in hand. Repeated because the threads still wake in some
        // order, and one burst on its own only sometimes interleaves badly enough to double-write.
        using var lookedUpDevice = new Barrier(Requests);
        var deviceManager = new Mock<IDeviceManager>();
        deviceManager.Setup(m => m.GetDevices(It.IsAny<DeviceQuery>()))
            .Returns(() =>
            {
                var result = new QueryResult<Device>(new List<Device> { device });
                lookedUpDevice.SignalAndWait();
                return result;
            });
        deviceManager.Setup(m => m.UpdateDevice(It.IsAny<Device>()))
            .Returns(() =>
            {
                Interlocked.Increment(ref updates);
                return Task.CompletedTask;
            });

        var sut = CreateAuthorizationContext(deviceManager.Object);

        for (var burst = 0; burst < Bursts; burst++)
        {
            device.DateLastActivity = DateTime.UtcNow.AddMinutes(-10);

            // Dedicated threads: tasks that all block would otherwise wait on thread pool growth.
            var results = await Task.WhenAll(Enumerable.Range(0, Requests).Select(_ => Task.Factory.StartNew(
                () => sut.GetAuthorizationInfo(CreateRequest()),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap()));

            Assert.All(results, r => Assert.True(r.IsAuthenticated));
            Assert.True(device.DateLastActivity > DateTime.UtcNow.AddMinutes(-1));
        }

        Assert.Equal(Bursts, updates);
    }

    [Fact]
    public async Task GetAuthorizationInfo_RecentActivity_DoesNotWriteDevice()
    {
        var device = new Device(Guid.NewGuid(), "Jellyfin Web", "1.0.0", "Chrome", "device-1")
        {
            DateLastActivity = DateTime.UtcNow
        };

        var deviceManager = new Mock<IDeviceManager>();
        deviceManager.Setup(m => m.GetDevices(It.IsAny<DeviceQuery>()))
            .Returns(new QueryResult<Device>(new List<Device> { device }));

        var sut = CreateAuthorizationContext(deviceManager.Object);

        await sut.GetAuthorizationInfo(CreateRequest());

        deviceManager.Verify(m => m.UpdateDevice(It.IsAny<Device>()), Times.Never);
    }

    private static HttpRequest CreateRequest()
    {
        var context = new DefaultHttpContext();
        // Client, Device and Version match the stored device so only the activity timestamp is stale.
        context.Request.Headers.Authorization =
            $"MediaBrowser Token=\"{Token}\", Client=\"Jellyfin Web\", Device=\"Chrome\", DeviceId=\"device-1\", Version=\"1.0.0\"";
        return context.Request;
    }

    private AuthorizationContext CreateAuthorizationContext(IDeviceManager deviceManager)
    {
        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());

        return new AuthorizationContext(
            CreateDbContextFactory(),
            new Mock<IUserManager>().Object,
            deviceManager,
            new Mock<IServerApplicationHost>().Object,
            configurationManager.Object);
    }

    private IDbContextFactory<JellyfinDbContext> CreateDbContextFactory()
    {
        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(CreateDbContext);

        return factory.Object;
    }

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }
}
