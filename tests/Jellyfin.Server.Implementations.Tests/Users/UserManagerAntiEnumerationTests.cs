using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Data.Events.Users;
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
using MediaBrowser.Model.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users;

public sealed class UserManagerAntiEnumerationTests : IDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConnection _keepAliveConnection;
    private readonly RecordingEventManager _eventManager = new();
    private readonly Mock<IDbContextFactory<JellyfinDbContext>> _dbContextFactory = new();
    private readonly Mock<IServerConfigurationManager> _configManager = new();
    private readonly Mock<INetworkManager> _networkManager = new();
    private readonly UserManager _userManager;

    public UserManagerAntiEnumerationTests()
    {
        // The user manager and the test's own queries each open their own context, possibly at the same time,
        // so every context gets its own connection to a shared in-memory database.
        _connectionString = $"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();

        using (var ctx = CreateDbContext())
        {
            ctx.Database.EnsureCreated();
        }

        _dbContextFactory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);
        _dbContextFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDbContext);

        var appPaths = new Mock<IServerApplicationPaths>();
        appPaths.Setup(x => x.ProgramDataPath).Returns(Path.GetTempPath());
        _configManager.Setup(x => x.ApplicationPaths).Returns(appPaths.Object);

        _networkManager.Setup(x => x.IsInLocalNetwork(It.IsAny<string>())).Returns(true);

        _userManager = CreateUserManager(new CountingCryptoProvider());
    }

    public void Dispose()
    {
        _userManager.Dispose();
        _keepAliveConnection.Dispose();
    }

    [Fact]
    public async Task AuthenticateUser_WrongPassword_RecordsFailedAttempt()
    {
        var user = await _userManager.CreateUserAsync("someone");

        Assert.Null(await _userManager.AuthenticateUser("someone", "wrong", "127.0.0.1", true));

        Assert.Equal(1, await GetInvalidLoginAttemptCountAsync(user.Id));
    }

    [Fact]
    public async Task AuthenticateUser_TooManyWrongPasswords_LocksOutUser()
    {
        var user = await _userManager.CreateUserAsync("someone");
        await using (var context = CreateDbContext())
        {
            await context.Users
                .Where(e => e.Id.Equals(user.Id))
                .ExecuteUpdateAsync(e => e.SetProperty(f => f.LoginAttemptsBeforeLockout, 2), TestContext.Current.CancellationToken);
        }

        Assert.Null(await _userManager.AuthenticateUser("someone", "wrong", "127.0.0.1", true));
        Assert.Equal(1, await GetInvalidLoginAttemptCountAsync(user.Id));
        Assert.DoesNotContain(_eventManager.Published, e => e is UserLockedOutEventArgs);

        Assert.Null(await _userManager.AuthenticateUser("someone", "wrong", "127.0.0.1", true));
        Assert.Contains(_eventManager.Published, e => e is UserLockedOutEventArgs);
        Assert.True(_userManager.GetUserById(user.Id)!.HasPermission(PermissionKind.IsDisabled));
    }

    [Fact]
    public async Task AuthenticateUser_DifferentUnknownUsernames_DoNotWaitForEachOther()
    {
        using var cryptoProvider = new GatedCryptoProvider();
        using var userManager = CreateUserManager(cryptoProvider);

        var first = Task.Run(() => userManager.AuthenticateUser("ghost1", "wrong", "127.0.0.1", true));
        Task<User?>? second = null;
        try
        {
            Assert.True(await WaitForAsync(() => Task.FromResult(cryptoProvider.InProgress == 1)), "The first login did not reach the password check");

            second = Task.Run(() => userManager.AuthenticateUser("ghost2", "wrong", "127.0.0.1", true));

            Assert.True(
                await WaitForAsync(() => Task.FromResult(cryptoProvider.InProgress == 2)),
                "The second login waited for the first one");
        }
        finally
        {
            cryptoProvider.Release();
        }

        await Assert.ThrowsAsync<AuthenticationException>(() => first);
        await Assert.ThrowsAsync<AuthenticationException>(() => second!);
    }

    [Fact]
    public async Task AuthenticateUser_SameUnknownUsername_StillRunsOneAtATime()
    {
        using var cryptoProvider = new GatedCryptoProvider();
        using var userManager = CreateUserManager(cryptoProvider);

        var first = Task.Run(() => userManager.AuthenticateUser("ghost", "wrong", "127.0.0.1", true));
        Task<User?>? second = null;
        try
        {
            Assert.True(await WaitForAsync(() => Task.FromResult(cryptoProvider.InProgress == 1)), "The first login did not reach the password check");

            second = Task.Run(() => userManager.AuthenticateUser("ghost", "wrong", "127.0.0.1", true));

            Assert.False(
                await WaitForAsync(() => Task.FromResult(cryptoProvider.InProgress == 2), TimeSpan.FromMilliseconds(500)),
                "The second login did not wait for the first one");
        }
        finally
        {
            cryptoProvider.Release();
        }

        await Assert.ThrowsAsync<AuthenticationException>(() => first);
        await Assert.ThrowsAsync<AuthenticationException>(() => second!);
    }

    private UserManager CreateUserManager(ICryptoProvider cryptoProvider)
    {
        var appHost = new Mock<IApplicationHost>();
        var dummyPasswordVerifier = new DummyPasswordVerifier(cryptoProvider);

        return new UserManager(
            _dbContextFactory.Object,
            _eventManager,
            _networkManager.Object,
            appHost.Object,
            new Mock<IImageProcessor>().Object,
            NullLogger<UserManager>.Instance,
            _configManager.Object,
            [new DefaultPasswordResetProvider(_configManager.Object, appHost.Object, NullLogger<DefaultPasswordResetProvider>.Instance)],
            [
                new DefaultAuthenticationProvider(NullLogger<DefaultAuthenticationProvider>.Instance, cryptoProvider, dummyPasswordVerifier),
                new InvalidAuthProvider(dummyPasswordVerifier)
            ]);
    }

    private JellyfinDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connectionString)
            .Options;

        return new JellyfinDbContext(
            options,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }

    private async Task<int> GetInvalidLoginAttemptCountAsync(Guid userId)
    {
        await using var context = CreateDbContext();
        return await context.Users
            .Where(e => e.Id.Equals(userId))
            .Select(e => e.InvalidLoginAttemptCount)
            .FirstAsync();
    }

    private static async Task<bool> WaitForAsync(Func<Task<bool>> condition, TimeSpan? timeout = null)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < (timeout ?? TimeSpan.FromSeconds(10))) // default timeout: 10 seconds
        {
            if (await condition())
            {
                return true;
            }

            await Task.Delay(25);
        }

        return false;
    }

    /// <summary>
    /// Holds every password check until <see cref="Release"/> is called, so a test can see which logins
    /// reach the check while another login is still inside it.
    /// </summary>
    private sealed class GatedCryptoProvider : ICryptoProvider, IDisposable
    {
        private readonly ManualResetEventSlim _released = new(false);
        private int _inProgress;

        public int InProgress => Volatile.Read(ref _inProgress);

        public string DefaultHashMethod => "PBKDF2-SHA512";

        public void Release() => _released.Set();

        public PasswordHash CreatePasswordHash(ReadOnlySpan<char> password)
            => new(DefaultHashMethod, new byte[] { 1 }, new byte[] { 2 });

        public bool Verify(PasswordHash hash, ReadOnlySpan<char> password)
        {
            Interlocked.Increment(ref _inProgress);
            _released.Wait(TimeSpan.FromSeconds(10));
            Interlocked.Decrement(ref _inProgress);
            return false;
        }

        public byte[] GenerateSalt() => new byte[] { 2 };

        public byte[] GenerateSalt(int length) => new byte[length];

        public void Dispose() => _released.Dispose();
    }

    private sealed class RecordingEventManager : IEventManager
    {
        public ConcurrentQueue<EventArgs> Published { get; } = new();

        public void Publish<T>(T eventArgs)
            where T : EventArgs
            => Published.Enqueue(eventArgs);

        public Task PublishAsync<T>(T eventArgs)
            where T : EventArgs
        {
            Published.Enqueue(eventArgs);
            return Task.CompletedTask;
        }
    }
}
