using Microsoft.Data.Sqlite;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

public class BundledSqliteTests
{
    [Fact]
    public void ForeignKeys_AreEnforcedByDefault()
    {
        // SQLite's own default is off; the foreign keys only hold because the bundled e_sqlite3 is built with
        // SQLITE_DEFAULT_FOREIGN_KEYS=1, and no connection turns them on.
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys;";

        Assert.Equal(1L, command.ExecuteScalar());
    }
}
