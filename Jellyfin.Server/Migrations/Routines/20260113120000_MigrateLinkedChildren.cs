using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
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
    private const int ParseProgressLogStep = 25_000;
    private const int FileCheckProgressLogStep = 10_000;
    private const int ResolveProgressLogStep = 10_000;
    private const int DeleteProgressLogStep = 25;

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
            .Select(b => new { b.Id, b.Data, b.Type, b.Path, b.IsFolder })
            .ToList();

        _logger.LogInformation("Found {Count} potential items with LinkedChildren data to process.", itemsWithData.Count);

        var pathToIdMap = context.BaseItems
            .Where(b => b.Path != null)
            .Select(b => new { b.Id, b.Path })
            .GroupBy(b => b.Path!)
            .ToDictionary(g => g.Key, g => g.First().Id);

        // Needed to tell a stale cached ItemId apart from one that still points at a real item.
        var allItemIds = context.BaseItems.Select(b => b.Id).ToHashSet();

        var playlistParentIds = itemsWithData
            .Where(b => b.Type == "MediaBrowser.Controller.Playlists.Playlist")
            .Select(b => b.Id)
            .ToHashSet();

        var droppedChildren = 0;
        var linkedChildrenToAdd = new List<LinkedChildEntity>();
        var processedCount = 0;
        var totalItems = itemsWithData.Count;

        foreach (var item in itemsWithData)
        {
            if (string.IsNullOrEmpty(item.Data))
            {
                continue;
            }

            if (processedCount > 0 && processedCount % ParseProgressLogStep == 0)
            {
                _logger.LogInformation("Processing LinkedChildren: {Processed}/{Total} items", processedCount, totalItems);
            }

            try
            {
                using var doc = JsonDocument.Parse(item.Data);

                var isVideo = videoTypes.Contains(item.Type);

                // Handle Video alternate versions
                if (isVideo)
                {
                    ProcessVideoAlternateVersions(doc.RootElement, item.Id, pathToIdMap, allItemIds, linkedChildrenToAdd);
                }

                // Handle LinkedChildren (for containers and other items)
                if (!doc.RootElement.TryGetProperty("LinkedChildren", out var linkedChildrenElement) || linkedChildrenElement.ValueKind != JsonValueKind.Array)
                {
                    processedCount++;
                    continue;
                }

                // Legacy entries may hold a path relative to the container that holds them, so the
                // container's own location has to be a real path, not a virtual one.
                var itemPath = item.Path is null ? null : _appHost.ExpandVirtualPath(item.Path);
                var containingFolderPath = item.IsFolder ? itemPath : Path.GetDirectoryName(itemPath);
                var sortOrder = 0;
                foreach (var childElement in linkedChildrenElement.EnumerateArray())
                {
                    var childId = ResolveChildId(childElement, containingFolderPath, pathToIdMap, allItemIds);
                    if (!childId.HasValue)
                    {
                        droppedChildren++;
                        _logger.LogWarning(
                            "Dropping unresolvable LinkedChild of {ParentId}: ItemId {ItemId}, path {ChildPath}",
                            item.Id,
                            GetStringProperty(childElement, "ItemId") ?? "none",
                            GetStringProperty(childElement, "Path") ?? "none");
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
                        SortOrder = sortOrder
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

            // A playlist may list the same child more than once, so it cannot be keyed by
            // (ParentId, ChildId): skip a playlist wholesale if it already has rows instead, which
            // keeps the routine re-runnable without collapsing repeated entries.
            var populatedParentIds = context.LinkedChildren
                .Select(lc => lc.ParentId)
                .Distinct()
                .ToHashSet();

            var toInsert = linkedChildrenToAdd
                .Where(lc => playlistParentIds.Contains(lc.ParentId)
                    ? !populatedParentIds.Contains(lc.ParentId)
                    : !existingKeys.Contains(new { lc.ParentId, lc.ChildId }))
                .ToList();

            if (toInsert.Count > 0)
            {
                // Every container type other than a playlist keeps a single entry per child.
                // Priority: LocalAlternateVersion > LinkedAlternateVersion > Other
                toInsert =
                [
                    .. toInsert.Where(lc => playlistParentIds.Contains(lc.ParentId)),
                    .. toInsert
                        .Where(lc => !playlistParentIds.Contains(lc.ParentId))
                        .OrderBy(lc => lc.ChildType switch
                        {
                            LinkedChildType.LocalAlternateVersion => 0,
                            LinkedChildType.LinkedAlternateVersion => 1,
                            _ => 2
                        })
                        .DistinctBy(lc => new { lc.ParentId, lc.ChildId })
                ];

                var childIds = toInsert.Select(lc => lc.ChildId).Distinct().ToList();
                var existingChildIds = context.BaseItems
                    .WhereOneOrMany(childIds, b => b.Id)
                    .Select(b => b.Id)
                    .ToHashSet();

                toInsert = toInsert.Where(lc => existingChildIds.Contains(lc.ChildId)).ToList();

                // Drop linked (user-merged) entries that point at items the parent owns (local
                // file-based alternates or extras). These stem from legacy data that merged an
                // owned item onto its own primary and would wrongly mark server-merged groups
                // as user-merged (splittable).
                var linkedChildIds = toInsert
                    .Where(lc => lc.ChildType == LinkedChildType.LinkedAlternateVersion)
                    .Select(lc => lc.ChildId)
                    .Distinct()
                    .ToList();

                if (linkedChildIds.Count > 0)
                {
                    var ownerIdByChildId = context.BaseItems
                        .WhereOneOrMany(linkedChildIds, b => b.Id)
                        .Where(b => b.OwnerId.HasValue)
                        .Select(b => new { b.Id, b.OwnerId })
                        .ToDictionary(b => b.Id, b => b.OwnerId!.Value);

                    var removedCount = toInsert.RemoveAll(lc =>
                        lc.ChildType == LinkedChildType.LinkedAlternateVersion
                        && ownerIdByChildId.TryGetValue(lc.ChildId, out var ownerId)
                        && ownerId.Equals(lc.ParentId));

                    if (removedCount > 0)
                    {
                        _logger.LogInformation("Skipped {Count} LinkedAlternateVersion records pointing at items owned by their parent.", removedCount);
                    }
                }

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

        _logger.LogInformation(
            "LinkedChildren migration completed. Processed {Count} items, dropped {DroppedCount} unresolvable children.",
            processedCount,
            droppedChildren);

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

        var deleted = ResolveAndDeleteItems(wrongTypeChildIds, "wrong-type alternate version items");

        _logger.LogInformation("Removed {Count} wrong-type alternate version items. They will be recreated with the correct type on next library scan.", deleted);
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

        var deleted = ResolveAndDeleteItems(orphanedVersionIds, "orphaned alternate version BaseItems");

        _logger.LogInformation("Removed {Count} orphaned alternate version BaseItems.", deleted);
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

        var deleted = ResolveAndDeleteItems(orphanedIds, "items from deleted libraries");

        _logger.LogInformation("Removed {Count} items from deleted libraries.", deleted);
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

        // An item outside every library location is normally left over from a removed media path, but
        // it looks exactly the same as one whose storage failed to mount (a wrong bind mount on the
        // first container start, for example). Only act on it while every location is readable.
        var canRemoveUnrootedItems = inaccessiblePaths.Count == 0;
        var skippedUnrootedItems = 0;

        var staleIds = new List<Guid>();
        var checkedCount = 0;
        _logger.LogInformation("Checking {Total} items for missing files.", itemsWithPaths.Count);

        foreach (var item in itemsWithPaths)
        {
            // A miss on offline storage can block for the mount timeout, so report while scanning.
            if (checkedCount > 0 && checkedCount % FileCheckProgressLogStep == 0)
            {
                _logger.LogInformation(
                    "Checking for missing files: {Checked}/{Total} items, {Stale} stale so far.",
                    checkedCount,
                    itemsWithPaths.Count,
                    staleIds.Count);
            }

            checkedCount++;

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
                    _logger.LogDebug("Removing item {ItemId}: file {Path} no longer exists.", item.Id, path);
                    staleIds.Add(item.Id);
                }
            }
            else if (!allLibraryPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                // Item is not under ANY library location (accessible or not) —
                // it's orphaned from all libraries (e.g. media path was removed from config)
                if (canRemoveUnrootedItems)
                {
                    _logger.LogDebug("Removing item {ItemId}: path {Path} is outside every library location.", item.Id, path);
                    staleIds.Add(item.Id);
                }
                else
                {
                    skippedUnrootedItems++;
                }
            }

            // Otherwise: item is under an inaccessible location — skip (storage may be offline)
        }

        if (skippedUnrootedItems > 0)
        {
            _logger.LogWarning(
                "Keeping {Count} items that are outside every library location because {LocationCount} library location(s) are currently unavailable.",
                skippedUnrootedItems,
                inaccessiblePaths.Count);
        }

        if (staleIds.Count == 0)
        {
            _logger.LogInformation("No stale items found.");
            return;
        }

        _logger.LogInformation("Found {Count} stale items to remove.", staleIds.Count);

        var deleted = ResolveAndDeleteItems(staleIds, "items with missing files");

        _logger.LogInformation("Removed {Count} stale items.", deleted);
    }

    private int ResolveAndDeleteItems(IReadOnlyCollection<Guid> ids, string description)
    {
        if (ids.Count == 0)
        {
            return 0;
        }

        return DeleteItems(ResolveItems(ids, description), description);
    }

    private List<BaseItem> ResolveItems(IReadOnlyCollection<Guid> ids, string description)
    {
        // Each lookup is a separate repository read; cached ones are fast, so this only reports
        // once a set is large enough for the reads to add up to a noticeable stretch.
        var items = new List<BaseItem>(ids.Count);
        var processed = 0;
        foreach (var id in ids)
        {
            if (processed > 0 && processed % ResolveProgressLogStep == 0)
            {
                _logger.LogInformation("Loading {Description}: {Processed}/{Total} items", description, processed, ids.Count);
            }

            processed++;

            var item = _libraryManager.GetItemById(id);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    private int DeleteItems(IReadOnlyCollection<BaseItem> items, string description)
    {
        if (items.Count == 0)
        {
            return 0;
        }

        var options = new DeleteOptions { DeleteFileLocation = false, DeleteFromExternalProvider = false };
        var deleted = 0;
        var processed = 0;
        foreach (var item in items)
        {
            if (processed > 0 && processed % DeleteProgressLogStep == 0)
            {
                _logger.LogInformation("Removing {Description}: {Processed}/{Total} items", description, processed, items.Count);
            }

            processed++;

            try
            {
                _libraryManager.DeleteItem(item, options);
                deleted++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping item {ItemId} ({ItemName}): delete failed.", item.Id, item.Name ?? "Unknown");
            }
        }

        return deleted;
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

        // Remove all orphaned records. Both queries can return the same row, and a playlist may hold
        // several rows for one child, so the position is what identifies an entry here.
        var distinctOrphaned = orphanedLinkedChildren.DistinctBy(lc => new { lc.ParentId, lc.SortOrder }).ToList();
        context.LinkedChildren.RemoveRange(distinctOrphaned);
        context.SaveChanges();

        _logger.LogInformation("Successfully removed {Count} orphaned LinkedChildren records.", distinctOrphaned.Count);
    }

    /// <summary>
    /// Resolves the item a legacy LinkedChild entry points at.
    /// </summary>
    private static Guid? ResolveChildId(
        JsonElement childElement,
        string? containingFolderPath,
        Dictionary<string, Guid> pathToIdMap,
        HashSet<Guid> allItemIds)
    {
        // Pre-12 data only cached ItemId and re-resolved it from the path whenever the cached value
        // went stale (BaseItem.GetLinkedChild in 10.x). An id that no longer exists must therefore
        // fall through to the path, or the entry is lost even though its file is still in the library.
        if (TryGetGuidProperty(childElement, "ItemId", out var itemId) && allItemIds.Contains(itemId))
        {
            return itemId;
        }

        var path = GetStringProperty(childElement, "Path");
        if (!string.IsNullOrEmpty(path))
        {
            if (pathToIdMap.TryGetValue(path, out var idByPath))
            {
                return idByPath;
            }

            // 10.x resolved entries relative to the container that holds them.
            if (!Path.IsPathRooted(path) && !string.IsNullOrEmpty(containingFolderPath))
            {
                string? absolutePath = null;
                try
                {
                    absolutePath = Path.GetFullPath(Path.Combine(containingFolderPath, path));
                }
                catch (ArgumentException)
                {
                    // Malformed path, nothing to resolve.
                }

                if (absolutePath is not null && pathToIdMap.TryGetValue(absolutePath, out var idByAbsolutePath))
                {
                    return idByAbsolutePath;
                }
            }
        }

        if (TryGetGuidProperty(childElement, "LibraryItemId", out var libraryItemId) && allItemIds.Contains(libraryItemId))
        {
            return libraryItemId;
        }

        return null;
    }

    private static string? GetStringProperty(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static bool TryGetGuidProperty(JsonElement element, string propertyName, out Guid value)
    {
        value = Guid.Empty;
        var raw = GetStringProperty(element, propertyName);

        return !string.IsNullOrEmpty(raw) && Guid.TryParse(raw, out value) && !value.IsEmpty();
    }

    private void ProcessVideoAlternateVersions(
        JsonElement root,
        Guid parentId,
        Dictionary<string, Guid> pathToIdMap,
        HashSet<Guid> allItemIds,
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
                var childId = ResolveChildId(linkedChildElement, null, pathToIdMap, allItemIds);
                if (!childId.HasValue)
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
