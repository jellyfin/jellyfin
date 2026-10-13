using System;
using System.IO;
using Jellyfin.Database.Implementations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
        var server = new FileServer();
        DatabaseDirectory = server.DatabaseDirectory;
        _pool = new LeasedDatabasePool(server);
    }

    public TestDbProvider Provider => TestDbProvider.Sqlite;

    public Version ServerVersion => SqliteTestBackend.Instance.ServerVersion;

    /// <summary>
    /// Gets the directory that holds the database files, which exists until this backend is disposed.
    /// </summary>
    public string DatabaseDirectory { get; }

    public TestDatabase Create(TestSchemaSource source, IInterceptor[] interceptors, bool fresh = false)
    {
        return _pool.Lease(source, interceptors);
    }

    public TestDatabase CreateEmpty()
    {
        return _pool.CreateEmpty();
    }

    public void Dispose()
    {
        _pool.Dispose();
    }

    private sealed class FileServer : IPooledDatabaseServer
    {
        public FileServer()
        {
            DatabaseDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"jellyfin-test-databases-{Guid.NewGuid():N}")).FullName;
        }

        public string DatabaseDirectory { get; }

        public string CreateDatabase() => $"jf_{Guid.NewGuid():N}";

        public DbContextOptions<JellyfinDbContext> CreateOptions(string database, IInterceptor[] interceptors)
        {
            var connectionString = new SqliteConnectionStringBuilder { DataSource = GetPath(database), Pooling = false }.ConnectionString;
            return SqliteTestBackend.CreateOptions(connectionString, interceptors);
        }

        public IJellyfinDatabaseProvider CreateProvider() => SqliteTestBackend.CreateProvider();

        public void DropDatabase(string database)
        {
            var path = GetPath(database);
            foreach (var file in new[] { path, path + "-journal", path + "-wal", path + "-shm" })
            {
                File.Delete(file);
            }
        }

        public void Dispose()
        {
            Directory.Delete(DatabaseDirectory, recursive: true);
        }

        private string GetPath(string database) => Path.Combine(DatabaseDirectory, database + ".db");
    }
}
