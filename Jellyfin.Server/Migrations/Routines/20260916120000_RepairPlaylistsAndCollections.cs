using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Emby.Server.Implementations.Playlists;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Repairs playlist and collection rows that earlier migrations would have deleted, and recreates
/// playlists whose folder is still on disk but whose item is no longer in the database.
/// </summary>
[JellyfinMigration("2026-09-16T12:00:00", nameof(RepairPlaylistsAndCollections))]
[JellyfinMigrationBackup(JellyfinDb = true)]
internal class RepairPlaylistsAndCollections : IAsyncMigrationRoutine
{
    private const string PlaylistFileName = "playlist.xml";
    private const string PlaylistType = "MediaBrowser.Controller.Playlists.Playlist";
    private const string BoxSetType = "MediaBrowser.Controller.Entities.Movies.BoxSet";

    private readonly ILogger<RepairPlaylistsAndCollections> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly ILibraryManager _libraryManager;
    private readonly IServerApplicationHost _appHost;
    private readonly IServerApplicationPaths _appPaths;

    public RepairPlaylistsAndCollections(
        ILoggerFactory loggerFactory,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        ILibraryManager libraryManager,
        IServerApplicationHost appHost,
        IServerApplicationPaths appPaths)
    {
        _logger = loggerFactory.CreateLogger<RepairPlaylistsAndCollections>();
        _dbProvider = dbProvider;
        _libraryManager = libraryManager;
        _appHost = appHost;
        _appPaths = appPaths;
    }

    /// <inheritdoc />
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var playlistsPath = Path.Combine(_appPaths.DataPath, "playlists");
        var collectionsPath = Path.Combine(_appPaths.DataPath, "collections");

        var context = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            await RepairRowsAsync(context, playlistsPath, collectionsPath, cancellationToken).ConfigureAwait(false);
            await RestoreMissingPlaylistsAsync(context, playlistsPath, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Brings the columns the library-media cleanups key off back to what a playlist or a collection
    /// is supposed to look like. A row that keeps a stale OwnerId survives those cleanups now, but
    /// every item query drops items that are owned and not an extra, so it stays invisible.
    /// </summary>
    private async Task RepairRowsAsync(
        JellyfinDbContext context,
        string playlistsPath,
        string collectionsPath,
        CancellationToken cancellationToken)
    {
#pragma warning disable RS0030 // Do not use banned APIs
        var containers = await context.BaseItems
            .Where(b => b.IsFolder && b.Path != null)
            .Select(b => new { b.Id, b.Path })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = await context.BaseItems
            .Where(b => b.Type == PlaylistType || b.Type == BoxSetType)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingIds = await context.BaseItems
            .Select(b => b.Id)
            .ToHashSetAsync(cancellationToken)
            .ConfigureAwait(false);
#pragma warning restore RS0030 // Do not use banned APIs

        var containerIdByPath = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var container in containers)
        {
            containerIdByPath.TryAdd(_appHost.ExpandVirtualPath(container.Path!), container.Id);
        }

        containerIdByPath.TryGetValue(playlistsPath, out var playlistsFolderId);
        containerIdByPath.TryGetValue(collectionsPath, out var collectionsFolderId);

        var repaired = 0;
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var changed = false;

            // Neither type is ever owned by another item or an extra of one.
            if (item.OwnerId.HasValue)
            {
                item.OwnerId = null;
                changed = true;
            }

            if (item.ExtraType.HasValue)
            {
                item.ExtraType = null;
                changed = true;
            }

            if (!item.IsFolder)
            {
                item.IsFolder = true;
                changed = true;
            }

            var path = item.Path is null ? null : _appHost.ExpandVirtualPath(item.Path);
            if (item.IsVirtualItem && path is not null && Directory.Exists(path))
            {
                item.IsVirtualItem = false;
                changed = true;
            }

            // TopParentId carries no foreign key, so one left pointing at a deleted row stays that
            // way and hides the item from every query scoped to a top level folder.
            var containerId = item.Type == PlaylistType ? playlistsFolderId : collectionsFolderId;
            if (!containerId.IsEmpty()
                && path is not null
                && string.Equals(Path.GetDirectoryName(path), item.Type == PlaylistType ? playlistsPath : collectionsPath, StringComparison.OrdinalIgnoreCase))
            {
                if (!item.ParentId.HasValue || !existingIds.Contains(item.ParentId.Value))
                {
                    item.ParentId = containerId;
                    changed = true;
                }

                if (!item.TopParentId.HasValue || !existingIds.Contains(item.TopParentId.Value))
                {
                    item.TopParentId = containerId;
                    changed = true;
                }
            }

            if (changed)
            {
                repaired++;
            }
        }

        if (repaired == 0)
        {
            _logger.LogInformation("No playlists or collections need repairing.");
            return;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Repaired {Count} playlists and collections.", repaired);
    }

    private async Task RestoreMissingPlaylistsAsync(
        JellyfinDbContext context,
        string playlistsPath,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(playlistsPath))
        {
            return;
        }

        var playlistsFolderId = _libraryManager.GetNewItemId(playlistsPath, typeof(PlaylistsFolder));
        if (_libraryManager.GetItemById(playlistsFolderId) is not Folder playlistsFolder)
        {
            // Without that folder there is nothing to parent a playlist to, and the next scan
            // recreates both anyway.
            _logger.LogInformation("No playlists folder in the library, skipping playlist recovery.");
            return;
        }

        var idByPath = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
#pragma warning disable RS0030 // Do not use banned APIs
        var storedItems = await context.BaseItems
            .Where(b => b.Path != null)
            .Select(b => new { b.Id, b.Path })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var storedIds = await context.BaseItems
            .Select(b => b.Id)
            .ToHashSetAsync(cancellationToken)
            .ConfigureAwait(false);
#pragma warning restore RS0030 // Do not use banned APIs

        foreach (var item in storedItems)
        {
            idByPath.TryAdd(_appHost.ExpandVirtualPath(item.Path!), item.Id);
        }

        // A playlist whose item is still there under a path that no longer matches its folder is not
        // missing, and recreating it would collide with the row that already holds that id.
        var missing = Directory.EnumerateDirectories(playlistsPath)
            .Where(dir => !idByPath.ContainsKey(dir)
                && !storedIds.Contains(_libraryManager.GetNewItemId(dir, typeof(Playlist))))
            .OrderBy(dir => dir, StringComparer.Ordinal)
            .ToList();

        if (missing.Count == 0)
        {
            _logger.LogInformation("Every playlist folder still has an item, nothing to restore.");
            return;
        }

        _logger.LogInformation("Found {Count} playlist folders without an item, restoring them.", missing.Count);

        var restoredEntries = 0;
        foreach (var dir in missing)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var playlist = new Playlist
            {
                Path = dir,
                Name = Path.GetFileName(dir),
                // Ownership is not recorded on disk, so the playlist comes back shared, which is what
                // a library scan would have recreated it as.
                OpenAccess = true,
                Id = _libraryManager.GetNewItemId(dir, typeof(Playlist)),
                DateCreated = Directory.GetCreationTimeUtc(dir),
                DateModified = Directory.GetLastWriteTimeUtc(dir)
            };

            playlist.SetMediaType(MediaType.Audio);

            // CreateItem does not walk the hierarchy, so ParentId and TopParentId only get written if
            // the item already knows its parent.
            playlist.SetParent(playlistsFolder);
            playlist.PresentationUniqueKey = playlist.CreatePresentationUniqueKey();
            _libraryManager.CreateItem(playlist, playlistsFolder);

            var metadataPath = Path.Combine(dir, PlaylistFileName);
            var sortOrder = 0;
            foreach (var storedPath in File.Exists(metadataPath) ? ReadEntryPaths(metadataPath) : [])
            {
                if (!idByPath.TryGetValue(storedPath, out var childId))
                {
                    continue;
                }

                context.LinkedChildren.Add(new LinkedChildEntity
                {
                    ParentId = playlist.Id,
                    ChildId = childId,
                    ChildType = LinkedChildType.Manual,
                    SortOrder = sortOrder
                });

                sortOrder++;
            }

            restoredEntries += sortOrder;
            _logger.LogInformation("Restored playlist {Name} with {Count} entries.", playlist.Name, sortOrder);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Restored {PlaylistCount} playlists holding {EntryCount} entries.",
            missing.Count,
            restoredEntries);
    }

    private List<string> ReadEntryPaths(string metadataPath)
    {
        var paths = new List<string>();
        var settings = new XmlReaderSettings
        {
            IgnoreComments = true,
            IgnoreWhitespace = true,
            IgnoreProcessingInstructions = true,
            DtdProcessing = DtdProcessing.Prohibit
        };

        try
        {
            using var reader = XmlReader.Create(metadataPath, settings);
            var inEntry = false;
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                if (string.Equals(reader.Name, "PlaylistItem", StringComparison.Ordinal))
                {
                    inEntry = true;
                }
                else if (inEntry && string.Equals(reader.Name, "Path", StringComparison.Ordinal))
                {
                    inEntry = false;
                    var value = reader.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        paths.Add(value.Trim());
                    }
                }
            }
        }
        catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not read playlist metadata {MetadataPath}.", metadataPath);
        }

        return paths;
    }
}
