using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller;
using MediaBrowser.Model.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Deletes backdrop files in the internal metadata folder that are byte-identical to another backdrop of the
/// same item, and their rows. Older servers saved a backdrop URL from an NFO file as a new file on each
/// replace-images refresh, so one item can hold thousands of copies of one image.
/// </summary>
[JellyfinMigration("2026-10-11T12:00:00", nameof(RemoveDuplicateBackdrops))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class RemoveDuplicateBackdrops : IAsyncMigrationRoutine
{
    private const int BatchSize = 500;

    private readonly IStartupLogger<RemoveDuplicateBackdrops> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;
    private readonly IServerApplicationHost _appHost;
    private readonly IServerApplicationPaths _appPaths;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveDuplicateBackdrops"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    /// <param name="appHost">The application host.</param>
    /// <param name="appPaths">The server application paths.</param>
    /// <param name="fileSystem">The file system.</param>
    public RemoveDuplicateBackdrops(
        IStartupLogger<RemoveDuplicateBackdrops> logger,
        IDbContextFactory<JellyfinDbContext> dbContextFactory,
        IServerApplicationHost appHost,
        IServerApplicationPaths appPaths,
        IFileSystem fileSystem)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _appHost = appHost;
        _appPaths = appPaths;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            var itemIds = await context.BaseItemImageInfos
                .Where(e => e.ImageType == ImageInfoImageType.Backdrop)
                .GroupBy(e => e.ItemId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (itemIds.Count == 0)
            {
                _logger.LogInformation("No items with more than one backdrop found.");
                return;
            }

            _logger.LogInformation("Checking {Count} items with more than one backdrop for duplicates.", itemIds.Count);

            var metadataPath = Path.GetFullPath(_appPaths.InternalMetadataPath);
            var duplicates = new List<(Guid Id, string Path)>();
            foreach (var batch in itemIds.Chunk(BatchSize))
            {
                var backdrops = await context.BaseItemImageInfos
                    .AsNoTracking()
                    .Where(e => batch.Contains(e.ItemId) && e.ImageType == ImageInfoImageType.Backdrop)
                    .Select(e => new { e.ItemId, e.Id, e.Path })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                foreach (var item in backdrops.GroupBy(e => e.ItemId))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var itemDuplicates = await FindDuplicatesAsync(item.Select(e => (e.Id, e.Path)), metadataPath, cancellationToken).ConfigureAwait(false);
                    if (itemDuplicates.Count > 0)
                    {
                        _logger.LogInformation("Found {Count} duplicate backdrops for item {ItemId}.", itemDuplicates.Count, item.Key);
                        duplicates.AddRange(itemDuplicates);
                    }
                }
            }

            if (duplicates.Count == 0)
            {
                _logger.LogInformation("No duplicate backdrops found.");
                return;
            }

            // Rows go before files, so a failure leaves an unreferenced file and never a row without its file.
            var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                foreach (var batch in duplicates.Chunk(BatchSize))
                {
                    var ids = batch.Select(e => e.Id).ToArray();
                    await context.BaseItemImageInfos
                        .Where(e => ids.Contains(e.Id))
                        .ExecuteDeleteAsync(cancellationToken)
                        .ConfigureAwait(false);
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            var deletedFiles = duplicates.Count(e => TryDeleteFile(e.Path));
            _logger.LogInformation("Removed {Rows} duplicate backdrop rows and {Files} files.", duplicates.Count, deletedFiles);
        }
    }

    private async Task<List<(Guid Id, string Path)>> FindDuplicatesAsync(IEnumerable<(Guid Id, string Path)> backdrops, string metadataPath, CancellationToken cancellationToken)
    {
        var duplicates = new List<(Guid Id, string Path)>();
        var sameLength = backdrops
            .Select(e => (e.Id, Path: _appHost.ExpandVirtualPath(e.Path)))
            .Where(e => Path.IsPathFullyQualified(e.Path))
            .Select(e => (e.Id, Path: Path.GetFullPath(e.Path)))
            .Where(e => _fileSystem.ContainsSubPath(metadataPath, e.Path))
            .Select(e => (Backdrop: e, File: new FileInfo(e.Path)))
            .Where(e => e.File.Exists)
            .OrderBy(e => e.Backdrop.Path, StringComparer.Ordinal)
            .GroupBy(e => e.File.Length, e => e.Backdrop)
            .Where(g => g.Count() > 1);

        foreach (var group in sameLength)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var backdrop in group)
            {
                var hash = await TryHashFileAsync(backdrop.Path, cancellationToken).ConfigureAwait(false);
                if (hash is not null && !seen.Add(hash))
                {
                    duplicates.Add(backdrop);
                }
            }
        }

        return duplicates;
    }

    private async Task<string?> TryHashFileAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var stream = File.OpenRead(path);
            await using (stream.ConfigureAwait(false))
            {
                var hash = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
                return Convert.ToHexString(hash);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to read backdrop {Path}", path);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Permission denied reading backdrop {Path}", path);
        }

        return null;
    }

    private bool TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
            return true;
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to delete duplicate backdrop {Path}", path);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Permission denied deleting duplicate backdrop {Path}", path);
        }

        return false;
    }
}
