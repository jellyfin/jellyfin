using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Events;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users;

/// <summary>
/// The user returned by <c>GetUserById</c> is cached and carries the permissions authorization runs
/// on, so every write path has to drop it again.
/// </summary>
public sealed class UserManagerCacheTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly UserManager _userManager;

    public UserManagerCacheTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var context = CreateDbContext())
        {
            context.Database.EnsureCreated();
        }

        _userManager = CreateUserManager();
    }

    public void Dispose()
    {
        _userManager.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetUserById_SecondCall_IsServedFromCache()
    {
        var created = await _userManager.CreateUserAsync("cached-user");

        var first = _userManager.GetUserById(created.Id);
        var second = _userManager.GetUserById(created.Id);

        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    [Fact]
    public async Task UpdatePolicyAsync_MakesNewPermissionsVisible()
    {
        var created = await _userManager.CreateUserAsync("policy-user");
        var before = _userManager.GetUserById(created.Id);
        Assert.False(before!.HasPermission(PermissionKind.IsAdministrator));

        await _userManager.UpdatePolicyAsync(created.Id, PolicyFor(before!, isAdministrator: true));

        var after = _userManager.GetUserById(created.Id);
        Assert.NotSame(before, after);
        Assert.True(after!.HasPermission(PermissionKind.IsAdministrator));
    }

    [Fact]
    public async Task UpdatePolicyAsync_MakesRevokedPermissionsVisible()
    {
        var created = await _userManager.CreateUserAsync("disabled-user");
        var enabled = _userManager.GetUserById(created.Id);
        Assert.False(enabled!.HasPermission(PermissionKind.IsDisabled));

        await _userManager.UpdatePolicyAsync(created.Id, PolicyFor(enabled!, isDisabled: true));

        Assert.True(_userManager.GetUserById(created.Id)!.HasPermission(PermissionKind.IsDisabled));
    }

    [Fact]
    public async Task UpdateConfigurationAsync_MakesNewConfigurationVisible()
    {
        var created = await _userManager.CreateUserAsync("config-user");
        var before = _userManager.GetUserById(created.Id);
        Assert.False(before!.DisplayCollectionsView);

        await _userManager.UpdateConfigurationAsync(created.Id, new UserConfiguration { DisplayCollectionsView = true });

        var after = _userManager.GetUserById(created.Id);
        Assert.NotSame(before, after);
        Assert.True(after!.DisplayCollectionsView);
    }

    [Fact]
    public async Task UpdateUserAsync_DropsTheCachedInstance()
    {
        var created = await _userManager.CreateUserAsync("update-user");
        var before = _userManager.GetUserById(created.Id);
        before!.MaxActiveSessions = 5;

        await _userManager.UpdateUserAsync(before);

        var after = _userManager.GetUserById(created.Id);
        Assert.NotSame(before, after);
        Assert.Equal(5, after!.MaxActiveSessions);
    }

    [Fact]
    public async Task RecordUserActivityAsync_MakesTheNewDateVisible()
    {
        var created = await _userManager.CreateUserAsync("activity-user");
        var before = _userManager.GetUserById(created.Id);
        var activityDate = DateTime.UtcNow;

        await _userManager.RecordUserActivityAsync(created.Id, activityDate);

        var after = _userManager.GetUserById(created.Id);
        Assert.NotSame(before, after);
        Assert.Equal(activityDate, after!.LastActivityDate!.Value, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task RecordUserActivityAsync_WithinTheInterval_DoesNotWriteAgain()
    {
        var created = await _userManager.CreateUserAsync("throttled-user");
        var first = DateTime.UtcNow;
        await _userManager.RecordUserActivityAsync(created.Id, first);

        // Every request of an active session calls this; only one write a minute may reach the row.
        await _userManager.RecordUserActivityAsync(created.Id, first.AddSeconds(5));

        var stored = _userManager.GetUserById(created.Id);
        Assert.Equal(first, stored!.LastActivityDate!.Value, TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public async Task RecordUserActivityAsync_AfterTheInterval_WritesAgain()
    {
        var created = await _userManager.CreateUserAsync("stale-user");
        var first = DateTime.UtcNow;
        await _userManager.RecordUserActivityAsync(created.Id, first);

        var later = first.AddMinutes(2);
        await _userManager.RecordUserActivityAsync(created.Id, later);

        var stored = _userManager.GetUserById(created.Id);
        Assert.Equal(later, stored!.LastActivityDate!.Value, TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public async Task RenameUser_MakesTheNewNameVisible()
    {
        var created = await _userManager.CreateUserAsync("old-name");
        Assert.Equal("old-name", _userManager.GetUserById(created.Id)!.Username);

        await _userManager.RenameUser(created.Id, "old-name", "new-name");

        Assert.Equal("new-name", _userManager.GetUserById(created.Id)!.Username);
    }

    [Fact]
    public async Task DeleteUserAsync_StopsServingTheUser()
    {
        var keep = await _userManager.CreateUserAsync("keep-user");
        var doomed = await _userManager.CreateUserAsync("doomed-user");
        Assert.NotNull(_userManager.GetUserById(doomed.Id));

        await _userManager.DeleteUserAsync(doomed.Id);

        Assert.Null(_userManager.GetUserById(doomed.Id));
        Assert.NotNull(_userManager.GetUserById(keep.Id));
    }

    private static UserPolicy PolicyFor(User user, bool isAdministrator = false, bool isDisabled = false)
        => new()
        {
            IsAdministrator = isAdministrator,
            IsDisabled = isDisabled,
            AuthenticationProviderId = user.AuthenticationProviderId,
            PasswordResetProviderId = user.PasswordResetProviderId
        };

    private UserManager CreateUserManager()
    {
        var applicationPaths = new Mock<IServerApplicationPaths>();
        applicationPaths.Setup(p => p.ProgramDataPath).Returns(System.IO.Path.GetTempPath());

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        configurationManager.Setup(c => c.ApplicationPaths).Returns(applicationPaths.Object);

        var appHost = new Mock<IApplicationHost>();

        IAuthenticationProvider[] authenticationProviders =
        [
            new InvalidAuthProvider(),
            new DefaultAuthenticationProvider(NullLogger<DefaultAuthenticationProvider>.Instance, new Mock<ICryptoProvider>().Object)
        ];

        return new UserManager(
            CreateDbContextFactory(),
            new Mock<IEventManager>().Object,
            new Mock<INetworkManager>().Object,
            appHost.Object,
            new Mock<IImageProcessor>().Object,
            NullLogger<UserManager>.Instance,
            configurationManager.Object,
            [new DefaultPasswordResetProvider(configurationManager.Object, appHost.Object)],
            authenticationProviders);
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
