using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migrates LinkedChildren data from JSON Data column to the LinkedChildren table.
/// </summary>
[JellyfinMigration("2026-01-13T12:00:00", nameof(MigrateLinkedChildren))]
[JellyfinMigrationBackup(JellyfinDb = true)]
internal class MigrateLinkedChildren : IDatabaseMigrationRoutine
{
    private readonly ILogger<MigrateLinkedChildren> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly ILibraryManager _libraryManager;
    private readonly IServerApplicationHost _appHost;
    private readonly IServerApplicationPaths _appPaths;

    public MigrateLinkedChildren(
        ILoggerFactory loggerFactory,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        ILibraryManager libraryManager,
        IServerApplicationHost appHost,
        IServerApplicationPaths appPaths)
    {
        _logger = loggerFactory.CreateLogger<MigrateLinkedChildren>();
        _dbProvider = dbProvider;
        _libraryManager = libraryManager;
        _appHost = appHost;
        _appPaths = appPaths;
    }

    /// <inheritdoc/>
    public void Perform()
    {
        using var context = _dbProvider.CreateDbContext();

        var containerTypes = new[]
        {
            "MediaBrowser.Controller.Entities.Movies.BoxSet",
            "MediaBrowser.Controller.Playlists.Playlist",
            "MediaBrowser.Controller.Entities.CollectionFolder"
        };

        var videoTypes = new[]
        {
            "MediaBrowser.Controller.Entities.Video",
            "MediaBrowser.Controller.Entities.Movies.Movie",
            "MediaBrowser.Controller.Entities.TV.Episode"
        };

        var itemsWithData = context.BaseItems
            .Where(b => b.Data != null && (containerTypes.Contains(b.Type) || videoTypes.Contains(b.Type)))
            .Select(b => new { b.Id, b.Data, b.Type })
            .ToList();

        _logger.LogInformation("Found {Count} potential items with LinkedChildren data to process.", itemsWithData.Count);

        var pathToIdMap = context.BaseItems
            .Where(b => b.Path != null)
            .Select(b => new { b.Id, b.Path })
            .GroupBy(b => b.Path!)
            .ToDictionary(g => g.Key, g => g.First().Id);

        var linkedChildrenToAdd = new List<LinkedChildEntity>();
        var processedCount = 0;

        foreach (var item in itemsWithData)
        {
            if (string.IsNullOrEmpty(item.Data))
            {
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(item.Data);

                var isVideo = videoTypes.Contains(item.Type);

                // Handle Video alternate versions
                if (isVideo)
                {
                    ProcessVideoAlternateVersions(doc.RootElement, item.Id, pathToIdMap, linkedChildrenToAdd);
                }

                // Handle LinkedChildren (for containers and other items)
                if (!doc.RootElement.TryGetProperty("LinkedChildren", out var linkedChildrenElement) || linkedChildrenElement.ValueKind != JsonValueKind.Array)
                {
                    processedCount++;
                    continue;
                }

                var isPlaylist = item.Type == "MediaBrowser.Controller.Playlists.Playlist";
                var sortOrder = 0;
                foreach (var childElement in linkedChildrenElement.EnumerateArray())
                {
                    Guid? childId = null;
                    if (childElement.TryGetProperty("ItemId", out var itemIdProp) && itemIdProp.ValueKind != JsonValueKind.Null)
                    {
                        var itemIdStr = itemIdProp.GetString();
                        if (!string.IsNullOrEmpty(itemIdStr) && Guid.TryParse(itemIdStr, out var parsedId))
                        {
                            childId = parsedId;
                        }
                    }

                    if (!childId.HasValue || childId.Value.IsEmpty())
                    {
                        if (childElement.TryGetProperty("Path", out var pathProp))
                        {
                            var path = pathProp.GetString();
                            if (!string.IsNullOrEmpty(path) && pathToIdMap.TryGetValue(path, out var resolvedId))
                            {
                                childId = resolvedId;
                            }
                        }
                    }

                    if (!childId.HasValue || childId.Value.IsEmpty())
                    {
                        if (childElement.TryGetProperty("LibraryItemId", out var libIdProp))
                        {
                            var libIdStr = libIdProp.GetString();
                            if (!string.IsNullOrEmpty(libIdStr) && Guid.TryParse(libIdStr, out var parsedLibId))
                            {
                                childId = parsedLibId;
                            }
                        }
                    }

                    if (!childId.HasValue || childId.Value.IsEmpty())
                    {
                        continue;
                    }

                    var childType = LinkedChildType.Manual;
                    if (childElement.TryGetProperty("Type", out var typeProp))
                    {
                        if (typeProp.ValueKind == JsonValueKind.Number)
                        {
                            childType = (LinkedChildType)typeProp.GetInt32();
                        }
                        else if (typeProp.ValueKind == JsonValueKind.String)
                        {
                            var typeStr = typeProp.GetString();
                            if (Enum.TryParse<LinkedChildType>(typeStr, out var parsedType))
                            {
                                childType = parsedType;
                            }
                        }
                    }

                    linkedChildrenToAdd.Add(new LinkedChildEntity
                    {
                        ParentId = item.Id,
                        ChildId = childId.Value,
                        ChildType = childType,
                        SortOrder = isPlaylist ? sortOrder : null
                    });

                    sortOrder++;
                }

                processedCount++;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse JSON for item {ItemId}", item.Id);
            }
        }

        if (linkedChildrenToAdd.Count > 0)
        {
            _logger.LogInformation("Inserting {Count} LinkedChildren records.", linkedChildrenToAdd.Count);

            var existingKeys = context.LinkedChildren
                .Select(lc => new { lc.ParentId, lc.ChildId })
                .ToHashSet();

            var toInsert = linkedChildrenToAdd
                .Where(lc => !existingKeys.Contains(new { lc.ParentId, lc.ChildId }))
                .ToList();

            if (toInsert.Count > 0)
            {
                // Deduplicate by composite key (ParentId, ChildId)
                // Priority: LocalAlternateVersion > LinkedAlternateVersion > Other
                toInsert = toInsert
                    .OrderBy(lc => lc.ChildType switch
                    {
                        LinkedChildType.LocalAlternateVersion => 0,
                        LinkedChildType.LinkedAlternateVersion => 1,
                        _ => 2
                    })
                    .DistinctBy(lc => new { lc.ParentId, lc.ChildId })
                    .ToList();

                var childIds = toInsert.Select(lc => lc.ChildId).Distinct().ToList();
                var existingChildIds = context.BaseItems
                    .Where(b => childIds.Contains(b.Id))
                    .Select(b => b.Id)
                    .ToHashSet();

                toInsert = toInsert.Where(lc => existingChildIds.Contains(lc.ChildId)).ToList();

                context.LinkedChildren.AddRange(toInsert);
                context.SaveChanges();

                _logger.LogInformation("Successfully inserted {Count} LinkedChildren records.", toInsert.Count);
            }
            else
            {
                _logger.LogInformation("All LinkedChildren records already exist, nothing to insert.");
            }
        }
        else
        {
            _logger.LogInformation("No LinkedChildren data found to migrate.");
        }

        _logger.LogInformation("LinkedChildren migration completed. Processed {Count} items.", processedCount);

        CleanupWrongTypeAlternateVersions(context);
        CleanupOrphanedAlternateVersionBaseItems(context);
        CleanupItemsFromDeletedLibraries(context);
        CleanupStaleFileEntries(context);
        CleanupOrphanedLinkedChildren(context);
    }

    private void CleanupWrongTypeAlternateVersions(JellyfinDbContext context)
    {
        _logger.LogInformation("Cleaning up alternate version items with wrong type...");

        // Find all LocalAlternateVersion relationships where the child is a generic Video
        // but the parent is a more specific type (like Movie).
        // Since IDs are computed from type + path, just updating the Type column would break ID lookups.
        // Instead, delete them and let the runtime recreate them with the correct type during the next library scan.
        var wrongTypeChildIds = context.LinkedChildren
            .Where(lc => lc.ChildType == LinkedChildType.LocalAlternateVersion)
            .Join(
                context.BaseItems,
                lc => lc.ParentId,
                parent => parent.Id,
                (lc, parent) => new { lc.ChildId, ParentType = parent.Type })
            .Join(
                context.BaseItems,
                x => x.ChildId,
                child => child.Id,
                (x, child) => new { x.ChildId, x.ParentType, ChildType = child.Type })
            .Where(x => x.ChildType != x.ParentType)
            .Select(x => x.ChildId)
            .Distinct()
            .ToList();

        if (wrongTypeChildIds.Count == 0)
        {
            _logger.LogInformation("No wrong-type alternate version items found.");
            return;
        }

        _logger.LogInformation("Found {Count} wrong-type alternate version items to remove.", wrongTypeChildIds.Count);

        foreach (var childId in wrongTypeChildIds)
        {
            var item = _libraryManager.GetItemById(childId);
            if (item is not null)
            {
                _libraryManager.DeleteItem(item, new DeleteOptions { DeleteFileLocation = false });
            }
        }

        _logger.LogInformation("Removed {Count} wrong-type alternate version items. They will be recreated with the correct type on next library scan.", wrongTypeChildIds.Count);
    }

    private void CleanupOrphanedAlternateVersionBaseItems(JellyfinDbContext context)
    {
        _logger.LogInformation("Starting cleanup of orphaned alternate version BaseItems...");

        // Find BaseItems that have OwnerId set (they belonged to another item) and are not extras,
        // but no LinkedChild entry references them — meaning they're orphaned alternate versions.
        // This happens when a version file is renamed: the old BaseItem remains in the DB
        // with a stale OwnerId but nothing links to it anymore.
        var orphanedVersionIds = context.BaseItems
            .Where(b => b.OwnerId.HasValue && b.ExtraType == null)
            .Where(b => !context.LinkedChildren.Any(lc => lc.ChildId.Equals(b.Id)))
            .Select(b => b.Id)
            .ToList();

        if (orphanedVersionIds.Count == 0)
        {
            _logger.LogInformation("No orphaned alternate version BaseItems found.");
            return;
        }

        _logger.LogInformation("Found {Count} orphaned alternate version BaseItems to remove.", orphanedVersionIds.Count);

        foreach (var id in orphanedVersionIds)
        {
            var item = _libraryManager.GetItemById(id);
            if (item is not null)
            {
                _libraryManager.DeleteItem(item, new DeleteOptions { DeleteFileLocation = false });
            }
        }

        _logger.LogInformation("Removed {Count} orphaned alternate version BaseItems.", orphanedVersionIds.Count);
    }

    private void CleanupItemsFromDeletedLibraries(JellyfinDbContext context)
    {
        _logger.LogInformation("Starting cleanup of items from deleted libraries...");

        // Find BaseItems whose TopParentId points to a library (collection folder) that no longer exists.
        // This happens when a library is removed but the scan didn't fully clean up all items under it.
        var orphanedIds = context.BaseItems
            .Where(b => b.TopParentId.HasValue)
            .Where(b => !context.BaseItems.Any(lib => lib.Id.Equals(b.TopParentId!.Value)))
            .Select(b => b.Id)
            .ToList();

        if (orphanedIds.Count == 0)
        {
            _logger.LogInformation("No items from deleted libraries found.");
            return;
        }

        _logger.LogInformation("Found {Count} items from deleted libraries to remove.", orphanedIds.Count);

        foreach (var id in orphanedIds)
        {
            var item = _libraryManager.GetItemById(id);
            if (item is not null)
            {
                _libraryManager.DeleteItem(item, new DeleteOptions { DeleteFileLocation = false });
            }
        }

        _logger.LogInformation("Removed {Count} items from deleted libraries.", orphanedIds.Count);
    }

    private void CleanupStaleFileEntries(JellyfinDbContext context)
    {
        _logger.LogInformation("Starting cleanup of items with missing files...");

        // Get all library media locations and partition into accessible vs inaccessible.
        // This mirrors the scanner's safeguard: if a library root is inaccessible
        // (e.g. NAS offline), we skip items under it to avoid false deletions.
        var virtualFolders = _libraryManager.GetVirtualFolders();
        var accessiblePaths = new List<string>();
        var inaccessiblePaths = new List<string>();

        foreach (var folder in virtualFolders)
        {
            foreach (var location in folder.Locations)
            {
                if (Directory.Exists(location) && Directory.EnumerateFileSystemEntries(location).Any())
                {
                    accessiblePaths.Add(location);
                }
                else
                {
                    inaccessiblePaths.Add(location);
                    _logger.LogWarning(
                        "Library location {Path} is inaccessible or empty, skipping file existence checks for items under this path.",
                        location);
                }
            }
        }

        var allLibraryPaths = accessiblePaths.Concat(inaccessiblePaths).ToList();

        // Get all non-folder, non-virtual items with paths from the DB
        var itemsWithPaths = context.BaseItems
            .Where(b => b.Path != null && b.Path != string.Empty)
            .Where(b => !b.IsFolder && !b.IsVirtualItem)
            .Select(b => new { b.Id, b.Path })
            .ToList();

        var internalMetadataPath = _appPaths.InternalMetadataPath;

        var staleIds = new List<Guid>();
        foreach (var item in itemsWithPaths)
        {
            // Expand virtual path placeholders (%AppDataPath%, %MetadataPath%) to real paths
            var path = _appHost.ExpandVirtualPath(item.Path!);

            // Skip items stored under internal metadata (images, subtitles, trickplay, etc.)
            if (path.StartsWith(internalMetadataPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (accessiblePaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                // Item is under an accessible library location — check if it still exists
                // Directory check covers BDMV/DVD items whose Path points to a folder
                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    staleIds.Add(item.Id);
                }
            }
            else if (!allLibraryPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                // Item is not under ANY library location (accessible or not) —
                // it's orphaned from all libraries (e.g. media path was removed from config)
                staleIds.Add(item.Id);
            }

            // Otherwise: item is under an inaccessible location — skip (storage may be offline)
        }

        if (staleIds.Count == 0)
        {
            _logger.LogInformation("No stale items found.");
            return;
        }

        _logger.LogInformation("Found {Count} stale items to remove.", staleIds.Count);

        foreach (var id in staleIds)
        {
            var item = _libraryManager.GetItemById(id);
            if (item is not null)
            {
                _libraryManager.DeleteItem(item, new DeleteOptions { DeleteFileLocation = false });
            }
        }

        _logger.LogInformation("Removed {Count} stale items.", staleIds.Count);
    }

    private void CleanupOrphanedLinkedChildren(JellyfinDbContext context)
    {
        _logger.LogInformation("Starting cleanup of orphaned LinkedChildren records...");

        // Find all LinkedChildren where the ChildId doesn't exist in BaseItems
        var orphanedLinkedChildren = context.LinkedChildren
            .Where(lc => !context.BaseItems.Any(b => b.Id.Equals(lc.ChildId)))
            .ToList();

        if (orphanedLinkedChildren.Count == 0)
        {
            _logger.LogInformation("No orphaned LinkedChildren found.");
            return;
        }

        _logger.LogInformation("Found {Count} orphaned LinkedChildren records to remove.", orphanedLinkedChildren.Count);

        var orphanedByParent = context.LinkedChildren
            .Where(lc => !context.BaseItems.Any(b => b.Id.Equals(lc.ParentId)))
            .ToList();

        if (orphanedByParent.Count > 0)
        {
            _logger.LogInformation("Found {Count} LinkedChildren with non-existent parent.", orphanedByParent.Count);
            orphanedLinkedChildren.AddRange(orphanedByParent);
        }

        // Remove all orphaned records
        var distinctOrphaned = orphanedLinkedChildren.DistinctBy(lc => new { lc.ParentId, lc.ChildId }).ToList();
        context.LinkedChildren.RemoveRange(distinctOrphaned);
        context.SaveChanges();

        _logger.LogInformation("Successfully removed {Count} orphaned LinkedChildren records.", distinctOrphaned.Count);
    }

    private void ProcessVideoAlternateVersions(
        JsonElement root,
        Guid parentId,
        Dictionary<string, Guid> pathToIdMap,
        List<LinkedChildEntity> linkedChildrenToAdd)
    {
        int sortOrder = 0;

        if (root.TryGetProperty("LocalAlternateVersions", out var localAlternateVersionsElement)
            && localAlternateVersionsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var pathElement in localAlternateVersionsElement.EnumerateArray())
            {
                if (pathElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var path = pathElement.GetString();
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                // Try to resolve the path to an ItemId
                if (pathToIdMap.TryGetValue(path, out var childId))
                {
                    linkedChildrenToAdd.Add(new LinkedChildEntity
                    {
                        ParentId = parentId,
                        ChildId = childId,
                        ChildType = LinkedChildType.LocalAlternateVersion,
                        SortOrder = sortOrder++
                    });

                    _logger.LogDebug(
                        "Migrating LocalAlternateVersion: Parent={ParentId}, Child={ChildId}, Path={Path}",
                        parentId,
                        childId,
                        path);
                }
                else
                {
                    _logger.LogWarning(
                        "Could not resolve LocalAlternateVersion path to ItemId: {Path} for parent {ParentId}",
                        path,
                        parentId);
                }
            }
        }

        if (root.TryGetProperty("LinkedAlternateVersions", out var linkedAlternateVersionsElement)
            && linkedAlternateVersionsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var linkedChildElement in linkedAlternateVersionsElement.EnumerateArray())
            {
                Guid? childId = null;

                // Try to get ItemId
                if (linkedChildElement.TryGetProperty("ItemId", out var itemIdProp) && itemIdProp.ValueKind != JsonValueKind.Null)
                {
                    var itemIdStr = itemIdProp.GetString();
                    if (!string.IsNullOrEmpty(itemIdStr) && Guid.TryParse(itemIdStr, out var parsedId))
                    {
                        childId = parsedId;
                    }
                }

                // Try to get from Path if ItemId not available
                if (!childId.HasValue || childId.Value.IsEmpty())
                {
                    if (linkedChildElement.TryGetProperty("Path", out var pathProp))
                    {
                        var path = pathProp.GetString();
                        if (!string.IsNullOrEmpty(path) && pathToIdMap.TryGetValue(path, out var resolvedId))
                        {
                            childId = resolvedId;
                        }
                    }
                }

                // Try LibraryItemId as fallback
                if (!childId.HasValue || childId.Value.IsEmpty())
                {
                    if (linkedChildElement.TryGetProperty("LibraryItemId", out var libIdProp))
                    {
                        var libIdStr = libIdProp.GetString();
                        if (!string.IsNullOrEmpty(libIdStr) && Guid.TryParse(libIdStr, out var parsedLibId))
                        {
                            childId = parsedLibId;
                        }
                    }
                }

                if (!childId.HasValue || childId.Value.IsEmpty())
                {
                    _logger.LogWarning("Could not resolve LinkedAlternateVersion child ID for parent {ParentId}", parentId);
                    continue;
                }

                linkedChildrenToAdd.Add(new LinkedChildEntity
                {
                    ParentId = parentId,
                    ChildId = childId.Value,
                    ChildType = LinkedChildType.LinkedAlternateVersion,
                    SortOrder = sortOrder++
                });

                _logger.LogDebug(
                    "Migrating LinkedAlternateVersion: Parent={ParentId}, Child={ChildId}",
                    parentId,
                    childId.Value);
            }
        }
    }
}
