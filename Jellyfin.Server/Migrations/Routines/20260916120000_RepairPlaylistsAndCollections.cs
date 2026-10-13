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
using Jellyfin.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

            var metadataPath = Path.Combine(dir, PlaylistFileName);
            var metadata = File.Exists(metadataPath) ? ReadMetadata(metadataPath) : new PlaylistMetadata();
            var playlist = new Playlist
            {
                Path = dir,
                // The folder name is sanitised and may carry a numeric suffix; LocalTitle is the real name.
                Name = metadata.Name ?? Path.GetFileName(dir),
                OwnerUserId = metadata.OwnerUserId,
                Shares = metadata.Shares,
                // The public flag is not written to playlist.xml; only a playlist without an owner is
                // opened up, as FixPlaylistOwner did.
                OpenAccess = metadata.OwnerUserId.IsEmpty(),
                Id = _libraryManager.GetNewItemId(dir, typeof(Playlist)),
                DateCreated = Directory.GetCreationTimeUtc(dir),
                DateModified = Directory.GetLastWriteTimeUtc(dir)
            };

            playlist.SetMediaType(metadata.MediaType);

            // The entries must be on the item CreateItem caches: an unassigned LinkedChildren means
            // "unknown", and the first edit through that cached instance would wipe rows added here.
            var entries = new List<LinkedChild>();
            foreach (var storedPath in metadata.EntryPaths)
            {
                if (idByPath.TryGetValue(storedPath, out var childId))
                {
                    entries.Add(new LinkedChild { ItemId = childId, Type = LinkedChildType.Manual });
                }
            }

            playlist.LinkedChildren = [.. entries];

            // CreateItem does not walk the hierarchy, so ParentId and TopParentId only get written if
            // the item already knows its parent.
            playlist.SetParent(playlistsFolder);
            playlist.PresentationUniqueKey = playlist.CreatePresentationUniqueKey();
            _libraryManager.CreateItem(playlist, playlistsFolder);

            restoredEntries += entries.Count;
            _logger.LogInformation("Restored playlist {Name} with {Count} entries.", playlist.Name, entries.Count);
        }

        _logger.LogInformation(
            "Restored {PlaylistCount} playlists holding {EntryCount} entries.",
            missing.Count,
            restoredEntries);
    }

    private PlaylistMetadata ReadMetadata(string metadataPath)
    {
        var metadata = new PlaylistMetadata();
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
            string? parent = null;
            Guid? shareUserId = null;
            var shareCanEdit = false;

            // Reading an element's content already moves to the next node, so only advance otherwise.
            reader.Read();
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.EndElement && string.Equals(reader.Name, "Share", StringComparison.Ordinal))
                {
                    if (shareUserId.HasValue)
                    {
                        metadata.Shares.Add(new PlaylistUserPermissions(shareUserId.Value, shareCanEdit));
                    }

                    parent = null;
                    reader.Read();
                    continue;
                }

                if (reader.NodeType != XmlNodeType.Element || reader.IsEmptyElement)
                {
                    reader.Read();
                    continue;
                }

                switch (reader.Name)
                {
                    case "PlaylistItem":
                        parent = reader.Name;
                        reader.Read();
                        break;
                    case "Share":
                        parent = reader.Name;
                        shareUserId = null;
                        shareCanEdit = false;
                        reader.Read();
                        break;
                    case "Path" when parent == "PlaylistItem":
                        parent = null;
                        var value = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            metadata.EntryPaths.Add(value.Trim());
                        }

                        break;
                    case "UserId" when parent == "Share":
                        shareUserId = Guid.TryParse(reader.ReadElementContentAsString(), out var userId) && !userId.IsEmpty() ? userId : null;
                        break;
                    case "CanEdit" when parent == "Share":
                        shareCanEdit = string.Equals(reader.ReadElementContentAsString().Trim(), "true", StringComparison.OrdinalIgnoreCase);
                        break;
                    case "LocalTitle":
                        var title = reader.ReadElementContentAsString();
                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            metadata.Name = title.Trim();
                        }

                        break;
                    case "OwnerUserId":
                        if (Guid.TryParse(reader.ReadElementContentAsString(), out var ownerId))
                        {
                            metadata.OwnerUserId = ownerId;
                        }

                        break;
                    case "PlaylistMediaType":
                        if (Enum.TryParse<MediaType>(reader.ReadElementContentAsString(), out var mediaType) && mediaType != MediaType.Unknown)
                        {
                            metadata.MediaType = mediaType;
                        }

                        break;
                    default:
                        reader.Read();
                        break;
                }
            }
        }
        catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Could not read playlist metadata {MetadataPath}.", metadataPath);
        }

        return metadata;
    }

    private sealed class PlaylistMetadata
    {
        public string? Name { get; set; }

        public Guid OwnerUserId { get; set; }

        public List<PlaylistUserPermissions> Shares { get; } = [];

        public MediaType MediaType { get; set; } = MediaType.Audio;

        public List<string> EntryPaths { get; } = [];
    }
}
