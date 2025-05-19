using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Implementations.SystemBackupService;
using Jellyfin.Server.Migrations.Stages;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.SystemBackupService;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IBackupService? _backupService;
    private readonly IApplicationPaths _applicationPaths;
    private (string? LibraryDb, BackupManifestDto? FullBackup) _backupKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinMigrationService"/> class.
    /// </summary>
    /// <param name="dbContextFactory">Provides access to the jellyfin database.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="applicationPaths">Application paths for library.db backup.</param>
    /// <param name="backupService">The jellyfin backup service.</param>
    public JellyfinMigrationService(
        IDbContextFactory<JellyfinDbContext> dbContextFactory,
        ILoggerFactory loggerFactory,
        IApplicationPaths applicationPaths,
        IBackupService? backupService = null)
    {
        _dbContextFactory = dbContextFactory;
        _loggerFactory = loggerFactory;
        _backupService = backupService;
        _applicationPaths = applicationPaths;
#pragma warning disable CS0618 // Type or member is obsolete
        Migrations = [.. typeof(IMigrationRoutine).Assembly.GetTypes().Where(e => typeof(IMigrationRoutine).IsAssignableFrom(e) || typeof(IAsyncMigrationRoutine).IsAssignableFrom(e))
            .Select(e => (Type: e, Metadata: e.GetCustomAttribute<JellyfinMigrationAttribute>(), Backup: e.GetCustomAttribute<JellyfinMigrationBackupAttribute>()))
            .Where(e => e.Metadata != null)
            .GroupBy(e => e.Metadata!.Stage)
            .Select(f =>
            {
                var stage = new MigrationStage(f.Key);
                foreach (var item in f)
                {
                    stage.Add(new(item.Type, item.Metadata!, item.Backup));
                }

                return stage;
            })];
#pragma warning restore CS0618 // Type or member is obsolete
    }

    private interface IInternalMigration
    {
        Task PerformAsync(ILogger logger);
    }

    private HashSet<MigrationStage> Migrations { get; set; }

    public async Task CheckFirstTimeRunOrMigration(IApplicationPaths appPaths)
    {
        var logger = _loggerFactory.CreateLogger<JellyfinMigrationService>();
        logger.LogInformation("Initialise Migration service.");
        var xmlSerializer = new MyXmlSerializer();
        var serverConfig = File.Exists(appPaths.SystemConfigurationFilePath)
            ? (ServerConfiguration)xmlSerializer.DeserializeFromFile(typeof(ServerConfiguration), appPaths.SystemConfigurationFilePath)!
            : new ServerConfiguration();
        if (!serverConfig.IsStartupWizardCompleted)
        {
            logger.LogInformation("System initialisation detected. Seed data.");
            var flatApplyMigrations = Migrations.SelectMany(e => e.Where(f => !f.Metadata.RunMigrationOnSetup)).ToArray();

            var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                var historyRepository = dbContext.GetService<IHistoryRepository>();

                await historyRepository.CreateIfNotExistsAsync().ConfigureAwait(false);
                var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync().ConfigureAwait(false);
                var startupScripts = flatApplyMigrations
                    .Where(e => !appliedMigrations.Any(f => f != e.BuildCodeMigrationId()))
                    .Select(e => (Migration: e.Metadata, Script: historyRepository.GetInsertScript(new HistoryRow(e.BuildCodeMigrationId(), GetJellyfinVersion()))))
                    .ToArray();
                foreach (var item in startupScripts)
                {
                    logger.LogInformation("Seed migration {Key}-{Name}.", item.Migration.Key, item.Migration.Name);
                    await dbContext.Database.ExecuteSqlRawAsync(item.Script).ConfigureAwait(false);
                }
            }

            logger.LogInformation("Migration system initialisation completed.");
        }
        else
        {
            // migrate any existing migration.xml files
            var migrationConfigPath = Path.Join(appPaths.ConfigurationDirectoryPath, "migrations.xml");
            var migrationOptions = File.Exists(migrationConfigPath)
                 ? (MigrationOptions)xmlSerializer.DeserializeFromFile(typeof(MigrationOptions), migrationConfigPath)!
                 : null;
            if (migrationOptions != null && migrationOptions.Applied.Count > 0)
            {
                logger.LogInformation("Old migration style migration.xml detected. Migrate now.");
                try
                {
                    var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
                    await using (dbContext.ConfigureAwait(false))
                    {
                        var historyRepository = dbContext.GetService<IHistoryRepository>();
                        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync().ConfigureAwait(false);
                        var oldMigrations = Migrations
                            .SelectMany(e => e.Where(e => e.Metadata.Key is not null)) // only consider migrations that have the key set as its the reference marker for legacy migrations.
                            .Where(e => migrationOptions.Applied.Any(f => f.Id.Equals(e.Metadata.Key!.Value)))
                            .Where(e => !appliedMigrations.Contains(e.BuildCodeMigrationId()))
                            .ToArray();
                        var startupScripts = oldMigrations.Select(e => (Migration: e.Metadata, Script: historyRepository.GetInsertScript(new HistoryRow(e.BuildCodeMigrationId(), GetJellyfinVersion()))));
                        foreach (var item in startupScripts)
                        {
                            logger.LogInformation("Migrate migration {Key}-{Name}.", item.Migration.Key, item.Migration.Name);
                            await dbContext.Database.ExecuteSqlRawAsync(item.Script).ConfigureAwait(false);
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

    public async Task MigrateStepAsync(JellyfinMigrationStageTypes stage, IServiceProvider? serviceProvider)
    {
        var logger = _loggerFactory.CreateLogger<JellyfinMigrationService>();
        logger.LogInformation("Migrate stage {Stage}.", stage);
        ICollection<CodeMigration> migrationStage = (Migrations.FirstOrDefault(e => e.Stage == stage) as ICollection<CodeMigration>) ?? [];

        var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var historyRepository = dbContext.GetService<IHistoryRepository>();
            var migrationsAssembly = dbContext.GetService<IMigrationsAssembly>();
            var appliedMigrations = await historyRepository.GetAppliedMigrationsAsync().ConfigureAwait(false);
            var pendingCodeMigrations = migrationStage
                .Where(e => appliedMigrations.All(f => f.MigrationId != e.BuildCodeMigrationId()))
                .Select(e => (Key: e.BuildCodeMigrationId(), Migration: new InternalCodeMigration(e, serviceProvider, dbContext)))
                .ToArray();

            (string Key, InternalDatabaseMigration Migration)[] pendingDatabaseMigrations = [];
            if (stage is JellyfinMigrationStageTypes.CoreInitialisaition)
            {
                pendingDatabaseMigrations = migrationsAssembly.Migrations.Where(f => appliedMigrations.All(e => e.MigrationId != f.Key))
                   .Select(e => (Key: e.Key, Migration: new InternalDatabaseMigration(e, dbContext)))
                   .ToArray();
            }

            (string Key, IInternalMigration Migration)[] pendingMigrations = [.. pendingCodeMigrations, .. pendingDatabaseMigrations];
            logger.LogInformation("There are {Pending} migrations for stage {Stage}.", pendingCodeMigrations.Length, stage);
            var migrations = pendingMigrations.OrderBy(e => e.Key).ToArray();

            foreach (var item in migrations)
            {
                try
                {
                    logger.LogInformation("Perform migration {Name}", item.Key);
                    await item.Migration.PerformAsync(_loggerFactory.CreateLogger(item.GetType().Name)).ConfigureAwait(false);
                    logger.LogInformation("Migration {Name} was successfully applied", item.Key);
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Migration {Name} failed, migration service will attempt to roll back.", item.Key);

                    if (_backupKey != default && _backupService is not null)
                    {
                        if (_backupKey.LibraryDb is not null)
                        {
                            logger.LogInformation("Attempt to rollback librarydb.");
                            try
                            {
                                var libraryDbPath = Path.Combine(_applicationPaths.DataPath, DbFilename);
                                File.Move(_backupKey.LibraryDb, libraryDbPath, true);
                            }
                            catch (Exception inner)
                            {
                                logger.LogCritical(inner, "Could not rollback {LibraryPath}. Manual intervention might be required to restore a operational state.", _backupKey.LibraryDb);
                            }
                        }
                        else if (_backupKey.FullBackup is not null)
                        {
                            logger.LogInformation("Attempt to rollback from backup.");
                            try
                            {
                                await _backupService.RestoreBackupAsync(_backupKey.FullBackup.Path).ConfigureAwait(false);
                            }
                            catch (Exception inner)
                            {
                                logger.LogCritical(inner, "Could not rollback from backup {Backup}. Manual intervention might be required to restore a operational state.", _backupKey.FullBackup.Path);
                            }
                        }
                    }

                    throw;
                }
            }
        }
    }

    private static string GetJellyfinVersion()
    {
        return Assembly.GetEntryAssembly()!.GetName().Version!.ToString();
    }

    public void CleanupSystemAfterMigration(ILogger logger)
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
            else if (_backupKey.FullBackup is not null)
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
           .Aggregate(backupInstruction, (left, right) => new JellyfinMigrationBackupAttribute()
           {
               JellyfinDb = left!.JellyfinDb || right!.JellyfinDb,
               LegacyLibraryDb = left.LegacyLibraryDb || right!.LegacyLibraryDb,
               Metadata = left.Metadata || right!.Metadata,
               Subtitles = left.Subtitles || right!.Subtitles,
               Trickplay = left.Trickplay || right!.Trickplay
           });

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
                            _backupKey = (bakPath, null);
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

        if (_backupService is not null && (backupInstruction.JellyfinDb || backupInstruction.Metadata || backupInstruction.Subtitles || backupInstruction.Trickplay))
        {
            logger.LogInformation("A migration will attempt to modify system resources. Will attempt to create backup now.");
            _backupKey = (_backupKey.LibraryDb, await _backupService.CreateBackupAsync(new BackupOptionsDto()
            {
                Metadata = backupInstruction.Metadata,
                Subtitles = backupInstruction.Subtitles,
                Trickplay = backupInstruction.Trickplay
            }).ConfigureAwait(false));
            logger.LogInformation("Pre-Migration backup successfully created as {BackupKey}", _backupKey.FullBackup.Path);
        }
    }

    private class InternalCodeMigration : IInternalMigration
    {
        private readonly CodeMigration _codeMigration;
        private readonly IServiceProvider? _serviceProvider;
        private JellyfinDbContext _dbContext;

        public InternalCodeMigration(CodeMigration codeMigration, IServiceProvider? serviceProvider, JellyfinDbContext dbContext)
        {
            _codeMigration = codeMigration;
            _serviceProvider = serviceProvider;
            _dbContext = dbContext;
        }

        public async Task PerformAsync(ILogger logger)
        {
            await _codeMigration.Perform(_serviceProvider, CancellationToken.None).ConfigureAwait(false);

            var historyRepository = _dbContext.GetService<IHistoryRepository>();
            var createScript = historyRepository.GetInsertScript(new HistoryRow(_codeMigration.BuildCodeMigrationId(), GetJellyfinVersion()));
            await _dbContext.Database.ExecuteSqlRawAsync(createScript).ConfigureAwait(false);
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

        public async Task PerformAsync(ILogger logger)
        {
            var migrator = _jellyfinDbContext.GetService<IMigrator>();
            await migrator.MigrateAsync(_databaseMigrationInfo.Key).ConfigureAwait(false);
        }
    }
}
