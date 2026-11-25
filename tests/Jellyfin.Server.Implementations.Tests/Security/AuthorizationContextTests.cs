using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Queries;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
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

public sealed class AuthorizationContextTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly Mock<IDeviceManager> _deviceManager = new();
    private readonly List<Device> _devices = [];
    private readonly User _user = new("playback-owner", "authentication", "password-reset");
    private readonly AuthorizationContext _authorizationContext;

    public AuthorizationContextTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>().UseSqlite(_connection).Options;
        using (var context = CreateDbContext())
        {
            context.Database.EnsureCreated();
            context.Users.Add(_user);
            context.SaveChanges();
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(value => value.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(CreateDbContext);
        var userManager = new Mock<IUserManager>();
        userManager.Setup(value => value.GetUserById(_user.Id)).Returns(_user);
        _deviceManager.Setup(value => value.GetDevices(It.IsAny<DeviceQuery>()))
            .Returns((DeviceQuery query) => new QueryResult<Device>
            {
                Items = _devices.Where(device => device.AccessToken == query.AccessToken).ToArray()
            });
        var configuration = new Mock<IServerConfigurationManager>();
        configuration.SetupGet(value => value.Configuration).Returns(new ServerConfiguration());
        _authorizationContext = new AuthorizationContext(
            factory.Object,
            userManager.Object,
            _deviceManager.Object,
            Mock.Of<IServerApplicationHost>(),
            configuration.Object);
    }

    [Fact]
    public async Task GetAuthorizationInfo_CastingHeadersAndQueryTokenKeepAuthenticatedOwner()
    {
        var device = await AddDeviceAsync("shared-token");
        var castingRequest = CreateHeaderRequest(device.AccessToken);
        var queryRequest = new DefaultHttpContext();
        queryRequest.Request.QueryString = new QueryString("?ApiKey=shared-token&PlaybackOwnerId=spoofed-owner");

        var casting = await _authorizationContext.GetAuthorizationInfo(castingRequest);
        var media = await _authorizationContext.GetAuthorizationInfo(queryRequest);

        Assert.True(casting.IsAuthenticated);
        Assert.True(media.IsAuthenticated);
        Assert.Equal(device.Id.ToString(CultureInfo.InvariantCulture), casting.PlaybackOwnerId);
        Assert.Equal(casting.PlaybackOwnerId, media.PlaybackOwnerId);
        Assert.Equal("casting-client", casting.Client);
        Assert.Equal("casting-device", casting.DeviceId);
        Assert.Equal(device.AppName, media.Client);
        Assert.Equal(device.DeviceId, media.DeviceId);
        _deviceManager.Verify(value => value.GetDevices(It.Is<DeviceQuery>(query => query.AccessToken == device.AccessToken)), Times.Exactly(2));
    }

    [Fact]
    public async Task GetAuthorizationInfo_DifferentTokensWithIdenticalHeadersHaveDistinctOwners()
    {
        var firstDevice = await AddDeviceAsync("first-token");
        var secondDevice = await AddDeviceAsync("second-token");

        var first = await _authorizationContext.GetAuthorizationInfo(CreateHeaderRequest(firstDevice.AccessToken));
        var second = await _authorizationContext.GetAuthorizationInfo(CreateHeaderRequest(secondDevice.AccessToken));

        Assert.Equal(first.UserId, second.UserId);
        Assert.Equal(first.Client, second.Client);
        Assert.Equal(first.DeviceId, second.DeviceId);
        Assert.Equal(firstDevice.Id.ToString(CultureInfo.InvariantCulture), first.PlaybackOwnerId);
        Assert.Equal(secondDevice.Id.ToString(CultureInfo.InvariantCulture), second.PlaybackOwnerId);
        Assert.NotEqual(first.PlaybackOwnerId, second.PlaybackOwnerId);
    }

    [Fact]
    public async Task GetAuthorizationInfo_WithoutTokenDoesNotAcceptClaimedOwner()
    {
        var request = new DefaultHttpContext();
        request.Request.Headers.Authorization = "MediaBrowser Client=\"casting-client\", DeviceId=\"casting-device\", PlaybackOwnerId=\"42\"";

        var authorization = await _authorizationContext.GetAuthorizationInfo(request);

        Assert.False(authorization.IsAuthenticated);
        Assert.Null(authorization.PlaybackOwnerId);
        _deviceManager.Verify(value => value.GetDevices(It.IsAny<DeviceQuery>()), Times.Never);
    }

    public void Dispose() => _connection.Dispose();

    private static DefaultHttpContext CreateHeaderRequest(string token)
    {
        var request = new DefaultHttpContext();
        request.Request.Headers.Authorization = $"MediaBrowser Client=\"casting-client\", DeviceId=\"casting-device\", Token=\"{token}\", PlaybackOwnerId=\"spoofed-owner\"";
        return request;
    }

    private async Task<Device> AddDeviceAsync(string token)
    {
        var device = new Device(_user.Id, "stored-client", "1.0", "stored-device-name", "stored-device-id")
        {
            AccessToken = token
        };
        await using var context = CreateDbContext();
        context.Devices.Add(device);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _devices.Add(device);
        return device;
    }

    private JellyfinDbContext CreateDbContext()
        => new(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
}
