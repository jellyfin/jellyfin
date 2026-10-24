using Jellyfin.Database.Implementations;
using Jellyfin.Database.Providers.Sqlite;
using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// An in-memory SQLite database, empty until the fixture creates the schema. The connection owns the database, so it stays open for the lifetime of this object.
/// </summary>
public sealed class SqliteTestDatabase : TestDatabase
{
    private readonly SqliteConnection _connection;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public override DbContextOptionsBuilder<JellyfinDbContext> Configure(DbContextOptionsBuilder<JellyfinDbContext> builder)
        => builder.UseSqlite(_connection);

    public override IJellyfinDatabaseProvider CreateProvider(IApplicationPaths applicationPaths)
        => new SqliteDatabaseProvider(applicationPaths, NullLogger<SqliteDatabaseProvider>.Instance);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
