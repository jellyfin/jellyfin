#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// The migration routine for checking if the current instance of Jellyfin is compatiable to be upgraded.
/// </summary>
[JellyfinMigration("2025-04-20T19:30:00", nameof(MigrateLibraryDbCompatibilityCheck))]
public class MigrateLibraryDbCompatibilityCheck : IAsyncMigrationRoutine
{
    private const string DbFilename = "library.db";
    private readonly IStartupLogger _logger;
    private readonly IServerApplicationPaths _paths;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrateLibraryDbCompatibilityCheck"/> class.
    /// </summary>
    /// <param name="startupLogger">The startup logger.</param>
    /// <param name="paths">The Path service.</param>
    public MigrateLibraryDbCompatibilityCheck(IStartupLogger<MigrateLibraryDbCompatibilityCheck> startupLogger, IServerApplicationPaths paths)
    {
        _logger = startupLogger;
        _paths = paths;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var dataPath = _paths.DataPath;
        var libraryDbPath = Path.Combine(dataPath, DbFilename);
        if (!File.Exists(libraryDbPath))
        {
            _logger.LogError("Cannot migrate {LibraryDb} as it does not exist..", libraryDbPath);
            return;
        }

        using var connection = new SqliteConnection($"Filename={libraryDbPath};Mode=ReadOnly");
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        CheckMigratableVersion(connection);
        await connection.CloseAsync().ConfigureAwait(false);
    }

    private static void CheckMigratableVersion(SqliteConnection connection)
    {
        CheckColumnExistance(connection, "TypedBaseItems", "lufs");
        CheckColumnExistance(connection, "TypedBaseItems", "normalizationgain");
        CheckColumnExistance(connection, "mediastreams", "dvversionmajor");

        static void CheckColumnExistance(SqliteConnection connection, string table, string column)
        {
            using (var cmd = connection.CreateCommand())
            {
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                cmd.CommandText = $"Select COUNT(1) FROM pragma_table_xinfo('{table}') WHERE lower(name) = '{column}';";
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                var result = cmd.ExecuteScalar()!;
                if (!result.Equals(1L))
                {
                    throw new InvalidOperationException("Your database does not meet the required standard. Only upgrades from server version 10.9.11 or above are supported. Please upgrade first to server version 10.10.7 before attempting to upgrade afterwards to 10.11");
                }
            }
        }
    }
}
