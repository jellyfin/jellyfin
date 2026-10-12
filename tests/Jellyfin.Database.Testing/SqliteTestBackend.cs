using System;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Providers.Sqlite;
using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Jellyfin.Database.Testing;

public sealed class SqliteTestBackend : ITestDatabaseBackend
{
    private SqliteTestBackend()
    {
    }

    public static SqliteTestBackend Instance { get; } = new();

    public TestDbProvider Provider => TestDbProvider.Sqlite;

    public Version ServerVersion { get; } = ReadServerVersion();

    public TestDatabase Create(TestSchemaSource source, IInterceptor[] interceptors)
    {
        var applicationPaths = new Mock<IApplicationPaths>().Object;
        IJellyfinDatabaseProvider CreateProvider() => new SqliteDatabaseProvider(applicationPaths, NullLogger<SqliteDatabaseProvider>.Instance);

        // The connection owns an in-memory database, so it has to stay open until the test is done with it.
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(connection, sqliteOptions => sqliteOptions.MigrationsAssembly(typeof(SqliteDatabaseProvider).Assembly))
            .AddInterceptors(interceptors)
            .Options;

        source.Apply(options, CreateProvider());

        return new TestDatabase(options, CreateProvider, connection.Dispose);
    }

    private static Version ReadServerVersion()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return Version.Parse(connection.ServerVersion);
    }
}
