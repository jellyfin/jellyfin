using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Common;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users;

/// <summary>
/// Tests that reproduce the DbUpdateConcurrencyException race condition
/// in UpdateUserInternalAsync when LockingBehavior is NoLock.
/// See: https://github.com/jellyfin/jellyfin/issues/16353
/// </summary>
public sealed class UserUpdateConcurrencyTests : IDisposable
{
    private readonly SqliteConnection _keepAliveConnection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;

    public UserUpdateConcurrencyTests()
    {
        // Shared in-memory SQLite database — the keep-alive connection prevents
        // the database from being destroyed when individual contexts are disposed.
        var dbName = Guid.NewGuid().ToString("N");
        _keepAliveConnection = new SqliteConnection($"DataSource=file:{dbName}?mode=memory&cache=shared");
        _keepAliveConnection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite($"DataSource=file:{dbName}?mode=memory&cache=shared")
            .Options;

        // Create the schema.
        using var ctx = CreateDbContext();
        ctx.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _keepAliveConnection.Dispose();
    }

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }

    private User SeedUser()
    {
        var user = new User(
            "testuser_" + Guid.NewGuid().ToString("N")[..8],
            "Jellyfin.Server.Implementations.Users.DefaultAuthenticationProvider",
            "Jellyfin.Server.Implementations.Users.DefaultPasswordResetProvider");

        using var ctx = CreateDbContext();
        ctx.Users.Add(user);
        ctx.SaveChanges();

        return user;
    }

    private UserManager CreateUserManager()
    {
        var dbContextFactory = new TestDbContextFactory(this);
        var cryptoProvider = Mock.Of<MediaBrowser.Model.Cryptography.ICryptoProvider>();
        var appHost = Mock.Of<IApplicationHost>();

        var appPaths = new Mock<MediaBrowser.Controller.IServerApplicationPaths>();
        appPaths.Setup(p => p.ProgramDataPath).Returns(Path.GetTempPath());

        var configManager = new Mock<IServerConfigurationManager>();
        configManager.Setup(c => c.ApplicationPaths).Returns(appPaths.Object);

        return new UserManager(
            dbContextFactory,
            Mock.Of<IEventManager>(),
            Mock.Of<INetworkManager>(),
            appHost,
            Mock.Of<IImageProcessor>(),
            NullLogger<UserManager>.Instance,
            configManager.Object,
            new IPasswordResetProvider[]
            {
                new DefaultPasswordResetProvider(configManager.Object, appHost)
            },
            new IAuthenticationProvider[]
            {
                new InvalidAuthProvider(),
                new DefaultAuthenticationProvider(
                    NullLogger<DefaultAuthenticationProvider>.Instance,
                    cryptoProvider)
            });
    }

    /// <summary>
    /// Reproduces the single-request sequential scenario: within one auth flow,
    /// two separate UpdateUserAsync calls on the same user via separate DbContexts.
    /// Validates baseline sequential behavior works.
    /// </summary>
    [Fact]
    public async Task UpdateUser_SequentialSavesWithSeparateContexts_Succeeds()
    {
        var user = SeedUser();

        // Step 1: simulate AuthenticateUser's UpdateUserAsync
        user.LastActivityDate = DateTime.UtcNow;
        user.LastLoginDate = DateTime.UtcNow;
        user.InvalidLoginAttemptCount = 0;
        await using (var ctx1 = CreateDbContext())
        {
            ctx1.Users.Attach(user);
            ctx1.Entry(user).State = EntityState.Modified;
            await ctx1.SaveChangesAsync();
        }

        // Step 3: simulate LogSessionActivity's UpdateUserAsync
        var expectedDate = DateTime.UtcNow;
        user.LastActivityDate = expectedDate;
        await using (var ctx2 = CreateDbContext())
        {
            ctx2.Users.Attach(user);
            ctx2.Entry(user).State = EntityState.Modified;
            await ctx2.SaveChangesAsync();
        }

        // Verify the final update was persisted.
        await using var verifyCtx = CreateDbContext();
        var savedUser = await verifyCtx.Users.FindAsync(user.Id);
        Assert.NotNull(savedUser);
        Assert.Equal(expectedDate, savedUser!.LastActivityDate);
    }

    /// <summary>
    /// Proves the EF Core concurrency check works: two contexts load the same
    /// user, both modify it, the first save wins, the second throws
    /// DbUpdateConcurrencyException.
    /// </summary>
    [Fact]
    public async Task UpdateUser_ConcurrentAttachThenSave_ThrowsConcurrencyException()
    {
        var user = SeedUser();

        await using var ctxA = CreateDbContext();
        await using var ctxB = CreateDbContext();

        var userA = await ctxA.Users.FindAsync(user.Id);
        var userB = await ctxB.Users.FindAsync(user.Id);

        Assert.NotNull(userA);
        Assert.NotNull(userB);

        userA!.LastActivityDate = DateTime.UtcNow;
        userB!.LastActivityDate = DateTime.UtcNow.AddSeconds(1);

        // First save succeeds, DB RowVersion increments.
        await ctxA.SaveChangesAsync();

        // Second save — stale RowVersion — must throw.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => ctxB.SaveChangesAsync());
    }

    /// <summary>
    /// Multiple parallel tasks load and update the same user row.
    /// At least one should fail with DbUpdateConcurrencyException.
    /// </summary>
    [Fact]
    public async Task UpdateUser_ParallelUpdates_AtLeastOneThrowsConcurrencyException()
    {
        var user = SeedUser();

        const int concurrency = 5;
        var exceptions = new Exception?[concurrency];

        var tasks = new Task[concurrency];
        for (int i = 0; i < concurrency; i++)
        {
            int index = i;
            tasks[index] = Task.Run(async () =>
            {
                try
                {
                    await using var ctx = CreateDbContext();
                    var localUser = await ctx.Users.FindAsync(user.Id);
                    localUser!.LastActivityDate = DateTime.UtcNow.AddSeconds(index);
                    await ctx.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    exceptions[index] = ex;
                }
            });
        }

        await Task.WhenAll(tasks);

        Assert.Contains(exceptions, ex => ex is DbUpdateConcurrencyException);
    }

    /// <summary>
    /// Validates that <see cref="UserManager.UpdateUserAsync"/> succeeds even
    /// when another concurrent operation modifies the User row between the
    /// Attach and SaveChangesAsync calls. The retry logic in the fix should
    /// reload the DB state and re-apply the in-memory changes.
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_WithConcurrentModification_RetriesAndSucceeds()
    {
        var userManager = CreateUserManager();
        var user = SeedUser();

        // Force the user into UserManager's internal cache so UpdateUserAsync works.
        // We do this by re-reading from the DB through a context so it's detached.
        // But UserManager loaded users in its constructor, so it should already
        // have users from the DB. Let's add our test user to the DB first and
        // create a new UserManager that loads it.
        var manager = CreateUserManager();

        // Now simulate a concurrent modification: update the user row in the DB
        // directly (incrementing RowVersion) before UserManager's UpdateUserAsync runs.
        // This mimics another request having just saved the same user.
        await using (var interferingCtx = CreateDbContext())
        {
            var dbUser = await interferingCtx.Users.FindAsync(user.Id);
            Assert.NotNull(dbUser);
            dbUser!.LastActivityDate = DateTime.UtcNow.AddHours(-1);
            await interferingCtx.SaveChangesAsync();
        }

        // The user object in memory still has the old RowVersion.
        // With the fix, UpdateUserAsync should retry and succeed.
        user.LastActivityDate = DateTime.UtcNow;
        user.LastLoginDate = DateTime.UtcNow;

        var exception = await Record.ExceptionAsync(
            () => manager.UpdateUserAsync(user));

        Assert.Null(exception);

        // Verify the values were actually persisted.
        await using var verifyCtx = CreateDbContext();
        var savedUser = await verifyCtx.Users.FindAsync(user.Id);
        Assert.NotNull(savedUser);
        Assert.NotNull(savedUser!.LastActivityDate);
        Assert.NotNull(savedUser.LastLoginDate);
    }

    /// <summary>
    /// Validates that property changes are preserved through the retry.
    /// When a concurrency conflict occurs, the fix reloads DB values but
    /// re-applies the caller's in-memory changes (client-wins).
    /// </summary>
    [Fact]
    public async Task UpdateUserAsync_RetryPreservesPropertyChanges()
    {
        var manager = CreateUserManager();
        var user = SeedUser();
        manager = CreateUserManager(); // Reload with the seeded user.

        var expectedDate = new DateTime(2026, 3, 6, 12, 0, 0, DateTimeKind.Utc);

        // Another operation modifies the row first, making our RowVersion stale.
        await using (var interferingCtx = CreateDbContext())
        {
            var dbUser = await interferingCtx.Users.FindAsync(user.Id);
            dbUser!.LastActivityDate = DateTime.UtcNow.AddDays(-10);
            await interferingCtx.SaveChangesAsync();
        }

        // Our intended update — should survive the retry.
        user.LastActivityDate = expectedDate;
        await manager.UpdateUserAsync(user);

        // Verify our value won (client-wins resolution).
        await using var verifyCtx = CreateDbContext();
        var savedUser = await verifyCtx.Users.FindAsync(user.Id);
        Assert.NotNull(savedUser);
        Assert.Equal(expectedDate, savedUser!.LastActivityDate);
    }

    /// <summary>
    /// Factory that creates DbContext instances pointing to our shared in-memory DB.
    /// </summary>
    private sealed class TestDbContextFactory : IDbContextFactory<JellyfinDbContext>
    {
        private readonly UserUpdateConcurrencyTests _fixture;

        public TestDbContextFactory(UserUpdateConcurrencyTests fixture)
        {
            _fixture = fixture;
        }

        public JellyfinDbContext CreateDbContext() => _fixture.CreateDbContext();
    }
}
