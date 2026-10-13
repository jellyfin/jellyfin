using System;
using System.Collections.Concurrent;
using System.Threading;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Providers.Sqlite;
using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Jellyfin.Database.Testing;

/// <summary>
/// Creates an in-memory SQLite database for every test instance. The schema is built once per
/// <see cref="TestSchemaSource"/> into a template database that every instance is copied from.
/// </summary>
public sealed class SqliteTestBackend : ITestDatabaseBackend
{
    private static readonly IApplicationPaths _applicationPaths = new Mock<IApplicationPaths>().Object;

    private readonly ConcurrentDictionary<TestSchemaSource, Lazy<SqliteConnection>> _templates = new();

    // A connection serves one caller at a time, so the copies of a template are made one after the other.
    private readonly Lock _copyLock = new();

    private SqliteTestBackend()
    {
    }

    public static SqliteTestBackend Instance { get; } = new();

    public TestDbProvider Provider => TestDbProvider.Sqlite;

    public Version ServerVersion { get; } = ReadServerVersion();

    public TestDatabase Create(TestSchemaSource source, IInterceptor[] interceptors, bool fresh = false)
    {
        var template = _templates.GetOrAdd(source, key => new Lazy<SqliteConnection>(() => CreateTemplate(key))).Value;

        // The connection owns an in-memory database, so it has to stay open until the test is done with it.
        var connection = OpenInMemory();
        try
        {
            lock (_copyLock)
            {
                template.BackupDatabase(connection);
            }
        }
        catch
        {
            connection.Dispose();
            throw;
        }

        return new TestDatabase(CreateOptions(connection, interceptors), CreateProvider, connection.Dispose);
    }

    public TestDatabase CreateEmpty()
    {
        var connection = OpenInMemory();
        return new TestDatabase(CreateOptions(connection, []), CreateProvider, connection.Dispose);
    }

    internal static IJellyfinDatabaseProvider CreateProvider()
    {
        return new SqliteDatabaseProvider(_applicationPaths, NullLogger<SqliteDatabaseProvider>.Instance);
    }

    internal static DbContextOptions<JellyfinDbContext> CreateOptions(string connectionString, IInterceptor[] interceptors)
    {
        return Build(builder => builder.UseSqlite(connectionString, ConfigureSqlite), interceptors);
    }

    private static SqliteConnection OpenInMemory()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static DbContextOptions<JellyfinDbContext> CreateOptions(SqliteConnection connection, IInterceptor[] interceptors)
    {
        return Build(builder => builder.UseSqlite(connection, ConfigureSqlite), interceptors);
    }

    private static void ConfigureSqlite(SqliteDbContextOptionsBuilder sqliteOptions)
    {
        sqliteOptions.MigrationsAssembly(typeof(SqliteDatabaseProvider).Assembly);
    }

    private static DbContextOptions<JellyfinDbContext> Build(Action<DbContextOptionsBuilder<JellyfinDbContext>> useSqlite, IInterceptor[] interceptors)
    {
        var builder = new DbContextOptionsBuilder<JellyfinDbContext>();
        useSqlite(builder);
        return builder.AddInterceptors(interceptors).Options;
    }

    private static SqliteConnection CreateTemplate(TestSchemaSource source)
    {
        var connection = OpenInMemory();
        try
        {
            source.Apply(CreateOptions(connection, []), CreateProvider());
        }
        catch
        {
            connection.Dispose();
            throw;
        }

        return connection;
    }

    private static Version ReadServerVersion()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return Version.Parse(connection.ServerVersion);
    }
}
