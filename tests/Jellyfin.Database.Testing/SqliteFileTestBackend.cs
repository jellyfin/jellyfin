using System;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jellyfin.Database.Testing;

/// <summary>
/// Creates SQLite databases as files in a temporary directory, through the same <see cref="LeasedDatabasePool"/> that
/// the engines without cheap database copies use. It exists to test that pool without a database server, so tests that
/// merely need SQLite take <see cref="SqliteTestBackend"/>. Disposing it drops every database.
/// </summary>
public sealed class SqliteFileTestBackend : ITestDatabaseBackend, IDisposable
{
    private readonly LeasedDatabasePool _pool;

    public SqliteFileTestBackend()
    {
        var server = new SqliteFileServer();
        DatabaseDirectory = server.DatabaseDirectory;
        _pool = new LeasedDatabasePool(server);
    }

    public TestDbProvider Provider => TestDbProvider.Sqlite;

    public Version ServerVersion => SqliteTestBackend.Instance.ServerVersion;

    public string ReportedServerVersion => SqliteTestBackend.Instance.ReportedServerVersion;

    /// <summary>
    /// Gets the directory that holds the database files, which exists until this backend is disposed.
    /// </summary>
    public string DatabaseDirectory { get; }

    public TestDatabase Create(TestSchemaSource source, IInterceptor[] interceptors, bool fresh = false)
    {
        return _pool.Lease(source, interceptors, fresh);
    }

    public TestDatabase CreateEmpty()
    {
        return _pool.CreateEmpty();
    }

    public void Dispose()
    {
        _pool.Dispose();
    }
}
