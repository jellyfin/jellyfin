#pragma warning disable RS0030 // Do not use banned APIs
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

[JellyfinMigration("2025-07-30T21:50:00", nameof(ReseedFolderFlag))]
[JellyfinMigrationBackup(JellyfinDb = true)]
internal class ReseedFolderFlag : IAsyncMigrationRoutine
{
    private const string DbFilename = "library.db.old";

    private readonly IStartupLogger _logger;
    private readonly IServerApplicationPaths _paths;
    private readonly IDbContextFactory<JellyfinDbContext> _provider;

    public ReseedFolderFlag(
            IStartupLogger<MigrateLibraryDb> startupLogger,
            IDbContextFactory<JellyfinDbContext> provider,
            IServerApplicationPaths paths)
    {
        _logger = startupLogger;
        _provider = provider;
        _paths = paths;
    }

    internal static bool RerunGuardFlag { get; set; } = false;

    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        if (RerunGuardFlag)
        {
            _logger.LogInformation("Migration is skipped because it does not apply.");
            return;
        }

        _logger.LogInformation("Migrating the IsFolder flag from library.db.old may take a while, do not stop Jellyfin.");

        var dataPath = _paths.DataPath;
        var libraryDbPath = Path.Combine(dataPath, DbFilename);
        if (!File.Exists(libraryDbPath))
        {
            _logger.LogError("Cannot migrate IsFolder flag from {LibraryDb} as it does not exist. This migration expects the MigrateLibraryDb to run first.", libraryDbPath);
            return;
        }

        var dbContext = await _provider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            using var connection = new SqliteConnection($"Filename={libraryDbPath};Mode=ReadOnly");
            var queryResult = connection.Query(
"""
    SELECT key FROM TypedBaseItems

    WHERE IsFolder = true
""");
            foreach (var entity in queryResult)
            {
                var id = entity.GetGuid(0);
                await dbContext.BaseItems.Where(e => e.Id == id).ExecuteUpdateAsync(e => e.SetProperty(f => f.IsFolder, true), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
