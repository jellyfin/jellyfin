using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
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

    public MigrateLinkedChildren(
        ILoggerFactory loggerFactory,
        IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _logger = loggerFactory.CreateLogger<MigrateLinkedChildren>();
        _dbProvider = dbProvider;
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
            "MediaBrowser.Controller.Entities.Movies.Movie"
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

                var isVideo = item.Type == "MediaBrowser.Controller.Entities.Video" || item.Type == "MediaBrowser.Controller.Entities.Movies.Movie";

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

        UpdateAlternateVersionTypes(context);
        CleanupOrphanedLinkedChildren(context);
    }

    private void UpdateAlternateVersionTypes(JellyfinDbContext context)
    {
        _logger.LogInformation("Updating alternate version item types to match their parent's type...");

        // Find all LocalAlternateVersion relationships where the child is a generic Video
        // but the parent is a more specific type (like Movie)
        var genericVideoType = "MediaBrowser.Controller.Entities.Video";

        var alternateVersionsToUpdate = context.LinkedChildren
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
                (x, child) => new { x.ChildId, x.ParentType, ChildType = child.Type, Child = child })
            .Where(x => x.ChildType == genericVideoType && x.ParentType != genericVideoType)
            .ToList();

        if (alternateVersionsToUpdate.Count == 0)
        {
            _logger.LogInformation("No alternate version items need type updates.");
            return;
        }

        _logger.LogInformation("Found {Count} alternate version items to update.", alternateVersionsToUpdate.Count);

        foreach (var item in alternateVersionsToUpdate)
        {
            item.Child.Type = item.ParentType;
            _logger.LogDebug(
                "Updating item {ChildId} type from {OldType} to {NewType}",
                item.ChildId,
                genericVideoType,
                item.ParentType);
        }

        context.SaveChanges();
        _logger.LogInformation("Successfully updated {Count} alternate version item types.", alternateVersionsToUpdate.Count);
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
                        SortOrder = null
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
                    SortOrder = null
                });

                _logger.LogDebug(
                    "Migrating LinkedAlternateVersion: Parent={ParentId}, Child={ChildId}",
                    parentId,
                    childId.Value);
            }
        }
    }
}
