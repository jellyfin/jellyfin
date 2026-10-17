using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Queries;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Entities.Security;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Devices;
using MediaBrowser.Controller.Library;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Devices;

public sealed class DeviceManagerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly Guid _userId = Guid.NewGuid();

    public DeviceManagerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateDbContext();
        context.Database.EnsureCreated();

        context.Users.Add(new User(
            "device-owner",
            "Jellyfin.Server.Implementations.Users.DefaultAuthenticationProvider",
            "Jellyfin.Server.Implementations.Users.DefaultPasswordResetProvider")
        {
            Id = _userId
        });
        context.Devices.Add(new Device(_userId, "Jellyfin Web", "1.0.0", "Chrome", "device-1"));
        context.SaveChanges();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task UpdateDevice_PersistsChangedValues()
    {
        var manager = CreateDeviceManager();
        var device = manager.GetDevices(new DeviceQuery { DeviceId = "device-1" }).Items[0];

        var activity = DateTime.UtcNow;
        device.DateLastActivity = activity;
        device.AppVersion = "2.0.0";

        await manager.UpdateDevice(device);

        using var context = CreateDbContext();
        var stored = context.Devices.AsNoTracking().Single();
        Assert.Equal("2.0.0", stored.AppVersion);
        Assert.Equal(activity, stored.DateLastActivity, TimeSpan.FromSeconds(1));
        Assert.Equal("Chrome", stored.DeviceName);
    }

    [Fact]
    public async Task UpdateDevice_DeletedRow_DoesNotResurrectIt()
    {
        var manager = CreateDeviceManager();
        var device = manager.GetDevices(new DeviceQuery { DeviceId = "device-1" }).Items[0];

        using (var context = CreateDbContext())
        {
            context.Devices.Where(d => d.Id == device.Id).ExecuteDelete();
        }

        device.DateLastActivity = DateTime.UtcNow;

        // Update() used to mark every property modified, so a vanished row failed the rows-affected
        // check and threw on what is an ordinary authenticated request.
        await manager.UpdateDevice(device);

        using var verifyContext = CreateDbContext();
        Assert.Empty(verifyContext.Devices);
        Assert.Empty(manager.GetDevices(new DeviceQuery { DeviceId = "device-1" }).Items);
    }

    private DeviceManager CreateDeviceManager()
        => new(CreateDbContextFactory(), new Mock<IUserManager>().Object);

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
