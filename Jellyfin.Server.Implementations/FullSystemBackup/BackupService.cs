using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Implementations.CustomNetflix;
using Jellyfin.Server.Implementations.StorageHelpers;
using Jellyfin.Server.Implementations.SystemBackupService;
using MediaBrowser.Controller;
using MediaBrowser.Controller.SystemBackupService;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Jellyfin.Server.Implementations.FullSystemBackup;

/// <summary>
/// Contains methods for creating and restoring backups.
/// </summary>
public class BackupService : IBackupService
{
    private const string ManifestEntryName = "manifest.json";
    private const string CustomNetflixDatabaseEntryName = "Database/customnetflix.pgdump";
    private const int MaxCustomNetflixTocCharacters = 16 * 1024 * 1024;
    private const int MaxCustomNetflixTocEntries = 100_000;
    private const long MinimumRestoreSafetyMarginBytes = 64L * 1024 * 1024;
    private const int RestoreSafetyMarginDivisor = 10;
    private readonly ILogger<BackupService> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IServerApplicationHost _applicationHost;
    private readonly IServerApplicationPaths _applicationPaths;
    private readonly IJellyfinDatabaseProvider _jellyfinDatabaseProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IConfiguration _configuration;
    private readonly ICustomNetflixWatchProgressBuffer? _watchProgressBuffer;
    private readonly ICustomNetflixWatchEventBuffer? _watchEventBuffer;
    private static readonly SemaphoreSlim _backupCreationLock = new(1, 1);
    private static readonly string[] _allowedCustomNetflixTocObjectTypes =
    [
        "CHECK CONSTRAINT",
        "FK CONSTRAINT",
        "TABLE DATA",
        "COMMENT",
        "CONSTRAINT",
        "INDEX",
        "TABLE"
    ];

    private static readonly JsonSerializerOptions _serializerSettings = new JsonSerializerOptions(JsonSerializerDefaults.General)
    {
        AllowTrailingCommas = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };

    private readonly Version _backupEngineVersion = new Version(0, 2, 0);

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupService"/> class.
    /// </summary>
    /// <param name="logger">A logger.</param>
    /// <param name="dbProvider">A Database Factory.</param>
    /// <param name="applicationHost">The Application host.</param>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="jellyfinDatabaseProvider">The Jellyfin database Provider in use.</param>
    /// <param name="applicationLifetime">The SystemManager.</param>
    /// <param name="configuration">The application configuration.</param>
    public BackupService(
        ILogger<BackupService> logger,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IServerApplicationHost applicationHost,
        IServerApplicationPaths applicationPaths,
        IJellyfinDatabaseProvider jellyfinDatabaseProvider,
        IHostApplicationLifetime applicationLifetime,
        IConfiguration configuration)
        : this(
            logger,
            dbProvider,
            applicationHost,
            applicationPaths,
            jellyfinDatabaseProvider,
            applicationLifetime,
            configuration,
            null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupService"/> class.
    /// </summary>
    /// <param name="logger">A logger.</param>
    /// <param name="dbProvider">A Database Factory.</param>
    /// <param name="applicationHost">The Application host.</param>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="jellyfinDatabaseProvider">The Jellyfin database Provider in use.</param>
    /// <param name="applicationLifetime">The SystemManager.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="serviceProvider">The optional service provider used to flush live CustomNetflix buffers.</param>
    public BackupService(
        ILogger<BackupService> logger,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IServerApplicationHost applicationHost,
        IServerApplicationPaths applicationPaths,
        IJellyfinDatabaseProvider jellyfinDatabaseProvider,
        IHostApplicationLifetime applicationLifetime,
        IConfiguration configuration,
        IServiceProvider? serviceProvider = null)
    {
        _logger = logger;
        _dbProvider = dbProvider;
        _applicationHost = applicationHost;
        _applicationPaths = applicationPaths;
        _jellyfinDatabaseProvider = jellyfinDatabaseProvider;
        _hostApplicationLifetime = applicationLifetime;
        _configuration = configuration;
        _watchProgressBuffer = serviceProvider?.GetService(typeof(ICustomNetflixWatchProgressBuffer))
            as ICustomNetflixWatchProgressBuffer;
        _watchEventBuffer = serviceProvider?.GetService(typeof(ICustomNetflixWatchEventBuffer))
            as ICustomNetflixWatchEventBuffer;
    }

    /// <inheritdoc/>
    public void ScheduleRestoreAndRestartServer(string archivePath)
    {
        _applicationHost.RestoreBackupPath = archivePath;
        _applicationHost.ShouldRestart = true;
        _applicationHost.NotifyPendingRestart();
        _ = Task.Run(async () =>
        {
            await Task.Delay(500).ConfigureAwait(false);
            _hostApplicationLifetime.StopApplication();
        });
    }

    /// <inheritdoc/>
    public async Task RestoreBackupAsync(string archivePath)
    {
        _logger.LogWarning("Begin restoring system to {BackupArchive}", archivePath); // Info isn't cutting it
        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException($"Requested backup file '{archivePath}' does not exist.");
        }

        Directory.CreateDirectory(_applicationPaths.TempDirectory);
        EnsureRestoreArchiveFitsTempStorage(archivePath);
        var stagingPath = Path.Combine(_applicationPaths.TempDirectory, $"restore-{Guid.NewGuid():N}");
        var fileRollback = new List<(string TargetPath, string? RollbackPath)>();
        var journaledPaths = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        var preserveStagingForManualRecovery = false;
        try
        {
            await ZipFile.ExtractToDirectoryAsync(archivePath, stagingPath, CancellationToken.None).ConfigureAwait(false);
            var (manifest, historyEntries) = await ValidateStagedBackupAsync(stagingPath, archivePath).ConfigureAwait(false);
            EnsureRestoreRollbackFitsTempStorage(stagingPath, manifest.Options.CustomNetflixDatabase);

            StorageHelper.TestCommonPathsForStorageCapacity(_applicationPaths, _logger);
            CopyStagedDirectory(stagingPath, "Config", _applicationPaths.ConfigurationDirectoryPath, fileRollback, journaledPaths);
            CopyStagedDirectory(stagingPath, "Data", _applicationPaths.DataPath, fileRollback, journaledPaths, ["metadata", "metadata-default"]);
            CopyStagedDirectory(stagingPath, "Root", _applicationPaths.RootFolderPath, fileRollback, journaledPaths);
            CopyStagedDirectory(stagingPath, "Data/metadata", _applicationPaths.InternalMetadataPath, fileRollback, journaledPaths);
            CopyStagedDirectory(stagingPath, "Data/metadata-default", _applicationPaths.DefaultInternalMetadataPath, fileRollback, journaledPaths);
            if (manifest.Options.Database)
            {
                await RestoreDatabasesWithRollbackAsync(
                    stagingPath,
                    historyEntries!,
                    manifest.Options.CustomNetflixDatabase,
                    manifest.CustomNetflixDatabaseSchema ?? "public").ConfigureAwait(false);
            }

            _logger.LogInformation("Restored Jellyfin system from {Date}", manifest.DateCreated);
        }
        catch (Exception restoreException)
        {
            try
            {
                RollbackFiles(fileRollback);
            }
            catch (Exception rollbackException)
            {
                preserveStagingForManualRecovery = true;
                _logger.LogCritical(
                    rollbackException,
                    "File rollback failed. Preserving restore staging directory {StagingPath}, including its .rollback files, for manual recovery.",
                    stagingPath);
                throw new AggregateException("System restore and file rollback both failed.", restoreException, rollbackException);
            }

            throw;
        }
        finally
        {
            if (preserveStagingForManualRecovery)
            {
                _logger.LogCritical(
                    "Restore staging directory {StagingPath} was intentionally preserved for manual recovery.",
                    stagingPath);
            }
            else
            {
                try
                {
                    if (Directory.Exists(stagingPath))
                    {
                        Directory.Delete(stagingPath, true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to remove restore staging directory {Path}", stagingPath);
                }
            }
        }
    }

    private async Task<(BackupManifest Manifest, HistoryRow[]? HistoryEntries)> ValidateStagedBackupAsync(string stagingPath, string archivePath)
    {
        var manifestPath = Path.Combine(stagingPath, ManifestEntryName);
        if (!File.Exists(manifestPath))
        {
            throw new NotSupportedException($"The loaded archive '{archivePath}' does not appear to be a Jellyfin backup as its missing the '{ManifestEntryName}'.");
        }

        BackupManifest? manifest;
        var manifestStream = File.OpenRead(manifestPath);
        await using (manifestStream.ConfigureAwait(false))
        {
            manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(manifestStream, _serializerSettings).ConfigureAwait(false);
        }

        if (manifest is null
            || manifest.ServerVersion is null
            || manifest.BackupEngineVersion is null
            || manifest.Options is null
            || (manifest.Options.Database && manifest.DatabaseTables is null))
        {
            throw new InvalidOperationException($"The loaded archive '{archivePath}' has an invalid manifest.");
        }

        if (manifest.Options.CustomNetflixDatabase && !manifest.Options.Database)
        {
            throw new InvalidOperationException("The backup manifest cannot declare a CustomNetflix PostgreSQL database without a Jellyfin database.");
        }

        if (manifest.Options.CustomNetflixDatabase
            && !File.Exists(GetStagedCustomNetflixDumpPath(stagingPath)))
        {
            throw new InvalidOperationException("The backup manifest declares a CustomNetflix PostgreSQL database, but its dump is missing.");
        }

        if (manifest.ServerVersion > _applicationHost.ApplicationVersion)
        {
            throw new NotSupportedException($"The loaded archive '{archivePath}' is made for a newer version of Jellyfin ({manifest.ServerVersion}) and cannot be loaded in this version.");
        }

        if (!TestBackupVersionCompatibility(manifest.BackupEngineVersion))
        {
            throw new NotSupportedException($"The loaded archive '{archivePath}' uses unsupported backup engine version {manifest.BackupEngineVersion}.");
        }

        if (manifest.Options.CustomNetflixDatabase)
        {
            var customNetflixSchema = manifest.CustomNetflixDatabaseSchema ?? "public";
            ArgumentException.ThrowIfNullOrWhiteSpace(customNetflixSchema);
            await ValidateCustomNetflixDumpAsync(
                GetStagedCustomNetflixDumpPath(stagingPath),
                customNetflixSchema,
                CancellationToken.None).ConfigureAwait(false);
            var customNetflixConnectionString = GetCustomNetflixConnectionString()
                ?? throw new InvalidOperationException("Cannot restore CustomNetflix PostgreSQL because it is not configured.");
            var (targetSchema, _) = await InspectCustomNetflixSchemaAsync(
                customNetflixConnectionString,
                CancellationToken.None).ConfigureAwait(false);
            ValidateCustomNetflixTargetSchema(customNetflixSchema, targetSchema);
        }

        var databaseFolder = Path.GetFullPath(Path.Combine(stagingPath, "Database")) + Path.DirectorySeparatorChar;
        foreach (var jsonPath in Directory.EnumerateFiles(stagingPath, "*.json", SearchOption.AllDirectories))
        {
            if (Path.GetFullPath(jsonPath).StartsWith(databaseFolder, StringComparison.Ordinal)
                || string.Equals(jsonPath, manifestPath, StringComparison.Ordinal))
            {
                continue;
            }

            var jsonStream = File.OpenRead(jsonPath);
            await using (jsonStream.ConfigureAwait(false))
            {
                using var document = await JsonDocument.ParseAsync(
                    jsonStream,
                    new JsonDocumentOptions
                    {
                        AllowTrailingCommas = true,
                        CommentHandling = JsonCommentHandling.Skip
                    }).ConfigureAwait(false);
            }
        }

        var xmlReaderSettings = new XmlReaderSettings
        {
            Async = true,
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
        foreach (var xmlPath in Directory.EnumerateFiles(stagingPath, "*", SearchOption.AllDirectories)
                     .Where(path => string.Equals(Path.GetExtension(path), ".xml", StringComparison.OrdinalIgnoreCase)))
        {
            using var reader = XmlReader.Create(xmlPath, xmlReaderSettings);
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
            }
        }

        if (!manifest.Options.Database)
        {
            return (manifest, null);
        }

        var historyPath = Path.Combine(stagingPath, "Database", $"{nameof(HistoryRow)}.json");
        if (!File.Exists(historyPath))
        {
            throw new InvalidOperationException("Cannot restore backup that has no History data.");
        }

        HistoryRow[]? historyEntries;
        var historyStream = File.OpenRead(historyPath);
        await using (historyStream.ConfigureAwait(false))
        {
            historyEntries = await JsonSerializer.DeserializeAsync<HistoryRow[]>(historyStream, _serializerSettings).ConfigureAwait(false);
        }

        if (historyEntries is null)
        {
            throw new InvalidOperationException("Cannot restore backup that has no History data.");
        }

        var legacyDatabaseTableName = typeof(DbSet<>).Name;
        foreach (var declaredTable in manifest.DatabaseTables
                     .Where(table => !string.Equals(table, legacyDatabaseTableName, StringComparison.Ordinal))
                     .Distinct(StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(declaredTable)
                || declaredTable.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0)
            {
                throw new InvalidOperationException($"The backup manifest declares an invalid database table name '{declaredTable}'.");
            }

            var declaredTablePath = Path.Combine(stagingPath, "Database", $"{declaredTable}.json");
            if (!File.Exists(declaredTablePath))
            {
                throw new InvalidOperationException($"No backup of declared table '{declaredTable}' is present in backup.");
            }
        }

        foreach (var entityType in GetDatabaseEntityTypes())
        {
            var tablePath = Path.Combine(stagingPath, "Database", $"{entityType.Name}.json");
            if (!File.Exists(tablePath))
            {
                continue;
            }

            var entityClrType = entityType.PropertyType.GetGenericArguments()[0];
            var tableStream = File.OpenRead(tablePath);
            await using (tableStream.ConfigureAwait(false))
            {
                await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<JsonObject>(tableStream, _serializerSettings).ConfigureAwait(false))
                {
                    if (item?.Deserialize(entityClrType, _serializerSettings) is null)
                    {
                        throw new InvalidOperationException($"Cannot deserialize entity in '{entityType.Name}'.");
                    }
                }
            }
        }

        return (manifest, historyEntries);
    }

    private void CopyStagedDirectory(
        string stagingPath,
        string source,
        string target,
        List<(string TargetPath, string? RollbackPath)> fileRollback,
        HashSet<string> journaledPaths,
        string[]? exclude = null)
    {
        var sourcePath = Path.GetFullPath(Path.Combine(stagingPath, source));
        if (!Directory.Exists(sourcePath))
        {
            return;
        }

        var fullTargetRoot = Path.GetFullPath(target) + Path.DirectorySeparatorChar;
        foreach (var item in Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourcePath, item);
            if (exclude is not null
                && exclude.Any(path => relativePath.StartsWith(path + Path.DirectorySeparatorChar, StringComparison.Ordinal)))
            {
                continue;
            }

            var targetPath = Path.GetFullPath(Path.Combine(target, relativePath));
            if (!targetPath.StartsWith(fullTargetRoot, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Backup entry '{relativePath}' escapes restore target '{target}'.");
            }

            _logger.LogInformation("Restore and override {File}", targetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            if (journaledPaths.Add(targetPath))
            {
                string? rollbackPath = null;
                if (File.Exists(targetPath))
                {
                    rollbackPath = Path.Combine(stagingPath, ".rollback", Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(Path.GetDirectoryName(rollbackPath)!);
                    File.Copy(targetPath, rollbackPath);
                }

                fileRollback.Add((targetPath, rollbackPath));
            }

            ReplaceFile(item, targetPath);
        }
    }

    private void RollbackFiles(List<(string TargetPath, string? RollbackPath)> fileRollback)
    {
        List<Exception>? errors = null;
        for (var index = fileRollback.Count - 1; index >= 0; index--)
        {
            var entry = fileRollback[index];
            try
            {
                if (entry.RollbackPath is null)
                {
                    File.Delete(entry.TargetPath);
                }
                else
                {
                    ReplaceFile(entry.RollbackPath, entry.TargetPath);
                }
            }
            catch (Exception ex)
            {
                (errors ??= []).Add(ex);
                _logger.LogError(ex, "Unable to roll back restored file {Path}", entry.TargetPath);
            }
        }

        if (errors is not null)
        {
            throw new AggregateException("One or more restored files could not be rolled back.", errors);
        }
    }

    private void EnsureRestoreArchiveFitsTempStorage(string archivePath)
    {
        long expandedSize;
        using (var archive = ZipFile.OpenRead(archivePath))
        {
            expandedSize = CalculateArchiveExpandedSize(archive.Entries.Select(entry => entry.Length));
        }

        EnsureTempStorageCapacity(
            CalculateRestoreTempRequirement(expandedSize, 0, 0),
            "archive extraction");
    }

    private void EnsureRestoreRollbackFitsTempStorage(string stagingPath, bool restoreCustomNetflixDatabase)
    {
        var rollbackFileSize = CalculateExistingFileRollbackSize(stagingPath);
        var databaseDumpSize = restoreCustomNetflixDatabase
            ? new FileInfo(GetStagedCustomNetflixDumpPath(stagingPath)).Length
            : 0;

        EnsureTempStorageCapacity(
            CalculateRestoreTempRequirement(0, rollbackFileSize, databaseDumpSize),
            "file and database rollback");
    }

    private void EnsureTempStorageCapacity(long requiredSize, string operation)
    {
        var storage = StorageHelper.GetFreeSpaceOf(_applicationPaths.TempDirectory);
        var freeSpace = storage.FreeSpace;
        if (freeSpace < 0)
        {
            try
            {
                var root = Path.GetPathRoot(Path.GetFullPath(_applicationPaths.TempDirectory));
                if (string.IsNullOrWhiteSpace(root))
                {
                    throw new InvalidOperationException("The temporary directory has no filesystem root.");
                }

                freeSpace = new DriveInfo(root).AvailableFreeSpace;
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"Unable to determine free space for restore temporary directory '{_applicationPaths.TempDirectory}'.",
                    ex);
            }
        }

        if (requiredSize > freeSpace)
        {
            throw new InvalidOperationException(
                $"The restore {operation} requires {StorageHelper.HumanizeStorageSize(requiredSize)} of temporary space, "
                + $"but only {StorageHelper.HumanizeStorageSize(freeSpace)} is available in '{_applicationPaths.TempDirectory}'.");
        }
    }

    internal long CalculateExistingFileRollbackSize(string stagingPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stagingPath);

        var journaledPaths = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        try
        {
            long total = 0;
            total = checked(total + CalculateExistingFileRollbackSize(
                stagingPath,
                "Config",
                _applicationPaths.ConfigurationDirectoryPath,
                journaledPaths));
            total = checked(total + CalculateExistingFileRollbackSize(
                stagingPath,
                "Data",
                _applicationPaths.DataPath,
                journaledPaths,
                ["metadata", "metadata-default"]));
            total = checked(total + CalculateExistingFileRollbackSize(
                stagingPath,
                "Root",
                _applicationPaths.RootFolderPath,
                journaledPaths));
            total = checked(total + CalculateExistingFileRollbackSize(
                stagingPath,
                "Data/metadata",
                _applicationPaths.InternalMetadataPath,
                journaledPaths));
            total = checked(total + CalculateExistingFileRollbackSize(
                stagingPath,
                "Data/metadata-default",
                _applicationPaths.DefaultInternalMetadataPath,
                journaledPaths));
            return total;
        }
        catch (OverflowException ex)
        {
            throw new InvalidDataException("The restore rollback file size exceeds the supported limit.", ex);
        }
    }

    private static long CalculateExistingFileRollbackSize(
        string stagingPath,
        string source,
        string target,
        HashSet<string> journaledPaths,
        string[]? exclude = null)
    {
        var sourcePath = Path.GetFullPath(Path.Combine(stagingPath, source));
        if (!Directory.Exists(sourcePath))
        {
            return 0;
        }

        long total = 0;
        var fullTargetRoot = Path.GetFullPath(target) + Path.DirectorySeparatorChar;
        foreach (var item in Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourcePath, item);
            if (exclude is not null
                && exclude.Any(path => relativePath.StartsWith(path + Path.DirectorySeparatorChar, StringComparison.Ordinal)))
            {
                continue;
            }

            var targetPath = Path.GetFullPath(Path.Combine(target, relativePath));
            if (!targetPath.StartsWith(fullTargetRoot, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Backup entry '{relativePath}' escapes restore target '{target}'.");
            }

            if (journaledPaths.Add(targetPath) && File.Exists(targetPath))
            {
                total = checked(total + new FileInfo(targetPath).Length);
            }
        }

        return total;
    }

    internal static long CalculateArchiveExpandedSize(IEnumerable<long> entryLengths)
    {
        ArgumentNullException.ThrowIfNull(entryLengths);
        try
        {
            long total = 0;
            foreach (var entryLength in entryLengths)
            {
                if (entryLength < 0)
                {
                    throw new InvalidDataException("A restore archive entry declares a negative expanded size.");
                }

                total = checked(total + entryLength);
            }

            return total;
        }
        catch (OverflowException ex)
        {
            throw new InvalidDataException("The restore archive expanded size exceeds the supported limit.", ex);
        }
    }

    internal static long CalculateRestoreTempRequirement(
        long archiveExpandedSize,
        long rollbackFileSize,
        long databaseDumpSize)
    {
        if (archiveExpandedSize < 0 || rollbackFileSize < 0 || databaseDumpSize < 0)
        {
            throw new InvalidDataException("Restore temporary space components cannot be negative.");
        }

        try
        {
            var payloadSize = checked(archiveExpandedSize + rollbackFileSize + databaseDumpSize);
            var proportionalMargin = payloadSize / RestoreSafetyMarginDivisor;
            if (payloadSize % RestoreSafetyMarginDivisor != 0)
            {
                proportionalMargin = checked(proportionalMargin + 1);
            }

            var safetyMargin = Math.Max(MinimumRestoreSafetyMarginBytes, proportionalMargin);
            return checked(payloadSize + safetyMargin);
        }
        catch (OverflowException ex)
        {
            throw new InvalidDataException("The restore temporary space requirement exceeds the supported limit.", ex);
        }
    }

    private void ReplaceFile(string source, string target)
    {
        var temporaryPath = $"{target}.{Guid.NewGuid():N}.restore";
        try
        {
            File.Copy(source, temporaryPath);
            File.Move(temporaryPath, target, true);
        }
        finally
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to remove temporary restore file {Path}", temporaryPath);
            }
        }
    }

    private async Task RestoreDatabasesWithRollbackAsync(
        string stagingPath,
        HistoryRow[] historyEntries,
        bool restoreCustomNetflixDatabase,
        string customNetflixDatabaseSchema)
    {
        _logger.LogInformation("Begin restoring databases");
        var customNetflixConnectionString = restoreCustomNetflixDatabase
            ? GetCustomNetflixConnectionString()
                ?? throw new InvalidOperationException("Cannot restore CustomNetflix PostgreSQL because it is not configured.")
            : null;
        var rollbackKey = await _jellyfinDatabaseProvider.MigrationBackupFast(CancellationToken.None).ConfigureAwait(false);
        string? customNetflixRollbackPath = null;
        string? customNetflixRollbackSchema = null;
        var customNetflixRestoreStarted = false;
        try
        {
            if (customNetflixConnectionString is not null)
            {
                customNetflixRollbackPath = GetTemporaryCustomNetflixDumpPath("rollback");
                customNetflixRollbackSchema = await CreateCustomNetflixDumpAsync(
                    customNetflixConnectionString,
                    customNetflixRollbackPath,
                    allowNoTables: true,
                    cancellationToken: CancellationToken.None).ConfigureAwait(false);
                if (customNetflixRollbackSchema is null)
                {
                    customNetflixRollbackPath = null;
                }
            }

            await RestoreDatabaseAsync(stagingPath, historyEntries).ConfigureAwait(false);

            if (customNetflixConnectionString is not null)
            {
                customNetflixRestoreStarted = true;
                await RestoreCustomNetflixDumpAsync(
                    customNetflixConnectionString,
                    GetStagedCustomNetflixDumpPath(stagingPath),
                    customNetflixDatabaseSchema,
                    CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception restoreException)
        {
            _logger.LogError(restoreException, "Database restore failed, rolling back all databases");
            List<Exception>? rollbackErrors = null;
            var jellyfinRollbackSucceeded = false;
            var customNetflixRollbackSucceeded = !customNetflixRestoreStarted
                || customNetflixRollbackPath is null;
            try
            {
                await _jellyfinDatabaseProvider.RestoreBackupFast(rollbackKey, CancellationToken.None).ConfigureAwait(false);
                jellyfinRollbackSucceeded = true;
            }
            catch (Exception rollbackException)
            {
                (rollbackErrors ??= []).Add(rollbackException);
                _logger.LogCritical(
                    rollbackException,
                    "Jellyfin database rollback failed. Preserving rollback backup {RollbackKey} for manual recovery.",
                    rollbackKey);
            }

            if (customNetflixRestoreStarted
                && customNetflixConnectionString is not null
                && customNetflixRollbackPath is not null)
            {
                try
                {
                    await RestoreCustomNetflixDumpAsync(
                        customNetflixConnectionString,
                        customNetflixRollbackPath,
                        customNetflixRollbackSchema!,
                        CancellationToken.None).ConfigureAwait(false);
                    customNetflixRollbackSucceeded = true;
                }
                catch (Exception rollbackException)
                {
                    (rollbackErrors ??= []).Add(rollbackException);
                    _logger.LogCritical(
                        rollbackException,
                        "CustomNetflix PostgreSQL rollback failed. Preserving rollback dump {RollbackPath} for manual recovery.",
                        customNetflixRollbackPath);
                }
            }

            if (jellyfinRollbackSucceeded)
            {
                await DeleteRollbackBackupAsync(rollbackKey).ConfigureAwait(false);
            }

            if (customNetflixRollbackSucceeded)
            {
                TryDeleteFile(customNetflixRollbackPath);
            }

            if (rollbackErrors is not null)
            {
                rollbackErrors.Insert(0, restoreException);
                throw new AggregateException("Database restore and rollback failed.", rollbackErrors);
            }

            throw;
        }

        await DeleteRollbackBackupAsync(rollbackKey).ConfigureAwait(false);
        TryDeleteFile(customNetflixRollbackPath);
        _logger.LogInformation("Restored database");
    }

    private async Task RestoreDatabaseAsync(string stagingPath, HistoryRow[] historyEntries)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var historyRepository = dbContext.GetService<IHistoryRepository>();
            await historyRepository.CreateIfNotExistsAsync().ConfigureAwait(false);

            foreach (var item in await historyRepository.GetAppliedMigrationsAsync(CancellationToken.None).ConfigureAwait(false))
            {
                await dbContext.Database.ExecuteSqlRawAsync(historyRepository.GetDeleteScript(item.MigrationId)).ConfigureAwait(false);
            }

            foreach (var item in historyEntries)
            {
                await dbContext.Database.ExecuteSqlRawAsync(historyRepository.GetInsertScript(item)).ConfigureAwait(false);
            }

            dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            var entityTypes = GetDatabaseEntityTypes();
            var tableNames = entityTypes.Select(entityType =>
                dbContext.Model.FindEntityType(entityType.PropertyType.GetGenericArguments()[0])!.GetSchemaQualifiedTableName()!);
            _logger.LogInformation("Begin purging database");
            await _jellyfinDatabaseProvider.PurgeDatabase(dbContext, tableNames).ConfigureAwait(false);
            _logger.LogInformation("Database Purged");

            foreach (var entityType in entityTypes)
            {
                var tableName = dbContext.Model.FindEntityType(entityType.PropertyType.GetGenericArguments()[0])!
                    .GetSchemaQualifiedTableName()!;
                var tablePath = Path.Combine(stagingPath, "Database", $"{tableName}.json");
                if (!File.Exists(tablePath))
                {
                    _logger.LogInformation(
                        "No backup of current table {Table} is present; a later migration may initialize it.",
                        tableName);
                    continue;
                }

                _logger.LogInformation("Restore backup of {Table}", tableName);
                var records = 0;
                var entityClrType = entityType.PropertyType.GetGenericArguments()[0];
                var tableStream = File.OpenRead(tablePath);
                await using (tableStream.ConfigureAwait(false))
                {
                    await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<JsonObject>(tableStream, _serializerSettings).ConfigureAwait(false))
                    {
                        var entity = item?.Deserialize(entityClrType, _serializerSettings)
                            ?? throw new InvalidOperationException($"Cannot deserialize entity in '{tableName}'.");
                        dbContext.Add(entity);
                        records++;
                    }
                }

                _logger.LogInformation("Prepared to restore {Number} entries for {Table}", records, tableName);
            }

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    private async Task DeleteRollbackBackupAsync(string key)
    {
        try
        {
            await _jellyfinDatabaseProvider.DeleteBackup(key).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to remove database rollback backup {Key}", key);
        }
    }

    private static System.Reflection.PropertyInfo[] GetDatabaseEntityTypes()
        => typeof(JellyfinDbContext)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(property => property.PropertyType.IsAssignableTo(typeof(IQueryable)))
            .ToArray();

    private bool TestBackupVersionCompatibility(Version backupEngineVersion)
    {
        if (backupEngineVersion == _backupEngineVersion)
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task<BackupManifestDto> CreateBackupAsync(BackupOptionsDto backupOptions)
    {
        await _backupCreationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            return await CreateBackupCoreAsync(backupOptions).ConfigureAwait(false);
        }
        finally
        {
            _backupCreationLock.Release();
        }
    }

    private async Task<BackupManifestDto> CreateBackupCoreAsync(BackupOptionsDto backupOptions)
    {
        var customNetflixConnectionString = GetCustomNetflixConnectionString();
        var manifest = new BackupManifest()
        {
            DateCreated = DateTime.UtcNow,
            ServerVersion = _applicationHost.ApplicationVersion,
            DatabaseTables = null!,
            BackupEngineVersion = _backupEngineVersion,
            Options = Map(backupOptions)
        };
        manifest.Options.CustomNetflixDatabase = backupOptions.Database
            && !string.IsNullOrWhiteSpace(customNetflixConnectionString);

        _logger.LogInformation("Running database optimization before backup");

        await _jellyfinDatabaseProvider.RunScheduledOptimisation(CancellationToken.None).ConfigureAwait(false);

        var backupFolder = Path.Combine(_applicationPaths.BackupPath);

        if (!Directory.Exists(backupFolder))
        {
            Directory.CreateDirectory(backupFolder);
        }

        var backupStorageSpace = StorageHelper.GetFreeSpaceOf(_applicationPaths.BackupPath);

        const long FiveGigabyte = 5_368_709_115;
        if (backupStorageSpace.FreeSpace < FiveGigabyte)
        {
            throw new InvalidOperationException($"The backup directory '{backupStorageSpace.Path}' does not have at least '{StorageHelper.HumanizeStorageSize(FiveGigabyte)}' free space. Cannot create backup.");
        }

        if (manifest.Options.CustomNetflixDatabase)
        {
            await FlushCustomNetflixBuffersAsync().ConfigureAwait(false);
        }

        var backupPath = Path.Combine(
            backupFolder,
            BuildBackupFileName(manifest.DateCreated, Guid.NewGuid()));
        var temporaryBackupPath = $"{backupPath}.partial";

        try
        {
            _logger.LogInformation("Attempting to create a new backup at {BackupPath}", backupPath);
            var fileStream = new FileStream(
                temporaryBackupPath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None);
            await using (fileStream.ConfigureAwait(false))
            using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, false))
            {
                _logger.LogInformation("Starting backup process");
                var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
                await using (dbContext.ConfigureAwait(false))
                {
                    dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                    static IAsyncEnumerable<object> GetValues(IQueryable dbSet)
                    {
                        var method = dbSet.GetType().GetMethod(nameof(DbSet<object>.AsAsyncEnumerable))!;
                        var enumerable = method.Invoke(dbSet, null)!;
                        return (IAsyncEnumerable<object>)enumerable;
                    }

                    // include the migration history as well
                    var historyRepository = dbContext.GetService<IHistoryRepository>();
                    var migrations = await historyRepository.GetAppliedMigrationsAsync().ConfigureAwait(false);

                    ICollection<(Type Type, string SourceName, Func<IAsyncEnumerable<object>> ValueFactory)> entityTypes =
                    [
                        .. typeof(JellyfinDbContext)
                            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                            .Where(e => e.PropertyType.IsAssignableTo(typeof(IQueryable)))
                            .Select(e => (Type: e.PropertyType, dbContext.Model.FindEntityType(e.PropertyType.GetGenericArguments()[0])!.GetSchemaQualifiedTableName()!, ValueFactory: new Func<IAsyncEnumerable<object>>(() => GetValues((IQueryable)e.GetValue(dbContext)!)))),
                        (Type: typeof(HistoryRow), SourceName: nameof(HistoryRow), ValueFactory: () => migrations.ToAsyncEnumerable())
                    ];
                    manifest.DatabaseTables = entityTypes.Select(e => e.SourceName).ToArray();
                    var transaction = await dbContext.Database.BeginTransactionAsync().ConfigureAwait(false);

                    await using (transaction.ConfigureAwait(false))
                    {
                        _logger.LogInformation("Begin Database backup");

                        foreach (var entityType in entityTypes)
                        {
                            _logger.LogInformation("Begin backup of entity {Table}", entityType.SourceName);
                            var zipEntry = zipArchive.CreateEntry(NormalizePathSeparator(Path.Combine("Database", $"{entityType.SourceName}.json")));
                            var entities = 0;
                            var zipEntryStream = await zipEntry.OpenAsync().ConfigureAwait(false);
                            await using (zipEntryStream.ConfigureAwait(false))
                            {
                                var jsonSerializer = new Utf8JsonWriter(zipEntryStream);
                                await using (jsonSerializer.ConfigureAwait(false))
                                {
                                    jsonSerializer.WriteStartArray();

                                    var set = entityType.ValueFactory().ConfigureAwait(false);
                                    await foreach (var item in set.ConfigureAwait(false))
                                    {
                                        entities++;
                                        try
                                        {
                                            using var document = JsonSerializer.SerializeToDocument(item, _serializerSettings);
                                            document.WriteTo(jsonSerializer);
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, "Could not load entity {Entity}", item);
                                            throw;
                                        }
                                    }

                                    jsonSerializer.WriteEndArray();
                                }
                            }

                            _logger.LogInformation("Backup of entity {Table} with {Number} created", entityType.SourceName, entities);
                        }
                    }
                }

                if (manifest.Options.CustomNetflixDatabase)
                {
                    // Reduce the write window created while serializing the native database.
                    // This is a queue barrier, not a producer lock: writes arriving afterward
                    // may still be included in PostgreSQL's snapshot.
                    await FlushCustomNetflixBuffersAsync().ConfigureAwait(false);
                    var customNetflixDumpPath = GetTemporaryCustomNetflixDumpPath("backup");
                    try
                    {
                        manifest.CustomNetflixDatabaseSchema = await CreateCustomNetflixDumpAsync(
                            customNetflixConnectionString!,
                            customNetflixDumpPath,
                            allowNoTables: false,
                            cancellationToken: CancellationToken.None).ConfigureAwait(false);
                        await zipArchive.CreateEntryFromFileAsync(
                            customNetflixDumpPath,
                            CustomNetflixDatabaseEntryName).ConfigureAwait(false);
                    }
                    finally
                    {
                        TryDeleteFile(customNetflixDumpPath);
                    }
                }

                _logger.LogInformation("Backup of folder {Table}", _applicationPaths.ConfigurationDirectoryPath);
                foreach (var item in Directory.EnumerateFiles(_applicationPaths.ConfigurationDirectoryPath, "*.xml", SearchOption.TopDirectoryOnly)
                             .Union(Directory.EnumerateFiles(_applicationPaths.ConfigurationDirectoryPath, "*.json", SearchOption.TopDirectoryOnly)))
                {
                    await zipArchive.CreateEntryFromFileAsync(item, NormalizePathSeparator(Path.Combine("Config", Path.GetFileName(item)))).ConfigureAwait(false);
                }

                void CopyDirectory(string source, string target, string filter = "*")
                {
                    if (!Directory.Exists(source))
                    {
                        return;
                    }

                    _logger.LogInformation("Backup of folder {Table}", source);

                    foreach (var item in Directory.EnumerateFiles(source, filter, SearchOption.AllDirectories))
                    {
                        // TODO: @bond make async
                        zipArchive.CreateEntryFromFile(item, NormalizePathSeparator(Path.Combine(target, Path.GetRelativePath(source, item))));
                    }
                }

                CopyDirectory(Path.Combine(_applicationPaths.ConfigurationDirectoryPath, "users"), Path.Combine("Config", "users"));
                CopyDirectory(Path.Combine(_applicationPaths.ConfigurationDirectoryPath, "ScheduledTasks"), Path.Combine("Config", "ScheduledTasks"));
                CopyDirectory(Path.Combine(_applicationPaths.RootFolderPath), "Root");
                CopyDirectory(Path.Combine(_applicationPaths.DataPath, "collections"), Path.Combine("Data", "collections"));
                CopyDirectory(Path.Combine(_applicationPaths.DataPath, "playlists"), Path.Combine("Data", "playlists"));
                CopyDirectory(Path.Combine(_applicationPaths.DataPath, "ScheduledTasks"), Path.Combine("Data", "ScheduledTasks"));
                if (backupOptions.Subtitles)
                {
                    CopyDirectory(Path.Combine(_applicationPaths.DataPath, "subtitles"), Path.Combine("Data", "subtitles"));
                }

                if (backupOptions.Trickplay)
                {
                    CopyDirectory(Path.Combine(_applicationPaths.DataPath, "trickplay"), Path.Combine("Data", "trickplay"));
                }

                if (backupOptions.Metadata)
                {
                    CopyDirectory(Path.Combine(_applicationPaths.InternalMetadataPath), Path.Combine("Data", "metadata"));

                    // If a custom metadata path is configured, the default location may still contain data.
                    if (!string.Equals(
                            Path.GetFullPath(_applicationPaths.DefaultInternalMetadataPath),
                            Path.GetFullPath(_applicationPaths.InternalMetadataPath),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        CopyDirectory(Path.Combine(_applicationPaths.DefaultInternalMetadataPath), Path.Combine("Data", "metadata-default"));
                    }
                }

                var manifestStream = await zipArchive.CreateEntry(ManifestEntryName).OpenAsync().ConfigureAwait(false);
                await using (manifestStream.ConfigureAwait(false))
                {
                    await JsonSerializer.SerializeAsync(manifestStream, manifest).ConfigureAwait(false);
                }
            }

            File.Move(temporaryBackupPath, backupPath);
            _logger.LogInformation("Backup created");
            return Map(manifest, backupPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup, removing {BackupPath}", backupPath);
            try
            {
                if (File.Exists(temporaryBackupPath))
                {
                    File.Delete(temporaryBackupPath);
                }
            }
            catch (Exception innerEx)
            {
                _logger.LogWarning(innerEx, "Unable to remove failed backup");
            }

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<BackupManifestDto?> GetBackupManifest(string archivePath)
    {
        if (!File.Exists(archivePath))
        {
            return null;
        }

        BackupManifest? manifest;
        try
        {
            manifest = await GetManifest(archivePath).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tried to load manifest from archive {Path} but failed", archivePath);
            return null;
        }

        if (manifest is null)
        {
            return null;
        }

        return Map(manifest, archivePath);
    }

    /// <inheritdoc/>
    public async Task<BackupManifestDto[]> EnumerateBackups()
    {
        if (!Directory.Exists(_applicationPaths.BackupPath))
        {
            return [];
        }

        var archives = Directory.EnumerateFiles(_applicationPaths.BackupPath, "*.zip");
        var manifests = new List<BackupManifestDto>();
        foreach (var item in archives)
        {
            try
            {
                var manifest = await GetManifest(item).ConfigureAwait(false);

                if (manifest is null)
                {
                    continue;
                }

                manifests.Add(Map(manifest, item));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tried to load manifest from archive {Path} but failed", item);
            }
        }

        return manifests.ToArray();
    }

    private static async ValueTask<BackupManifest?> GetManifest(string archivePath)
    {
        var archiveStream = File.OpenRead(archivePath);
        await using (archiveStream.ConfigureAwait(false))
        {
            using var zipStream = new ZipArchive(archiveStream, ZipArchiveMode.Read);
            var manifestEntry = zipStream.GetEntry(ManifestEntryName);
            if (manifestEntry is null)
            {
                return null;
            }

            var manifestStream = await manifestEntry.OpenAsync().ConfigureAwait(false);
            await using (manifestStream.ConfigureAwait(false))
            {
                return await JsonSerializer.DeserializeAsync<BackupManifest>(manifestStream, _serializerSettings).ConfigureAwait(false);
            }
        }
    }

    private static BackupManifestDto Map(BackupManifest manifest, string path)
    {
        return new BackupManifestDto()
        {
            BackupEngineVersion = manifest.BackupEngineVersion,
            DateCreated = manifest.DateCreated,
            ServerVersion = manifest.ServerVersion,
            Path = path,
            Options = Map(manifest.Options)
        };
    }

    private static BackupOptionsDto Map(BackupOptions options)
    {
        return new BackupOptionsDto()
        {
            Metadata = options.Metadata,
            Subtitles = options.Subtitles,
            Trickplay = options.Trickplay,
            Database = options.Database,
            CustomNetflixDatabase = options.CustomNetflixDatabase
        };
    }

    private static BackupOptions Map(BackupOptionsDto options)
    {
        return new BackupOptions()
        {
            Metadata = options.Metadata,
            Subtitles = options.Subtitles,
            Trickplay = options.Trickplay,
            Database = options.Database,
            CustomNetflixDatabase = options.CustomNetflixDatabase
        };
    }

    private string? GetCustomNetflixConnectionString()
        => _configuration["CustomNetflix:PostgreSqlConnectionString"]
            ?? _configuration["CustomNetflix:PostgresConnectionString"]
            ?? _configuration.GetConnectionString("CustomNetflixPostgres");

    internal async Task FlushCustomNetflixBuffersAsync()
    {
        if (_watchProgressBuffer is null && _watchEventBuffer is null)
        {
            return;
        }

        var timeoutSeconds = Math.Max(
            30,
            _configuration.GetValue("CustomNetflix:DatabaseBackupTimeoutSeconds", 300));
        using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        List<Task> flushTasks = [];
        if (_watchProgressBuffer is not null)
        {
            flushTasks.Add(_watchProgressBuffer.FlushAsync(timeoutSource.Token));
        }

        if (_watchEventBuffer is not null)
        {
            flushTasks.Add(_watchEventBuffer.FlushAsync(timeoutSource.Token));
        }

        await Task.WhenAll(flushTasks).ConfigureAwait(false);
    }

    private string GetTemporaryCustomNetflixDumpPath(string purpose)
    {
        Directory.CreateDirectory(_applicationPaths.TempDirectory);
        return Path.Combine(_applicationPaths.TempDirectory, $"customnetflix-{purpose}-{Guid.NewGuid():N}.pgdump");
    }

    private static string GetStagedCustomNetflixDumpPath(string stagingPath)
        => Path.Combine(stagingPath, CustomNetflixDatabaseEntryName.Replace('/', Path.DirectorySeparatorChar));

    private async Task<string?> CreateCustomNetflixDumpAsync(
        string connectionString,
        string destinationPath,
        bool allowNoTables,
        CancellationToken cancellationToken)
    {
        var (schema, hasTables) = await InspectCustomNetflixSchemaAsync(
            connectionString,
            cancellationToken).ConfigureAwait(false);
        if (!ShouldCreateCustomNetflixDump(hasTables, allowNoTables))
        {
            return null;
        }

        await RunPostgreSqlToolAsync(
            _configuration["CustomNetflix:PgDumpPath"] ?? "pg_dump",
            connectionString,
            BuildCustomNetflixDumpArguments(schema!, destinationPath),
            cancellationToken).ConfigureAwait(false);
        await ValidateCustomNetflixDumpAsync(destinationPath, schema!, cancellationToken).ConfigureAwait(false);
        return schema;
    }

    private async Task RestoreCustomNetflixDumpAsync(
        string connectionString,
        string sourcePath,
        string schema,
        CancellationToken cancellationToken)
    {
        await RunPostgreSqlToolAsync(
            _configuration["CustomNetflix:PgRestorePath"] ?? "pg_restore",
            connectionString,
            BuildCustomNetflixRestoreArguments(schema, sourcePath),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task ValidateCustomNetflixDumpAsync(
        string sourcePath,
        string schema,
        CancellationToken cancellationToken)
    {
        ValidateCustomNetflixSchemaName(schema);
        var executable = _configuration["CustomNetflix:PgRestorePath"] ?? "pg_restore";
        var completeToc = await RunPostgreSqlToolAsync(
            executable,
            connectionString: null,
            arguments: ["--list", sourcePath],
            cancellationToken: cancellationToken).ConfigureAwait(false);
        var scopedToc = await RunPostgreSqlToolAsync(
            executable,
            connectionString: null,
            arguments: ["--list", $"--schema={schema}", "--strict-names", sourcePath],
            cancellationToken: cancellationToken).ConfigureAwait(false);
        ValidateCustomNetflixDumpToc(completeToc, scopedToc, schema);
    }

    private static async Task<(string? Schema, bool HasTables)> InspectCustomNetflixSchemaAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var schemaCommand = new NpgsqlCommand("select current_schema()", connection);
        var schema = await schemaCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
        if (string.IsNullOrWhiteSpace(schema))
        {
            return (schema, false);
        }

        await using var tableCommand = new NpgsqlCommand(
            """
            select exists (
                select 1
                from pg_catalog.pg_class as c
                inner join pg_catalog.pg_namespace as n on n.oid = c.relnamespace
                where n.nspname = @schema
                    and c.relname like 'cnx\_%' escape '\'
                    and c.relkind in ('r', 'p')
            )
            """,
            connection);
        tableCommand.Parameters.AddWithValue("schema", schema);
        var hasTables = (bool)(await tableCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) ?? false);
        return (schema, hasTables);
    }

    internal static void ValidateCustomNetflixTargetSchema(string manifestSchema, string? targetSchema)
    {
        ValidateCustomNetflixSchemaName(manifestSchema);
        if (string.IsNullOrWhiteSpace(targetSchema))
        {
            throw new InvalidOperationException("The configured CustomNetflix PostgreSQL connection has no current schema.");
        }

        ValidateCustomNetflixSchemaName(targetSchema);
        if (!string.Equals(manifestSchema, targetSchema, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"The backup uses CustomNetflix PostgreSQL schema '{manifestSchema}', "
                + $"but the configured target connection uses current schema '{targetSchema}'.");
        }
    }

    internal static void ValidateCustomNetflixDumpToc(
        string completeToc,
        string scopedToc,
        string schema)
    {
        ArgumentNullException.ThrowIfNull(completeToc);
        ArgumentNullException.ThrowIfNull(scopedToc);
        ValidateCustomNetflixSchemaName(schema);
        var completeEntries = ParsePostgreSqlToc(completeToc);
        var scopedEntries = ParsePostgreSqlToc(scopedToc);
        var scopedObjectCount = 0;

        foreach (var entry in scopedEntries)
        {
            if (!completeEntries.TryGetValue(entry.Key, out var completePayload)
                || !string.Equals(entry.Value, completePayload, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"CustomNetflix PostgreSQL dump has an inconsistent filtered TOC entry {entry.Key}.");
            }

            if (IsSafeGlobalPostgreSqlTocEntry(entry.Value))
            {
                continue;
            }

            ValidateScopedCustomNetflixTocEntry(entry.Key, entry.Value, schema);
            scopedObjectCount++;
        }

        if (scopedObjectCount == 0)
        {
            throw new InvalidDataException(
                $"CustomNetflix PostgreSQL dump contains no objects in schema '{schema}'.");
        }

        foreach (var entry in completeEntries)
        {
            if (!scopedEntries.ContainsKey(entry.Key)
                && !IsSafeGlobalPostgreSqlTocEntry(entry.Value))
            {
                throw new InvalidDataException(
                    $"CustomNetflix PostgreSQL dump contains out-of-scope TOC entry {entry.Key}: {entry.Value}.");
            }
        }
    }

    private static Dictionary<int, string> ParsePostgreSqlToc(string toc)
    {
        if (toc.Length > MaxCustomNetflixTocCharacters)
        {
            throw new InvalidDataException("CustomNetflix PostgreSQL dump TOC exceeds the validation limit.");
        }

        var lines = toc.Split('\n');
        var archiveFormats = lines
            .Select(line => line.Trim())
            .Where(line => line.StartsWith(';'))
            .Select(line => line[1..].TrimStart())
            .Where(line => line.StartsWith("Format:", StringComparison.Ordinal))
            .Select(line => line["Format:".Length..].Trim())
            .ToArray();
        if (archiveFormats.Length != 1
            || !string.Equals(archiveFormats[0], "CUSTOM", StringComparison.Ordinal))
        {
            throw new InvalidDataException("CustomNetflix PostgreSQL dump is not a custom-format archive.");
        }

        Dictionary<int, string> entries = [];
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line[0] == ';')
            {
                continue;
            }

            var separatorIndex = line.IndexOf(';', StringComparison.Ordinal);
            if (separatorIndex <= 0
                || !int.TryParse(
                    line.AsSpan(0, separatorIndex),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var dumpId))
            {
                throw new InvalidDataException($"CustomNetflix PostgreSQL dump has an invalid TOC line: {line}");
            }

            var payload = line[(separatorIndex + 1)..].Trim();
            if (payload.Length == 0
                || entries.Count >= MaxCustomNetflixTocEntries
                || !entries.TryAdd(dumpId, payload))
            {
                throw new InvalidDataException($"CustomNetflix PostgreSQL dump has an invalid TOC entry {dumpId}.");
            }
        }

        return entries;
    }

    private static bool IsSafeGlobalPostgreSqlTocEntry(string payload)
    {
        var details = RemovePostgreSqlTocCatalogIds(payload);
        return details.StartsWith("ENCODING - ", StringComparison.Ordinal)
            || details.StartsWith("POST-DATA BOUNDARY - ", StringComparison.Ordinal)
            || details.StartsWith("PRE-DATA BOUNDARY - ", StringComparison.Ordinal)
            || details.StartsWith("SEARCHPATH - ", StringComparison.Ordinal)
            || details.StartsWith("STDSTRINGS - ", StringComparison.Ordinal);
    }

    private static void ValidateScopedCustomNetflixTocEntry(int dumpId, string payload, string schema)
    {
        var details = RemovePostgreSqlTocCatalogIds(payload);
        var objectType = _allowedCustomNetflixTocObjectTypes
            .FirstOrDefault(type => details.StartsWith(type + " ", StringComparison.Ordinal));
        if (objectType is null)
        {
            throw new InvalidDataException(
                $"CustomNetflix PostgreSQL dump TOC entry {dumpId} has an unsupported object type.");
        }

        var schemaAndNames = details[(objectType.Length + 1)..];
        var schemaPrefix = schema + " ";
        if (!schemaAndNames.StartsWith(schemaPrefix, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"CustomNetflix PostgreSQL dump TOC entry {dumpId} is not in declared schema '{schema}': {payload}.");
        }

        var nameAndOwner = schemaAndNames[schemaPrefix.Length..];
        var tokens = nameAndOwner.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var isValid = objectType switch
        {
            "INDEX" => tokens.Length >= 2,
            "COMMENT" => IsValidCustomNetflixCommentTocEntry(tokens),
            _ => tokens.Length >= 2 && IsCustomNetflixTableName(tokens[0])
        };
        if (!isValid)
        {
            throw new InvalidDataException(
                $"CustomNetflix PostgreSQL dump TOC entry {dumpId} is not attached to a cnx_* object: {payload}.");
        }
    }

    private static bool IsValidCustomNetflixCommentTocEntry(string[] tokens)
    {
        if (tokens.Length < 3)
        {
            return false;
        }

        return tokens[0] switch
        {
            "COLUMN" or "CONSTRAINT" or "TABLE" => IsCustomNetflixTableName(tokens[1]),
            // pg_dump -t includes subsidiary indexes even when a DBA chose a non-cnx name.
            "INDEX" => true,
            _ => false
        };
    }

    private static bool IsCustomNetflixTableName(string value)
        => value.Trim('"').StartsWith("cnx_", StringComparison.Ordinal);

    private static string RemovePostgreSqlTocCatalogIds(string payload)
    {
        var index = 0;
        for (var tokenNumber = 0; tokenNumber < 2; tokenNumber++)
        {
            while (index < payload.Length && char.IsWhiteSpace(payload[index]))
            {
                index++;
            }

            var start = index;
            while (index < payload.Length && !char.IsWhiteSpace(payload[index]))
            {
                index++;
            }

            if (start == index
                || !ulong.TryParse(
                    payload.AsSpan(start, index - start),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out _))
            {
                throw new InvalidDataException($"CustomNetflix PostgreSQL dump has invalid TOC catalog identifiers: {payload}");
            }
        }

        while (index < payload.Length && char.IsWhiteSpace(payload[index]))
        {
            index++;
        }

        if (index == payload.Length)
        {
            throw new InvalidDataException($"CustomNetflix PostgreSQL dump has an incomplete TOC entry: {payload}");
        }

        return payload[index..];
    }

    private static void ValidateCustomNetflixSchemaName(string schema)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schema);
        if (schema.IndexOfAny(['\r', '\n']) >= 0)
        {
            throw new ArgumentException(
                "CustomNetflix PostgreSQL schema names containing line breaks are not supported.",
                nameof(schema));
        }
    }

    private static string QuotePostgreSqlIdentifier(string identifier)
        => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    internal static string BuildCustomNetflixTablePattern(string schema)
    {
        ValidateCustomNetflixSchemaName(schema);
        return $"{QuotePostgreSqlIdentifier(schema)}.cnx_*";
    }

    internal static IReadOnlyList<string> BuildCustomNetflixDumpArguments(
        string schema,
        string destinationPath)
    {
        List<string> arguments =
        [
            "--format=custom",
            "--no-owner",
            "--no-acl",
            "--strict-names"
        ];
        arguments.Add($"--table={BuildCustomNetflixTablePattern(schema)}");
        arguments.Add($"--file={destinationPath}");
        return arguments;
    }

    internal static bool ShouldCreateCustomNetflixDump(bool hasTables, bool allowNoTables)
    {
        if (hasTables)
        {
            return true;
        }

        if (allowNoTables)
        {
            return false;
        }

        throw new InvalidOperationException("CustomNetflix PostgreSQL has no cnx_* tables to back up.");
    }

    internal static IReadOnlyList<string> BuildCustomNetflixRestoreArguments(
        string schema,
        string sourcePath)
    {
        ValidateCustomNetflixSchemaName(schema);
        return
        [
            "--clean",
            "--if-exists",
            "--no-owner",
            "--no-acl",
            $"--schema={schema}",
            "--strict-names",
            "--exit-on-error",
            "--single-transaction",
            sourcePath
        ];
    }

    internal static string BuildBackupFileName(DateTimeOffset dateCreated, Guid uniqueId)
        => $"jellyfin-backup-{dateCreated.ToLocalTime():yyyyMMddHHmmssfff}-{uniqueId:N}.zip";

    private async Task<string> RunPostgreSqlToolAsync(
        string executable,
        string? connectionString,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        if (connectionString is not null)
        {
            var connection = new NpgsqlConnectionStringBuilder(connectionString);
            AddArgument(startInfo, "--host", connection.Host);
            AddArgument(startInfo, "--port", connection.Port.ToString(CultureInfo.InvariantCulture));
            AddArgument(startInfo, "--username", connection.Username);
            AddArgument(startInfo, "--dbname", connection.Database);
            if (!string.IsNullOrEmpty(connection.Password))
            {
                startInfo.Environment["PGPASSWORD"] = connection.Password;
            }

            SetPostgreSqlEnvironment(startInfo, "PGPASSFILE", connection.Passfile);
            SetPostgreSqlEnvironment(startInfo, "PGSSLCERT", connection.SslCertificate);
            SetPostgreSqlEnvironment(startInfo, "PGSSLKEY", connection.SslKey);
            SetPostgreSqlEnvironment(startInfo, "PGSSLPASSWORD", connection.SslPassword);
            SetPostgreSqlEnvironment(startInfo, "PGSSLROOTCERT", connection.RootCertificate);
            startInfo.Environment["PGSSLMODE"] = connection.SslMode.ToString() switch
            {
                "VerifyCA" => "verify-ca",
                "VerifyFull" => "verify-full",
                var value => value.ToLowerInvariant()
            };
        }

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException(
                $"PostgreSQL backup tool '{executable}' is required when CustomNetflix PostgreSQL is configured.",
                ex);
        }

        var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var timeoutSeconds = Math.Max(30, _configuration.GetValue("CustomNetflix:DatabaseBackupTimeoutSeconds", 300));
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await TerminateProcessAsync(process).ConfigureAwait(false);
            throw new TimeoutException($"PostgreSQL backup tool '{executable}' exceeded {timeoutSeconds} seconds.");
        }
        catch (OperationCanceledException)
        {
            await TerminateProcessAsync(process).ConfigureAwait(false);
            throw;
        }

        var error = await standardError.ConfigureAwait(false);
        var output = await standardOutput.ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"PostgreSQL backup tool '{executable}' failed with exit code {process.ExitCode}: {error.Trim()}");
        }

        return output;
    }

    private static void SetPostgreSqlEnvironment(
        ProcessStartInfo startInfo,
        string name,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            startInfo.Environment[name] = value;
        }
    }

    private static void AddArgument(ProcessStartInfo startInfo, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        startInfo.ArgumentList.Add(name);
        startInfo.ArgumentList.Add(value);
    }

    private static async Task TerminateProcessAsync(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(true);
            }

            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
        {
        }
    }

    private void TryDeleteFile(string? path)
    {
        if (path is null)
        {
            return;
        }

        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to remove temporary CustomNetflix PostgreSQL dump {Path}", path);
        }
    }

    /// <summary>
    /// Windows is able to handle '/' as a path seperator in zip files
    /// but linux isn't able to handle '\' as a path seperator in zip files,
    /// So normalize to '/'.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path. </returns>
    private static string NormalizePathSeparator(string path)
        => path.Replace('\\', '/');
}
