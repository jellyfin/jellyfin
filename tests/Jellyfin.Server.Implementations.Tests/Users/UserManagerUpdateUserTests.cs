using System;
using System.Collections.Generic;
using System.IO;
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

public sealed class UserManagerUpdateUserTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly UserManager _userManager;

    public UserManagerUpdateUserTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Create the schema
        using var ctx = CreateDbContext();
        ctx.Database.EnsureCreated();

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDbContext);

        var cryptoProvider = new Mock<ICryptoProvider>();
        var configManager = new Mock<IServerConfigurationManager>();
        configManager.SetupGet(x => x.Configuration).Returns(new ServerConfiguration());
        var appPaths = new Mock<IServerApplicationPaths>();
        appPaths.Setup(x => x.ProgramDataPath).Returns(Path.GetTempPath());
        configManager.Setup(x => x.ApplicationPaths).Returns(appPaths.Object);

        var appHost = new Mock<IApplicationHost>();

        var defaultAuthProvider = new DefaultAuthenticationProvider(
            NullLogger<DefaultAuthenticationProvider>.Instance,
            cryptoProvider.Object);
        var invalidAuthProvider = new InvalidAuthProvider();
        var defaultPasswordResetProvider = new DefaultPasswordResetProvider(
            configManager.Object,
            appHost.Object);

        _userManager = new UserManager(
            factory.Object,
            new NoopEventManager(),
            new Mock<INetworkManager>().Object,
            appHost.Object,
            new Mock<IImageProcessor>().Object,
            NullLogger<UserManager>.Instance,
            configManager.Object,
            [defaultPasswordResetProvider],
            [defaultAuthProvider, invalidAuthProvider]);
    }

    public void Dispose()
    {
        _userManager.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task UpdateUserAsync_DoesNotDetachPermissionsOrPreferences()
    {
        var user = await _userManager.CreateUserAsync("orphanuser");
        var permissionCount = user.Permissions.Count;
        var preferenceCount = user.Preferences.Count;

        user.LastActivityDate = DateTime.UtcNow;
        await _userManager.UpdateUserAsync(user);
        await _userManager.UpdateUserAsync(user);

        await using var context = CreateDbContext();
        Assert.Equal(permissionCount, await context.Permissions.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(preferenceCount, await context.Preferences.CountAsync(TestContext.Current.CancellationToken));
        Assert.All(
            await context.Permissions.ToListAsync(TestContext.Current.CancellationToken),
            permission => Assert.Equal(user.Id, permission.UserId));
        Assert.All(
            await context.Preferences.ToListAsync(TestContext.Current.CancellationToken),
            preference => Assert.Equal(user.Id, preference.UserId));
    }

    [Fact]
    public async Task UpdateUserAsync_WhenOnlyTheUserRowChanged_LeavesChildRowsUntouched()
    {
        var user = await _userManager.CreateUserAsync("churnuser");
        var before = await ReadChildRowsAsync();

        // A session activity stamp goes through the same path. It must not rewrite all 37 child
        // rows, which is what tearing the collections down and rebuilding them used to do.
        user.LastActivityDate = DateTime.UtcNow;
        await _userManager.UpdateUserAsync(user);

        Assert.Equal(before, await ReadChildRowsAsync());
    }

    [Fact]
    public async Task UpdateUserAsync_StaleActivityUpdatePreservesCurrentVideoLimit()
    {
        var user = await _userManager.CreateUserAsync("stalevideolimituser");
        var policy = _userManager.GetUserDto(user).Policy!;
        policy.MaxActiveVideoStreams = 2;
        await _userManager.UpdatePolicyAsync(user.Id, policy);
        Assert.Equal(0, user.MaxActiveVideoStreams);

        var activityDate = new DateTime(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);
        user.LastActivityDate = activityDate;
        await _userManager.UpdateUserAsync(user);

        await using var context = CreateDbContext();
        var stored = await context.Users.SingleAsync(u => u.Id.Equals(user.Id), TestContext.Current.CancellationToken);
        Assert.Equal(2, stored.MaxActiveVideoStreams);
        Assert.Equal(activityDate, stored.LastActivityDate);
    }

    [Fact]
    public async Task UpdatePolicyAsync_RaisesOnUserUpdated()
    {
        var user = await _userManager.CreateUserAsync("policyeventuser");

        User? updated = null;
        _userManager.OnUserUpdated += (_, e) => updated = e.Argument;

        await _userManager.UpdatePolicyAsync(
            user.Id,
            new UserPolicy
            {
                EnableAllDevices = false,
                AuthenticationProviderId = user.AuthenticationProviderId,
                PasswordResetProviderId = user.PasswordResetProviderId
            });

        Assert.NotNull(updated);
        Assert.Equal(user.Id, updated.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(int.MaxValue)]
    public async Task UpdatePolicyAsync_PersistsMaxActiveVideoStreams(int limit)
    {
        var user = await _userManager.CreateUserAsync("videolimituser");
        var policy = _userManager.GetUserDto(user).Policy!;
        policy.MaxActiveVideoStreams = limit;

        await _userManager.UpdatePolicyAsync(user.Id, policy);

        await using var context = CreateDbContext();
        var stored = await context.Users.SingleAsync(u => u.Id.Equals(user.Id), TestContext.Current.CancellationToken);
        Assert.Equal(limit, stored.MaxActiveVideoStreams);
        Assert.Equal(limit, _userManager.GetUserDto(_userManager.GetUserById(user.Id)!).Policy!.MaxActiveVideoStreams);
    }

    [Fact]
    public async Task UpdatePolicyAsync_RejectsNegativeVideoLimitBeforeChangingUser()
    {
        var user = await _userManager.CreateUserAsync("invalidvideolimituser");
        var policy = _userManager.GetUserDto(user).Policy!;
        policy.MaxActiveVideoStreams = 2;
        await _userManager.UpdatePolicyAsync(user.Id, policy);

        var updated = false;
        _userManager.OnUserUpdated += (_, _) => updated = true;
        policy.MaxActiveVideoStreams = -1;
        policy.MaxActiveSessions = 7;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _userManager.UpdatePolicyAsync(user.Id, policy));

        await using var context = CreateDbContext();
        var stored = await context.Users.SingleAsync(u => u.Id.Equals(user.Id), TestContext.Current.CancellationToken);
        Assert.Equal(2, stored.MaxActiveVideoStreams);
        Assert.Equal(0, stored.MaxActiveSessions);
        Assert.False(updated);
    }

    [Fact]
    public async Task UpdatePolicyAsync_OmittedVideoLimitPreservesLimitAndExplicitZeroDisablesIt()
    {
        var user = await _userManager.CreateUserAsync("omittedvideolimituser");
        var policy = _userManager.GetUserDto(user).Policy!;
        policy.MaxActiveVideoStreams = 2;
        await _userManager.UpdatePolicyAsync(user.Id, policy);

        var oldPolicy = new UserPolicy
        {
            AuthenticationProviderId = user.AuthenticationProviderId,
            PasswordResetProviderId = user.PasswordResetProviderId,
            MaxActiveSessions = 3
        };
        await _userManager.UpdatePolicyAsync(user.Id, oldPolicy);
        Assert.Equal(2, _userManager.GetUserById(user.Id)!.MaxActiveVideoStreams);

        oldPolicy.MaxActiveVideoStreams = 0;
        await _userManager.UpdatePolicyAsync(user.Id, oldPolicy);
        Assert.Equal(0, _userManager.GetUserById(user.Id)!.MaxActiveVideoStreams);
    }

    [Fact]
    public async Task UpdateUserAsync_AppliesPermissionAndPreferenceChanges()
    {
        var user = await _userManager.CreateUserAsync("policyuser");
        Assert.False(user.HasPermission(PermissionKind.IsAdministrator));

        user.SetPermission(PermissionKind.IsAdministrator, true);
        user.SetPreference(PreferenceKind.BlockedTags, ["spoilers"]);
        user.Permissions.Remove(user.Permissions.First(permission => permission.Kind == PermissionKind.EnableAllChannels));

        await _userManager.UpdateUserAsync(user);

        var reloaded = _userManager.GetUserById(user.Id)!;
        Assert.True(reloaded.HasPermission(PermissionKind.IsAdministrator));
        Assert.Equal(new[] { "spoilers" }, reloaded.GetPreference(PreferenceKind.BlockedTags));
        Assert.DoesNotContain(reloaded.Permissions, permission => permission.Kind == PermissionKind.EnableAllChannels);

        await using var context = CreateDbContext();
        Assert.Equal(reloaded.Permissions.Count, await context.Permissions.CountAsync(TestContext.Current.CancellationToken));
    }

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }

    /// <summary>
    /// Reads the identity and concurrency token of every permission and preference row.
    /// </summary>
    private async Task<List<(string Table, int Id, int Kind, uint RowVersion)>> ReadChildRowsAsync()
    {
        await using var context = CreateDbContext();
        var permissions = await context.Permissions
            .OrderBy(permission => permission.Id)
            .Select(permission => new ValueTuple<string, int, int, uint>("Permission", permission.Id, (int)permission.Kind, permission.RowVersion))
            .ToListAsync(TestContext.Current.CancellationToken);
        var preferences = await context.Preferences
            .OrderBy(preference => preference.Id)
            .Select(preference => new ValueTuple<string, int, int, uint>("Preference", preference.Id, (int)preference.Kind, preference.RowVersion))
            .ToListAsync(TestContext.Current.CancellationToken);

        return permissions.Concat(preferences).ToList();
    }

    private sealed class NoopEventManager : IEventManager
    {
        public void Publish<T>(T eventArgs)
            where T : EventArgs
        {
        }

        public Task PublishAsync<T>(T eventArgs)
            where T : EventArgs
            => Task.CompletedTask;
    }
}
