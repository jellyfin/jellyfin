using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Removes the empty <c>trickplay</c> directories left under <c>metadata/library</c> by <see cref="MoveTrickplayFiles"/>,
/// which moved the tiles into the trickplay data path but never deleted the old parent directory.
/// Directories that still contain files are left alone.
/// </summary>
[JellyfinMigration("2026-10-01T12:00:00", nameof(RemoveEmptyLegacyTrickplayDirectories))]
public class RemoveEmptyLegacyTrickplayDirectories : IAsyncMigrationRoutine
{
    private const int ProgressLogStep = 500;

    private readonly IStartupLogger<RemoveEmptyLegacyTrickplayDirectories> _logger;
    private readonly IServerApplicationPaths _serverPaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveEmptyLegacyTrickplayDirectories"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="serverPaths">The server application paths.</param>
    public RemoveEmptyLegacyTrickplayDirectories(
        IStartupLogger<RemoveEmptyLegacyTrickplayDirectories> logger,
        IServerApplicationPaths serverPaths)
    {
        _logger = logger;
        _serverPaths = serverPaths;
    }

    /// <inheritdoc/>
    public Task PerformAsync(CancellationToken cancellationToken)
    {
        var root = Path.Combine(_serverPaths.InternalMetadataPath, "library");
        if (!Directory.Exists(root))
        {
            _logger.LogInformation("Skipping legacy trickplay cleanup; root {Root} does not exist", root);
            return Task.CompletedTask;
        }

        var scanned = 0;
        var removed = 0;
        foreach (var prefixDir in Directory.EnumerateDirectories(root))
        {
            foreach (var itemDir in Directory.EnumerateDirectories(prefixDir))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var trickplayDir = Path.Combine(itemDir, "trickplay");
                if (!Directory.Exists(trickplayDir))
                {
                    continue;
                }

                scanned++;
                if (scanned % ProgressLogStep == 0)
                {
                    _logger.LogInformation("Legacy trickplay cleanup: {Scanned} directories examined, {Removed} removed so far", scanned, removed);
                }

                if (TryRemoveEmptyTrickplayDirectory(itemDir, trickplayDir))
                {
                    removed++;
                }
            }
        }

        _logger.LogInformation("Finished legacy trickplay cleanup: found {Scanned} directories, removed {Removed} empty ones", scanned, removed);
        return Task.CompletedTask;
    }

    private bool TryRemoveEmptyTrickplayDirectory(string itemDir, string trickplayDir)
    {
        if (Directory.EnumerateFiles(trickplayDir, "*", SearchOption.AllDirectories).Any())
        {
            return false;
        }

        // Recursive because empty width subdirectories (e.g. "320 - 10x10") can remain; there are no files to lose.
        if (!TryDelete(trickplayDir, recursive: true))
        {
            return false;
        }

        // The item directory may have existed only to hold trickplay tiles.
        if (!Directory.EnumerateFileSystemEntries(itemDir).Any())
        {
            TryDelete(itemDir, recursive: false);
        }

        return true;
    }

    private bool TryDelete(string dir, bool recursive)
    {
        try
        {
            Directory.Delete(dir, recursive);
            return true;
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to delete legacy trickplay directory {Dir}", dir);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Permission denied deleting legacy trickplay directory {Dir}", dir);
        }

        return false;
    }
}
