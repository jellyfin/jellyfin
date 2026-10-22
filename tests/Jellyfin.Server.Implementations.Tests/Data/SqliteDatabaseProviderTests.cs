using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

/// <summary>
/// Tests for <see cref="SqliteDatabaseProvider"/> when the database file lives at a custom location
/// configured through the provider's "path" option instead of the default data directory.
/// See https://github.com/jellyfin/jellyfin/issues/18475.
/// </summary>
public sealed class SqliteDatabaseProviderTests : IDisposable
{
    private readonly string _testRoot;
    private readonly string _dataPath;
    private readonly string _customDbPath;

    public SqliteDatabaseProviderTests()
    {
        // Under the test assembly's output directory so the files land on the same volume as the
        // checkout on every CI runner (the system temp dir can be a small separate drive).
        _testRoot = Path.Combine(AppContext.BaseDirectory, "jellyfin-sqlite-provider-tests-" + Guid.NewGuid().ToString("N"));
        _dataPath = Path.Combine(_testRoot, "Data");
        _customDbPath = Path.Combine(_testRoot, "CustomDb", "jellyfin.db");
        Directory.CreateDirectory(_dataPath);
        Directory.CreateDirectory(Path.GetDirectoryName(_customDbPath)!);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, true);
        }
    }

    [Fact]
    public async Task MigrationBackupFast_WithCustomDatabasePath_BacksUpTheConfiguredDatabase()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var provider = CreateProviderWithCustomPath(out var createDbContext);
        provider.DbContextFactory = CreateDbContextFactory(createDbContext);

        await using (var context = createDbContext())
        {
            await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(true);
        }

        Assert.True(File.Exists(_customDbPath));

        var key = await provider.MigrationBackupFast(cancellationToken).ConfigureAwait(true);

        var backupFile = Path.Combine(_dataPath, "SQLiteBackups", $"{key}_jellyfin.db");
        Assert.True(File.Exists(backupFile));
        Assert.Equal(new FileInfo(_customDbPath).Length, new FileInfo(backupFile).Length);
    }

    [Fact]
    public async Task RestoreBackupFast_WithCustomDatabasePath_RestoresToTheConfiguredDatabase()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var provider = CreateProviderWithCustomPath(out var createDbContext);
        provider.DbContextFactory = CreateDbContextFactory(createDbContext);

        var key = "20261022000000";
        var backupFile = Path.Combine(_dataPath, "SQLiteBackups", $"{key}_jellyfin.db");
        Directory.CreateDirectory(Path.GetDirectoryName(backupFile)!);
        var expected = new byte[] { 0x53, 0x51, 0x4C, 0x69, 0x74, 0x65, 0x20, 0x66, 0x6F, 0x72, 0x6D, 0x61, 0x74, 0x20, 0x33, 0x00 };
        await File.WriteAllBytesAsync(backupFile, expected, cancellationToken).ConfigureAwait(true);
        await File.WriteAllBytesAsync(_customDbPath, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, cancellationToken).ConfigureAwait(true);

        await provider.RestoreBackupFast(key, cancellationToken).ConfigureAwait(true);

        Assert.Equal(expected, await File.ReadAllBytesAsync(_customDbPath, cancellationToken).ConfigureAwait(true));
    }

    private SqliteDatabaseProvider CreateProviderWithCustomPath(out Func<JellyfinDbContext> createDbContext)
    {
        var applicationPaths = new Mock<IApplicationPaths>();
        applicationPaths.Setup(p => p.DataPath).Returns(_dataPath);

        var provider = new SqliteDatabaseProvider(applicationPaths.Object, NullLogger<SqliteDatabaseProvider>.Instance);

        var optionsBuilder = new DbContextOptionsBuilder<JellyfinDbContext>();
        provider.Initialise(optionsBuilder, new DatabaseConfigurationOptions
        {
            DatabaseType = "Jellyfin-SQLite",
            CustomProviderOptions = new CustomDatabaseOptions
            {
                PluginName = "Jellyfin-SQLite",
                PluginAssembly = "Jellyfin.Database.Providers.Sqlite",
                ConnectionString = string.Empty,
                Options =
                {
                    new CustomDatabaseOption { Key = "path", Value = _customDbPath }
                }
            }
        });

        var options = optionsBuilder.Options;
        createDbContext = () => new JellyfinDbContext(
            options,
            NullLogger<JellyfinDbContext>.Instance,
            provider,
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
        return provider;
    }

    private static IDbContextFactory<JellyfinDbContext> CreateDbContextFactory(Func<JellyfinDbContext> createDbContext)
    {
        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(createDbContext);
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(createDbContext);
        return factory.Object;
    }
}
