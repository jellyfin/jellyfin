using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Jellyfin.Database.Implementations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jellyfin.Database.Testing;

/// <summary>
/// Keeps each SQLite database as a file in a temporary directory.
/// </summary>
internal sealed class SqliteFileServer : IPooledDatabaseServer
{
    public SqliteFileServer()
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

    public string GetSchemaFingerprint(string database)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = GetPath(database), Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT type, name, tbl_name, sql FROM sqlite_master ORDER BY type, name";
        using var reader = command.ExecuteReader();
        var description = new StringBuilder();
        while (reader.Read())
        {
            for (var column = 0; column < reader.FieldCount; column++)
            {
                description.Append(reader.IsDBNull(column) ? string.Empty : reader.GetString(column)).Append('\u001f');
            }

            description.Append('\u001e');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(description.ToString())));
    }

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
