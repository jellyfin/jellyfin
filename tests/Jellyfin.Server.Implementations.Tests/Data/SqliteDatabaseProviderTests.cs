using System;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Providers.Sqlite;
using MediaBrowser.Common.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

public sealed class SqliteDatabaseProviderTests
{
    [Fact]
    public async Task MigrationBackupFast_UsesConfiguredDataSourceAndRestoresWithoutSidecars()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var testDirectory = Path.Combine(Path.GetTempPath(), "jellyfin-sqlite-backup-tests", Guid.NewGuid().ToString("N"));
        var dataDirectory = Path.Combine(testDirectory, "data");
        var databasePath = Path.Combine(testDirectory, "custom", "database.sqlite");
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        var applicationPaths = new Mock<IApplicationPaths>();
        applicationPaths.SetupGet(paths => paths.DataPath).Returns(dataDirectory);
        var provider = new SqliteDatabaseProvider(applicationPaths.Object, NullLogger<SqliteDatabaseProvider>.Instance);
        provider.Initialise(
            new DbContextOptionsBuilder(),
            new DatabaseConfigurationOptions
            {
                DatabaseType = "Jellyfin-SQLite",
                CustomProviderOptions = new CustomDatabaseOptions
                {
                    PluginName = string.Empty,
                    PluginAssembly = string.Empty,
                    ConnectionString = string.Empty,
                    Options =
                    [
                        new CustomDatabaseOption
                        {
                            Key = "path",
                            Value = databasePath
                        }
                    ]
                }
            });

        try
        {
            string backupKey;
            await using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Pooling = false
            }.ToString()))
            {
                await connection.OpenAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText =
                    """
                    PRAGMA journal_mode = WAL;
                    PRAGMA wal_autocheckpoint = 0;
                    CREATE TABLE TestValues (Value TEXT NOT NULL);
                    INSERT INTO TestValues VALUES ('original');
                    """;
                await command.ExecuteNonQueryAsync(cancellationToken);

                backupKey = await provider.MigrationBackupFast(cancellationToken);

                command.CommandText = "UPDATE TestValues SET Value = 'mutated';";
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            SqliteConnection.ClearAllPools();
            await File.WriteAllTextAsync(databasePath + "-shm", "stale", cancellationToken);
            await File.WriteAllTextAsync(databasePath + "-wal", "stale", cancellationToken);

            await provider.RestoreBackupFast(backupKey, cancellationToken);

            Assert.False(File.Exists(databasePath + "-shm"));
            Assert.False(File.Exists(databasePath + "-wal"));
            await using var restoredConnection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Pooling = false
            }.ToString());
            await restoredConnection.OpenAsync(cancellationToken);
            await using var restoredCommand = restoredConnection.CreateCommand();
            restoredCommand.CommandText = "SELECT Value FROM TestValues;";
            Assert.Equal("original", await restoredCommand.ExecuteScalarAsync(cancellationToken));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }
    }
}
