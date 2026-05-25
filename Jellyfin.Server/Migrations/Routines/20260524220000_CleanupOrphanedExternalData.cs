using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Removes on-disk external item data (attachments, subtitles, trickplay tiles, chapter images) for items that
/// no longer exist in the <c>BaseItems</c> table. The database side is cleaned up synchronously by
/// <c>IItemPersistenceService.DeleteItem</c>, so the leftover orphans live on the filesystem.
/// </summary>
[JellyfinMigration("2026-05-25T01:00:00", nameof(CleanupOrphanedExternalData))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class CleanupOrphanedExternalData : IAsyncMigrationRoutine
{
    private const int ProgressLogStep = 500;

    private readonly IStartupLogger<CleanupOrphanedExternalData> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;
    private readonly IApplicationPaths _appPaths;
    private readonly IServerApplicationPaths _serverPaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanupOrphanedExternalData"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    /// <param name="appPaths">The application paths.</param>
    /// <param name="serverPaths">The server application paths.</param>
    public CleanupOrphanedExternalData(
        IStartupLogger<CleanupOrphanedExternalData> logger,
        IDbContextFactory<JellyfinDbContext> dbContextFactory,
        IApplicationPaths appPaths,
        IServerApplicationPaths serverPaths)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _appPaths = appPaths;
        _serverPaths = serverPaths;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var knownIds = await LoadKnownItemIdsAsync(cancellationToken).ConfigureAwait(false);

        CleanupGuidIndexedRoot(
            "attachment",
            Path.Combine(_appPaths.DataPath, "attachments"),
            knownIds,
            deleteSubPath: null,
            cancellationToken);

        CleanupGuidIndexedRoot(
            "subtitle",
            Path.Combine(_appPaths.DataPath, "subtitles"),
            knownIds,
            deleteSubPath: null,
            cancellationToken);

        CleanupGuidIndexedRoot(
            "trickplay",
            _appPaths.TrickplayPath,
            knownIds,
            deleteSubPath: null,
            cancellationToken);

        CleanupGuidIndexedRoot(
            "chapter image",
            Path.Combine(_serverPaths.InternalMetadataPath, "library"),
            knownIds,
            deleteSubPath: "chapters",
            cancellationToken);
    }

    private async Task<HashSet<Guid>> LoadKnownItemIdsAsync(CancellationToken cancellationToken)
    {
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            var ids = await context.BaseItems
                .AsNoTracking()
                .Select(b => b.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            return [.. ids];
        }
    }

    private void CleanupGuidIndexedRoot(
        string label,
        string root,
        HashSet<Guid> knownIds,
        string? deleteSubPath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
        {
            _logger.LogInformation("Skipping {Label} cleanup; root {Root} does not exist", label, root);
            return;
        }

        _logger.LogInformation("Scanning for orphaned {Label} data under {Root}", label, root);

        var scanned = 0;
        var removed = 0;
        foreach (var prefixDir in Directory.EnumerateDirectories(root))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var prefixName = Path.GetFileName(prefixDir);
            if (prefixName.Length != 2)
            {
                continue;
            }

            foreach (var guidDir in Directory.EnumerateDirectories(prefixDir))
            {
                cancellationToken.ThrowIfCancellationRequested();

                scanned++;
                if (scanned % ProgressLogStep == 0)
                {
                    _logger.LogInformation("Scanning {Label}: {Scanned} directories examined, {Removed} orphans removed so far", label, scanned, removed);
                }

                var leafName = Path.GetFileName(guidDir);
                if (!Guid.TryParse(leafName, CultureInfo.InvariantCulture, out var id))
                {
                    continue;
                }

                if (knownIds.Contains(id))
                {
                    continue;
                }

                var target = deleteSubPath is null ? guidDir : Path.Combine(guidDir, deleteSubPath);
                if (deleteSubPath is not null && !Directory.Exists(target))
                {
                    continue;
                }

                if (TryDelete(target))
                {
                    removed++;
                }
            }
        }

        _logger.LogInformation("Finished {Label} cleanup: scanned {Scanned} directories, removed {Removed} orphans", label, scanned, removed);
    }

    private bool TryDelete(string dir)
    {
        try
        {
            Directory.Delete(dir, recursive: true);
            return true;
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to delete orphaned directory {Dir}", dir);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Permission denied deleting orphaned directory {Dir}", dir);
        }

        return false;
    }
}
