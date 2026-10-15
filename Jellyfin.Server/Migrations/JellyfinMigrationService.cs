using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Database.Implementations;
using Jellyfin.Extensions.Json;
using Jellyfin.Server.Implementations.SystemBackupService;
using Jellyfin.Server.Migrations.Stages;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.SystemBackupService;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations;

/// <summary>
/// Handles Migration of the Jellyfin data structure.
/// </summary>
internal class JellyfinMigrationService
{
    private const string DbFilename = "library.db";
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IStartupLogger _startupLogger;
    private readonly IBackupService? _backupService;
    private readonly IJellyfinDatabaseProvider? _jellyfinDatabaseProvider;
    private readonly IApplicationPaths _applicationPaths;
    private (string? LibraryDb, string? JellyfinDb, BackupManifestDto? FullBackup) _backupKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinMigrationService"/> class.
    /// </summary>
    /// <param name="dbContextFactory">Provides access to the jellyfin database.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="startupLogger">The startup logger for Startup UI intigration.</param>
    /// <param name="applicationPaths">Application paths for library.db backup.</param>
    /// <param name="backupService">The jellyfin backup service.</param>
    /// <param name="jellyfinDatabaseProvider">The jellyfin database provider.</param>
    public JellyfinMigrationService(
        IDbContextFactory<JellyfinDbContext> dbContextFactory,
        ILoggerFactory loggerFactory,
        IStartupLogger<JellyfinMigrationService> startupLogger,
        IApplicationPaths applicationPaths,
        IBackupService? backupService = null,
        IJellyfinDatabaseProvider? jellyfinDatabaseProvider = null)
    {
        _dbContextFactory = dbContextFactory;
        _loggerFactory = loggerFactory;
        _startupLogger = startupLogger;
        _backupService = backupService;
        _jellyfinDatabaseProvider = jellyfinDatabaseProvider;
        _applicationPaths = applicationPaths;
        Migrations = [.. MigrationRoutineDiscovery.Find()
            .Select(e => (e.Type, e.Metadata, Backup: e.Type.GetCustomAttributes<JellyfinMigrationBackupAttribute>()))
            .GroupBy(e => e.Metadata.Stage)
            .Select(f =>
            {
                var stage = new MigrationStage(f.Key);
                foreach (var item in f)
                {
                    JellyfinMigrationBackupAttribute? backupMetadata = null;
                    if (item.Backup?.Any() == true)
                    {
                        backupMetadata = item.Backup.Aggregate(MergeBackupAttributes);
                    }

                    stage.Add(new(item.Type, item.Metadata, backupMetadata));
                }

                return stage;
            })];
    }

    private interface IInternalMigration
    {
        Task PerformAsync(IStartupLogger logger);
    }

    private HashSet<MigrationStage> Migrations { get; set; }

    private string MigrationInProgressMarkerPath => Path.Combine(_applicationPaths.DataPath, MigrationInProgressMarker.FileName);

    public async Task CheckFirstTimeRunOrMigration(IApplicationPaths appPaths, StartupOptions startupOptions)
    {
        var logger = _startupLogger.With(_loggerFactory.CreateLogger<JellyfinMigrationService>()).BeginGroup($"Migration Startup");
        logger.LogInformation("Initialise Migration service.");
        await EnsureNoMigrationInProgressAsync(logger, !string.IsNullOrWhiteSpace(startupOptions.RestoreArchive)).ConfigureAwait(false);
        var xmlSerializer = new MyXmlSerializer();
        var serverConfig = File.Exists(appPaths.SystemConfigurationFilePath)
            ? (ServerConfiguration)xmlSerializer.DeserializeFromFile(typeof(ServerConfiguration), appPaths.SystemConfigurationFilePath)!
            : new ServerConfiguration();
        if (!serverConfig.IsStartupWizardCompleted || startupOptions.StartupMode is Configuration.StartupMode.SeedSystem)
        {
            logger.LogInformation("System initialization detected. Seed data. Startup mode is: {StartupMode}", startupOptions.StartupMode ?? Configuration.StartupMode.MediaServer);
            var flatApplyMigrations = Migrations.SelectMany(e => e.Where(f => !f.Metadata.RunMigrationOnSetup)).ToArray();

            var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                var databaseCreator = dbContext.Database.GetService<IDatabaseCreator>() as IRelationalDatabaseCreator
                    ?? throw new InvalidOperationException("Jellyfin does only support relational databases.");
                if (!await databaseCreator.ExistsAsync().ConfigureAwait(false))
                {
                    await databaseCreator.CreateAsync().ConfigureAwait(false);
                }

                var historyRepository = dbContext.GetService<IHistoryRepository>();

                await historyRepository.CreateIfNotExistsAsync().ConfigureAwait(false);
                var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync().ConfigureAwait(false);
                var startupScripts = flatApplyMigrations
                    .Where(e => !appliedMigrations.Contains(e.BuildCodeMigrationId()))
                    .Select(e => (Migration: e.Metadata, Script: historyRepository.GetInsertScript(new HistoryRow(e.BuildCodeMigrationId(), GetJellyfinVersion()))))
                    .ToArray();
                foreach (var item in startupScripts)
                {
                    logger.LogInformation("Seed migration {Key}-{Name}.", item.Migration.Key, item.Migration.Name);
#pragma warning disable JF0002 // The script comes from the history repository of the provider in use, so it is the SQL of that engine.
                    await dbContext.Database.ExecuteSqlRawAsync(item.Script).ConfigureAwait(false);
#pragma warning restore JF0002
                }
            }

            logger.LogInformation("Migration system initialisation completed.");
        }
        else
        {
            // Only a media server start is refused. A MigrateSystem run is left to the migrations, which another database
            // provider may rely on to initialise its database, and a restore replaces the database later, in StartServer.
            var startupMode = startupOptions.StartupMode ?? Configuration.StartupMode.MediaServer;
            if (startupMode == Configuration.StartupMode.MediaServer && string.IsNullOrWhiteSpace(startupOptions.RestoreArchive))
            {
                await EnsureExistingDatabaseAsync(appPaths, logger).ConfigureAwait(false);
            }

            // migrate any existing migration.xml files
            var migrationConfigPath = Path.Join(appPaths.ConfigurationDirectoryPath, "migrations.xml");
            var migrationOptions = File.Exists(migrationConfigPath)
                 ? (MigrationOptions)xmlSerializer.DeserializeFromFile(typeof(MigrationOptions), migrationConfigPath)!
                 : null;
            if (migrationOptions is not null && migrationOptions.Applied.Count > 0)
            {
                logger.LogInformation("Old migration style migration.xml detected. Migrate now.");
                try
                {
                    var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                    await using (dbContext.ConfigureAwait(false))
                    {
                        var historyRepository = dbContext.GetService<IHistoryRepository>();
                        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync().ConfigureAwait(false);
                        var lastOldAppliedMigration = Migrations
                            .SelectMany(e => e.Where(e => e.Metadata.Key is not null)) // only consider migrations that have the key set as its the reference marker for legacy migrations.
                            .Where(e => migrationOptions.Applied.Any(f => f.Id.Equals(e.Metadata.Key!.Value)))
                            .Where(e => !appliedMigrations.Contains(e.BuildCodeMigrationId()))
                            .OrderBy(e => e.BuildCodeMigrationId())
                            .Last(); // this is the latest migration applied in the old migration.xml

                        IReadOnlyList<CodeMigration> oldMigrations = [
                            .. Migrations
                            .SelectMany(e => e)
                            .OrderBy(e => e.BuildCodeMigrationId())
                            .TakeWhile(e => e.BuildCodeMigrationId() != lastOldAppliedMigration.BuildCodeMigrationId()),
                            lastOldAppliedMigration
                        ];
                        // those are all migrations that had to run in the old migration system, even if not noted in the migration.xml file.

                        var startupScripts = oldMigrations.Select(e => (Migration: e.Metadata, Script: historyRepository.GetInsertScript(new HistoryRow(e.BuildCodeMigrationId(), GetJellyfinVersion()))));
                        foreach (var item in startupScripts)
                        {
                            logger.LogInformation("Migrate migration {Key}-{Name}.", item.Migration.Key, item.Migration.Name);
#pragma warning disable JF0002 // The script comes from the history repository of the provider in use, so it is the SQL of that engine.
                            await dbContext.Database.ExecuteSqlRawAsync(item.Script).ConfigureAwait(false);
#pragma warning restore JF0002
                        }

                        logger.LogInformation("Rename old migration.xml to migration.xml.backup");
                        File.Move(migrationConfigPath, Path.ChangeExtension(migrationConfigPath, ".xml.backup"), true);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Failed to apply migrations");
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Stops the startup of a server that has been set up before but whose database is missing or empty. Running the migrations
    /// against such a database fails part way through, and seeding it would leave a server nobody can log in to.
    /// </summary>
    private async Task EnsureExistingDatabaseAsync(IApplicationPaths appPaths, ILogger logger)
    {
        string? problem = null;
        var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var databaseCreator = dbContext.Database.GetService<IDatabaseCreator>() as IRelationalDatabaseCreator
                ?? throw new InvalidOperationException("Jellyfin does only support relational databases.");

            // Check existence first: opening a connection to a missing SQLite database creates an empty file.
            if (!await databaseCreator.ExistsAsync().ConfigureAwait(false))
            {
                problem = "the database does not exist";
            }
            else
            {
                var historyRepository = dbContext.GetService<IHistoryRepository>();
                if (!await historyRepository.ExistsAsync().ConfigureAwait(false))
                {
                    problem = "the database has no migration history";
                }
                else if ((await historyRepository.GetAppliedMigrationsAsync().ConfigureAwait(false)).Count == 0)
                {
                    problem = "the migration history of the database is empty";
                }
            }
        }

        if (problem is null)
        {
            return;
        }

        var message = string.Format(
            CultureInfo.InvariantCulture,
            "This server has been set up before (IsStartupWizardCompleted is true in {0}), but {1}. Jellyfin will not start an existing server with an empty database. "
            + "To continue, either restore the previous database; or start over and keep this server's settings by setting IsStartupWizardCompleted to false in {0} "
            + "(users, watch history and everything else stored in the database will not come back); or set up a new server with empty configuration and data directories.",
            appPaths.SystemConfigurationFilePath,
            problem);
        logger.LogCritical("{Message}", message);
        throw new InvalidOperationException(message);
    }

    /// <summary>
    /// Runs all pending migrations of the requested stage.
    /// </summary>
    /// <param name="stage">The stage to migrate.</param>
    /// <param name="serviceProvider">The service provider handed to the migrations.</param>
    /// <returns>A value indicating whether at least one migration has been applied.</returns>
    public async Task<bool> MigrateStepAsync(JellyfinMigrationStageTypes stage, IServiceProvider serviceProvider)
    {
        var logger = _startupLogger.With(_loggerFactory.CreateLogger<JellyfinMigrationService>()).BeginGroup($"Migrate stage {stage}.");
        await EnsureNoMigrationInProgressAsync(logger, restoringArchive: false).ConfigureAwait(false);
        ICollection<CodeMigration> migrationStage = (Migrations.FirstOrDefault(e => e.Stage == stage) as ICollection<CodeMigration>) ?? [];

        var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var historyRepository = dbContext.GetService<IHistoryRepository>();
            var migrationsAssembly = dbContext.GetService<IMigrationsAssembly>();
            var completedMigrations = 0;
            string? lastMigrationKey = null;

            while (true)
            {
                // A single migration can change which migrations still apply: IMigrator.MigrateAsync treats its argument as the
                // state to end up in, so it reverts everything applied after it, and a reverted migration can take code migrations
                // with it (AddNormalizedUsername.Down drops the UpdateNormalizedUsername history row). Anything computed before
                // that point is stale, so only ever run the next migration and then work out the pending set again.
                var appliedMigrations = await historyRepository.GetAppliedMigrationsAsync().ConfigureAwait(false);
                var pendingCodeMigrations = migrationStage
                    .Where(e => appliedMigrations.All(f => f.MigrationId != e.BuildCodeMigrationId()))
                    .Select(e => (Key: e.BuildCodeMigrationId(), Migration: new InternalCodeMigration(e, serviceProvider, dbContext)))
                    .ToArray();

                (string Key, InternalDatabaseMigration Migration)[] pendingDatabaseMigrations = [];
                if (stage is JellyfinMigrationStageTypes.CoreInitialisation)
                {
                    pendingDatabaseMigrations = migrationsAssembly.Migrations.Where(f => appliedMigrations.All(e => e.MigrationId != f.Key))
                       .Select(e => (Key: e.Key, Migration: new InternalDatabaseMigration(e, dbContext)))
                       .ToArray();
                }

                (string Key, IInternalMigration Migration)[] pendingMigrations = [.. pendingCodeMigrations, .. pendingDatabaseMigrations];
                if (pendingMigrations.Length == 0)
                {
                    break;
                }

                if (completedMigrations == 0)
                {
                    logger.LogInformation("There are {Pending} migrations for stage {Stage}.", pendingMigrations.Length, stage);
                }

                var item = pendingMigrations.OrderBy(e => e.Key, StringComparer.Ordinal).First();
                if (string.Equals(item.Key, lastMigrationKey, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Migration {item.Key} ran but did not record itself as applied and would repeat indefinitely.");
                }

                lastMigrationKey = item.Key;

                // Surface generic "Running migration X of Y" progress in the always-visible startup UI header.
                SetupServer.ReportActivity(StartupActivity.Migration(completedMigrations + 1, completedMigrations + pendingMigrations.Length));
                var migrationLogger = logger.With(_loggerFactory.CreateLogger(item.Migration.GetType().Name)).BeginGroup($"{item.Key}");
                var recordsProgress = item.Migration is InternalDatabaseMigration && _jellyfinDatabaseProvider is { SupportsTransactionalMigrations: false };
                if (recordsProgress)
                {
                    await WriteMigrationInProgressMarkerAsync(migrationLogger, item.Key).ConfigureAwait(false);
                }

                try
                {
                    migrationLogger.LogInformation("Perform migration {Name}", item.Key);
                    await item.Migration.PerformAsync(migrationLogger).ConfigureAwait(false);
                    migrationLogger.LogInformation("Migration {Name} was successfully applied", item.Key);
                }
                catch (Exception ex)
                {
                    migrationLogger.LogCritical("Error: {Error}", ex.Message);
                    migrationLogger.LogError(ex, "Migration {Name} failed", item.Key);

                    // A provider that cannot undo a failed migration and took no snapshot has nothing to roll back to. It is not asked to,
                    // as the instruction that follows is the one thing that can be done.
                    var providerTookNoSnapshot = string.IsNullOrEmpty(_backupKey.JellyfinDb);
                    if (_backupKey != default && _backupService is not null && _jellyfinDatabaseProvider is not null)
                    {
                        if (_backupKey.LibraryDb is not null)
                        {
                            migrationLogger.LogInformation("Attempt to rollback librarydb.");
                            try
                            {
                                var libraryDbPath = Path.Combine(_applicationPaths.DataPath, DbFilename);
                                File.Move(_backupKey.LibraryDb, libraryDbPath, true);
                            }
                            catch (Exception inner)
                            {
                                migrationLogger.LogCritical(inner, "Could not rollback {LibraryPath}. Manual intervention might be required to restore a operational state.", _backupKey.LibraryDb);
                            }
                        }

                        if (_backupKey.JellyfinDb is not null && !(recordsProgress && providerTookNoSnapshot))
                        {
                            migrationLogger.LogInformation("Attempt to rollback JellyfinDb.");
                            try
                            {
                                await _jellyfinDatabaseProvider.RestoreBackupFast(_backupKey.JellyfinDb, CancellationToken.None).ConfigureAwait(false);
                            }
                            catch (Exception inner)
                            {
                                migrationLogger.LogCritical(inner, "Could not rollback {LibraryPath}. Manual intervention might be required to restore a operational state.", _backupKey.JellyfinDb);
                            }
                        }

                        if (_backupKey.FullBackup is not null)
                        {
                            migrationLogger.LogInformation("Attempt to rollback from backup.");
                            try
                            {
                                await _backupService.RestoreBackupAsync(_backupKey.FullBackup.Path).ConfigureAwait(false);
                            }
                            catch (Exception inner)
                            {
                                migrationLogger.LogCritical(inner, "Could not rollback from backup {Backup}. Manual intervention might be required to restore a operational state.", _backupKey.FullBackup.Path);
                            }
                        }
                    }

                    if (recordsProgress)
                    {
                        migrationLogger.LogCritical(
                            "Schema migration {Name} did not finish and left {Path} in place, so the database may be partly migrated. {Instruction}",
                            item.Key,
                            MigrationInProgressMarkerPath,
                            providerTookNoSnapshot
                                ? "This database engine takes no snapshot before a migration, so Jellyfin did not try to roll the database back. "
                                    + "To continue, restore the database from the backup your database engine took before the upgrade (for example a dump made with mysqldump), "
                                    + "then delete that file and start Jellyfin again. Restoring a Jellyfin backup archive does not repair the database."
                                : "If Jellyfin attempted to roll the database back from the snapshot taken before the migration, the log above says whether that succeeded. "
                                    + "If the database is not in the state before the upgrade, restore the database from the backup your database engine took before the upgrade, "
                                    + "then delete that file and start Jellyfin again. Restoring a Jellyfin backup archive does not repair the database.");
                    }

                    throw;
                }

                if (recordsProgress && await IsMigrationRecordedAsync(historyRepository, item.Key).ConfigureAwait(false))
                {
                    DeleteMigrationInProgressMarker(migrationLogger, item.Key);
                }

                completedMigrations++;
            }

            return completedMigrations > 0;
        }
    }

    private static MigrationInProgressMarker? ReadMigrationInProgressMarker(string path, out Exception? failure)
    {
        failure = null;
        try
        {
            var marker = JsonSerializer.Deserialize<MigrationInProgressMarker>(File.ReadAllText(path), JsonDefaults.Options);
            if (marker is null || string.IsNullOrEmpty(marker.MigrationId) || string.IsNullOrEmpty(marker.ProviderKey))
            {
                return null;
            }

            return marker;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            failure = ex;
            return null;
        }
    }

    private static async Task<bool> IsMigrationRecordedAsync(IHistoryRepository historyRepository, string migrationId)
    {
        var appliedMigrations = await historyRepository.GetAppliedMigrationsAsync().ConfigureAwait(false);
        return appliedMigrations.Any(e => string.Equals(e.MigrationId, migrationId, StringComparison.Ordinal));
    }

    private static string GetProviderKey(IJellyfinDatabaseProvider provider)
    {
        var providerType = provider.GetType();
        return providerType.GetCustomAttribute<JellyfinDatabaseProviderKeyAttribute>()?.DatabaseProviderKey ?? providerType.FullName ?? providerType.Name;
    }

    private static string DescribeUndeletableMarker(string migrationId, string markerPath)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "Schema migration {0} was applied, but {1} could not be deleted. Delete that file, then start Jellyfin again.",
            migrationId,
            markerPath);
    }

    private async Task<bool> IsMigrationAppliedAsync(ILogger logger, string migrationId)
    {
        try
        {
            var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                if (dbContext.Database.GetService<IDatabaseCreator>() is not IRelationalDatabaseCreator databaseCreator
                    || !await databaseCreator.ExistsAsync().ConfigureAwait(false))
                {
                    return false;
                }

                var historyRepository = dbContext.GetService<IHistoryRepository>();
                return await historyRepository.ExistsAsync().ConfigureAwait(false)
                    && await IsMigrationRecordedAsync(historyRepository, migrationId).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not check whether schema migration {Name} was applied.", migrationId);
            return false;
        }
    }

    private async Task EnsureNoMigrationInProgressAsync(ILogger logger, bool restoringArchive)
    {
        var markerPath = MigrationInProgressMarkerPath;
        if (!File.Exists(markerPath))
        {
            return;
        }

        var marker = ReadMigrationInProgressMarker(markerPath, out var failure);
        var currentProviderKey = _jellyfinDatabaseProvider is null ? null : GetProviderKey(_jellyfinDatabaseProvider);
        if (marker is not null
            && currentProviderKey is not null
            && string.Equals(marker.ProviderKey, currentProviderKey, StringComparison.Ordinal)
            && await IsMigrationAppliedAsync(logger, marker.MigrationId).ConfigureAwait(false))
        {
            logger.LogWarning(
                "Schema migration {Name} is recorded as applied, so the leftover {Path} is deleted and the migration is not repeated.",
                marker.MigrationId,
                markerPath);
            DeleteMigrationInProgressMarker(logger, marker.MigrationId);
            return;
        }

        string problem;
        if (marker is null)
        {
            problem = string.Format(
                CultureInfo.InvariantCulture,
                "{0} exists but cannot be read, so it is not known which schema migration was started and did not finish. The database may be partly migrated.",
                markerPath);
        }
        else if (currentProviderKey is not null && !string.Equals(marker.ProviderKey, currentProviderKey, StringComparison.Ordinal))
        {
            problem = string.Format(
                CultureInfo.InvariantCulture,
                "Schema migration {0} was started with the database provider {1} at {2:u} and did not finish, but the configured database provider is {3}. "
                + "It is not known which database is partly migrated.",
                marker.MigrationId,
                marker.ProviderKey,
                marker.StartedAtUtc,
                currentProviderKey);
        }
        else
        {
            problem = string.Format(
                CultureInfo.InvariantCulture,
                "Schema migration {0} was started with the database provider {1} at {2:u} and did not finish. The database may be partly migrated.",
                marker.MigrationId,
                marker.ProviderKey,
                marker.StartedAtUtc);
        }

        // A Jellyfin backup archive cannot repair a schema, so only the backup of the database engine leads out of this.
        var message = string.Format(
            CultureInfo.InvariantCulture,
            "{0} Jellyfin will not migrate. To continue, restore the database from the backup your database engine took before the upgrade "
            + "(for example a dump made with mysqldump), then delete {1}. Restoring a Jellyfin backup archive does not repair the database.",
            problem,
            markerPath);
        if (restoringArchive)
        {
            message += " The archive given with --restore-archive was not restored. "
                + "After the database is restored and the file is deleted, run Jellyfin once with --mode MigrateSystem so that it migrates the database, "
                + "then restore the archive again.";
        }

        logger.LogCritical(failure, "{Message}", message);
        throw new InvalidOperationException(message);
    }

    private void DeleteMigrationInProgressMarker(ILogger logger, string migrationId)
    {
        var markerPath = MigrationInProgressMarkerPath;
        try
        {
            File.Delete(markerPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var message = DescribeUndeletableMarker(migrationId, markerPath);
            logger.LogCritical(ex, "{Message}", message);
            throw new InvalidOperationException(message, ex);
        }
    }

    private async Task WriteMigrationInProgressMarkerAsync(ILogger logger, string migrationId)
    {
        var markerPath = MigrationInProgressMarkerPath;
        var marker = new MigrationInProgressMarker(migrationId, GetProviderKey(_jellyfinDatabaseProvider!), DateTime.UtcNow);
        try
        {
            var stream = new FileStream(markerPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough);
            await using (stream.ConfigureAwait(false))
            {
                await JsonSerializer.SerializeAsync(stream, marker, JsonDefaults.Options).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogCritical(ex, "Could not write {Path} to record schema migration {Name}, so the migration was not started.", markerPath, migrationId);
            try
            {
                File.Delete(markerPath);
            }
            catch (Exception inner) when (inner is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(inner, "Could not delete the incomplete {Path}.", markerPath);
            }

            throw;
        }
    }

    private static string GetJellyfinVersion()
    {
        return Assembly.GetEntryAssembly()!.GetName().Version!.ToString();
    }

    public async Task CleanupSystemAfterMigration(ILogger logger)
    {
        if (_backupKey != default)
        {
            if (_backupKey.LibraryDb is not null)
            {
                logger.LogInformation("Attempt to cleanup librarydb backup.");
                try
                {
                    File.Delete(_backupKey.LibraryDb);
                }
                catch (Exception inner)
                {
                    logger.LogCritical(inner, "Could not cleanup {LibraryPath}.", _backupKey.LibraryDb);
                }
            }

            if (_backupKey.JellyfinDb is not null && _jellyfinDatabaseProvider is not null)
            {
                logger.LogInformation("Attempt to cleanup JellyfinDb backup.");
                try
                {
                    await _jellyfinDatabaseProvider.DeleteBackup(_backupKey.JellyfinDb).ConfigureAwait(false);
                }
                catch (Exception inner)
                {
                    logger.LogCritical(inner, "Could not cleanup {LibraryPath}.", _backupKey.JellyfinDb);
                }
            }

            if (_backupKey.FullBackup is not null)
            {
                logger.LogInformation("Attempt to cleanup from migration backup.");
                try
                {
                    File.Delete(_backupKey.FullBackup.Path);
                }
                catch (Exception inner)
                {
                    logger.LogCritical(inner, "Could not cleanup backup {Backup}.", _backupKey.FullBackup.Path);
                }
            }
        }
    }

    public async Task PrepareSystemForMigration(ILogger logger)
    {
        logger.LogInformation("Prepare system for possible migrations");
        JellyfinMigrationBackupAttribute backupInstruction;
        IReadOnlyList<HistoryRow> appliedMigrations;
        var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var historyRepository = dbContext.GetService<IHistoryRepository>();
            var migrationsAssembly = dbContext.GetService<IMigrationsAssembly>();
            appliedMigrations = await historyRepository.GetAppliedMigrationsAsync().ConfigureAwait(false);
            backupInstruction = new JellyfinMigrationBackupAttribute()
            {
                JellyfinDb = migrationsAssembly.Migrations.Any(f => appliedMigrations.All(e => e.MigrationId != f.Key))
            };
        }

        backupInstruction = Migrations.SelectMany(e => e)
           .Where(e => appliedMigrations.All(f => f.MigrationId != e.BuildCodeMigrationId()))
           .Select(e => e.BackupRequirements)
           .Where(e => e is not null)
           .Aggregate(backupInstruction, MergeBackupAttributes!);

        if (backupInstruction.LegacyLibraryDb)
        {
            logger.LogInformation("A migration will attempt to modify the library.db, will attempt to backup the file now.");
            // for legacy migrations that still operates on the library.db
            var libraryDbPath = Path.Combine(_applicationPaths.DataPath, DbFilename);
            if (File.Exists(libraryDbPath))
            {
                for (int i = 1; ; i++)
                {
                    var bakPath = string.Format(CultureInfo.InvariantCulture, "{0}.bak{1}", libraryDbPath, i);
                    if (!File.Exists(bakPath))
                    {
                        try
                        {
                            logger.LogInformation("Backing up {Library} to {BackupPath}", DbFilename, bakPath);
                            File.Copy(libraryDbPath, bakPath);
                            _backupKey = (bakPath, _backupKey.JellyfinDb, _backupKey.FullBackup);
                            logger.LogInformation("{Library} backed up to {BackupPath}", DbFilename, bakPath);
                            break;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Cannot make a backup of {Library} at path {BackupPath}", DbFilename, bakPath);
                            throw;
                        }
                    }
                }

                logger.LogInformation("{Library} has been backed up as {BackupPath}", DbFilename, _backupKey.LibraryDb);
            }
            else
            {
                logger.LogError("Cannot make a backup of {Library} at path {BackupPath} because file could not be found at {LibraryPath}", DbFilename, libraryDbPath, _applicationPaths.DataPath);
            }
        }

        if (backupInstruction.JellyfinDb && _jellyfinDatabaseProvider is not null)
        {
            logger.LogInformation("A migration will attempt to modify the Jellyfin database, will ask its provider for a backup now.");
            _backupKey = (_backupKey.LibraryDb, await _jellyfinDatabaseProvider.MigrationBackupFast(CancellationToken.None).ConfigureAwait(false), _backupKey.FullBackup);
            if (string.IsNullOrEmpty(_backupKey.JellyfinDb))
            {
                logger.LogInformation("No backup of the Jellyfin database was taken, as its provider has nothing to snapshot");
            }
            else
            {
                logger.LogInformation("Jellyfin database has been backed up as {BackupPath}", _backupKey.JellyfinDb);
            }
        }

        if (_backupService is not null && (backupInstruction.Metadata || backupInstruction.Subtitles || backupInstruction.Trickplay))
        {
            logger.LogInformation("A migration will attempt to modify system resources. Will attempt to create backup now.");
            _backupKey = (_backupKey.LibraryDb, _backupKey.JellyfinDb, await _backupService.CreateBackupAsync(new BackupOptionsDto()
            {
                Metadata = backupInstruction.Metadata,
                Subtitles = backupInstruction.Subtitles,
                Trickplay = backupInstruction.Trickplay,
                Database = false // database backups are explicitly handled by the provider itself as the backup service requires parity with the current model
            }).ConfigureAwait(false));
            logger.LogInformation("Pre-Migration backup successfully created as {BackupKey}", _backupKey.FullBackup.Path);
        }
    }

    private static JellyfinMigrationBackupAttribute MergeBackupAttributes(JellyfinMigrationBackupAttribute left, JellyfinMigrationBackupAttribute right)
    {
        return new JellyfinMigrationBackupAttribute()
        {
            JellyfinDb = left!.JellyfinDb || right!.JellyfinDb,
            LegacyLibraryDb = left.LegacyLibraryDb || right!.LegacyLibraryDb,
            Metadata = left.Metadata || right!.Metadata,
            Subtitles = left.Subtitles || right!.Subtitles,
            Trickplay = left.Trickplay || right!.Trickplay
        };
    }

    private class InternalCodeMigration : IInternalMigration
    {
        private readonly CodeMigration _codeMigration;
        private readonly IServiceProvider _serviceProvider;
        private JellyfinDbContext _dbContext;

        public InternalCodeMigration(CodeMigration codeMigration, IServiceProvider serviceProvider, JellyfinDbContext dbContext)
        {
            _codeMigration = codeMigration;
            _serviceProvider = serviceProvider;
            _dbContext = dbContext;
        }

        public async Task PerformAsync(IStartupLogger logger)
        {
            await _codeMigration.Perform(_serviceProvider, logger, CancellationToken.None).ConfigureAwait(false);

            var historyRepository = _dbContext.GetService<IHistoryRepository>();
            var createScript = historyRepository.GetInsertScript(new HistoryRow(_codeMigration.BuildCodeMigrationId(), GetJellyfinVersion()));
#pragma warning disable JF0002 // The script comes from the history repository of the provider in use, so it is the SQL of that engine.
            await _dbContext.Database.ExecuteSqlRawAsync(createScript).ConfigureAwait(false);
#pragma warning restore JF0002
        }
    }

    private class InternalDatabaseMigration : IInternalMigration
    {
        private readonly JellyfinDbContext _jellyfinDbContext;
        private KeyValuePair<string, TypeInfo> _databaseMigrationInfo;

        public InternalDatabaseMigration(KeyValuePair<string, TypeInfo> databaseMigrationInfo, JellyfinDbContext jellyfinDbContext)
        {
            _databaseMigrationInfo = databaseMigrationInfo;
            _jellyfinDbContext = jellyfinDbContext;
        }

        public async Task PerformAsync(IStartupLogger logger)
        {
            var migrator = _jellyfinDbContext.GetService<IMigrator>();
            await migrator.MigrateAsync(_databaseMigrationInfo.Key).ConfigureAwait(false);
        }
    }
}
