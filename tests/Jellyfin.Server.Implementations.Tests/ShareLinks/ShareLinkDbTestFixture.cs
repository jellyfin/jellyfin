using System;
using System.IO;
using System.Threading;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Database.Providers.Sqlite.Migrations;
using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Jellyfin.Server.Implementations.Tests.ShareLinks;

/// <summary>
/// Base fixture for the share link tests. Each test instance gets its own real SQLite database on a
/// temporary file that is migrated with the production migrations, so applying <c>AddShareLinks</c>
/// (including the ShareLinkKey backfill) is exercised by every test. Foreign keys are enabled so the
/// cascade delete from <c>Users</c> to <c>ShareLinks</c> behaves like at runtime.
/// </summary>
public abstract class ShareLinkDbTestFixture : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;

    protected ShareLinkDbTestFixture()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"jellyfin-sharelinks-{Guid.NewGuid():N}.db");
        ApplicationPaths = new Mock<IApplicationPaths>().Object;
        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(
                $"Data Source={_dbPath};Foreign Keys=True",
                options => options.MigrationsAssembly(typeof(SqliteDesignTimeJellyfinDbFactory).Assembly))
            .Options;

        using var context = CreateDbContext();
        context.Database.Migrate();
    }

    protected IApplicationPaths ApplicationPaths { get; }

    /// <summary>
    /// Creates a new <see cref="JellyfinDbContext"/> on its own connection to the test database.
    /// </summary>
    /// <returns>The context.</returns>
    protected JellyfinDbContext CreateDbContext() => new(
        _dbOptions,
        NullLogger<JellyfinDbContext>.Instance,
        new SqliteDatabaseProvider(ApplicationPaths, NullLogger<SqliteDatabaseProvider>.Instance),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    /// <summary>
    /// Creates a context factory whose contexts run on their own connections to the test database.
    /// </summary>
    /// <returns>The factory.</returns>
    protected IDbContextFactory<JellyfinDbContext> CreateDbContextFactory()
    {
        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDbContext);

        return factory.Object;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the test database file.
    /// </summary>
    /// <param name="disposing">Whether managed resources should be released.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            SqliteConnection.ClearAllPools();
            File.Delete(_dbPath);
        }
    }
}
