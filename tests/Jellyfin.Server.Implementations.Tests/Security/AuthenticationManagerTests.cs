using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Security;

public sealed class AuthenticationManagerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly AuthenticationManager _authenticationManager;

    public AuthenticationManagerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var ctx = CreateDbContext())
        {
            ctx.Database.EnsureCreated();
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDbContext);

        _authenticationManager = new AuthenticationManager(factory.Object);
    }

    public void Dispose() => _connection.Dispose();

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }

    [Fact]
    public async Task CreateApiKey_ReturnsInfoMatchingPersistedKey()
    {
        var result = await _authenticationManager.CreateApiKey("test-app");

        Assert.Equal("test-app", result.AppName);
        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.NotEqual(default, result.DateCreated);
        Assert.Equal(string.Empty, result.DeviceId);
        Assert.Equal(string.Empty, result.DeviceName);
        Assert.Equal(string.Empty, result.AppVersion);

        using var ctx = CreateDbContext();
        var persisted = Assert.Single(ctx.ApiKeys);
        Assert.Equal(result.AccessToken, persisted.AccessToken);
    }
}
