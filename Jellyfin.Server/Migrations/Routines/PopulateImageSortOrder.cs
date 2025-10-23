using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Populate SortOrder for existing images after the AddSortOrderToBaseItemImageInfo migration.
/// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
[JellyfinMigration("2025-10-23T02:30:00", nameof(PopulateImageSortOrder), "A7B3F8E9-4C2D-4E1A-9B5F-6D8E3F2A1C9B")]
internal class PopulateImageSortOrder : IMigrationRoutine
#pragma warning restore CS0618 // Type or member is obsolete
{
    private const int UnknownImagePriority = 999;

    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly ILogger<PopulateImageSortOrder> _logger;

    public PopulateImageSortOrder(
        IDbContextFactory<JellyfinDbContext> dbProvider,
        ILogger<PopulateImageSortOrder> logger)
    {
        _dbProvider = dbProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Perform()
    {
        _logger.LogInformation("Starting SortOrder population for existing images");

        using var context = _dbProvider.CreateDbContext();

        // Get all distinct ItemIds that have images
        var itemIdsWithImages = context.BaseItemImageInfos
            .Select(i => i.ItemId)
            .Distinct()
            .ToList();

        if (itemIdsWithImages.Count == 0)
        {
            _logger.LogInformation("No items with images found, skipping SortOrder population");
            return;
        }

        _logger.LogInformation("Processing {Count} items with images", itemIdsWithImages.Count);

        // Batch processing configuration
        const int batchSize = 500;
        int processedCount = 0;
        int errorCount = 0;

        // Load all base item paths into memory for fast lookup
        var itemPathLookup = context.BaseItems
            .Where(b => itemIdsWithImages.Contains(b.Id))
            .Select(b => new { b.Id, b.Path })
            .ToDictionary(x => x.Id, x => x.Path);

        // Load all images into memory
        var allImages = context.BaseItemImageInfos
            .Where(i => itemIdsWithImages.Contains(i.ItemId))
            .ToList();

        _logger.LogInformation("Loaded {Count} images into memory for processing", allImages.Count);

        // Group images by ItemId
        var imagesByItem = allImages.GroupBy(i => i.ItemId).ToList();

        for (int batchStart = 0; batchStart < imagesByItem.Count; batchStart += batchSize)
        {
            var batch = imagesByItem.Skip(batchStart).Take(batchSize).ToList();

            foreach (var itemImages in batch)
            {
                try
                {
                    var itemId = itemImages.Key;
                    var mediaFileName = itemPathLookup.TryGetValue(itemId, out var path) && !string.IsNullOrEmpty(path)
                        ? Path.GetFileNameWithoutExtension(path)
                        : null;

                    var images = itemImages.ToList();

                    // Group images by type and assign SortOrder per type
                    var imagesByType = images.GroupBy(img => img.ImageType);

                    foreach (var typeGroup in imagesByType)
                    {
                        // Calculate priority and sort order for each image within this type
                        var sortedImages = typeGroup
                            .Select(img => new
                            {
                                Image = img,
                                Priority = GetImageOrderPriority(img.Path, mediaFileName),
                                NumericIndex = GetNumericImageIndex(img.Path)
                            })
                            .OrderBy(x => x.Priority)
                            .ThenBy(x => x.NumericIndex)
                            .ThenBy(x => x.Image.Path, StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        // Update SortOrder for each image within this type (0, 1, 2, ...)
                        for (int i = 0; i < sortedImages.Count; i++)
                        {
                            sortedImages[i].Image.SortOrder = i;
                        }
                    }

                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing images for item {ItemId}", itemImages.Key);
                    errorCount++;
                }
            }

            // Bulk save every batch
            try
            {
                context.SaveChanges();
                _logger.LogInformation("Processed {Count}/{Total} items", processedCount, imagesByItem.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving batch starting at index {BatchStart}", batchStart);
            }
        }

        _logger.LogInformation(
            "SortOrder population completed. Processed: {Processed}, Errors: {Errors}, Total: {Total}",
            processedCount,
            errorCount,
            imagesByItem.Count);
    }

    private static int GetImageOrderPriority(string? path, string? mediaFileName)
    {
        if (string.IsNullOrEmpty(path))
        {
            return UnknownImagePriority;
        }

        var normalizedPath = path.Replace('\\', '/');
        var fileName = Path.GetFileNameWithoutExtension(normalizedPath);
        var fileNameLower = fileName.ToLowerInvariant();

        // Priority 0: {mediaFileName}-fanart (any extension)
        if (!string.IsNullOrEmpty(mediaFileName))
        {
            var expectedName = $"{mediaFileName}-fanart".ToLowerInvariant();
            if (fileNameLower == expectedName)
            {
                return 0;
            }
        }

        // Priority 1: fanart (not in extrafanart folder)
        if (fileNameLower == "fanart" && !normalizedPath.Contains("/extrafanart/", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        // Priority 2: fanart-N (numbered, not in extrafanart)
        if (fileNameLower.StartsWith("fanart-", StringComparison.Ordinal) &&
            !normalizedPath.Contains("/extrafanart/", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        // Priority 3: background or background-N
        if (fileNameLower == "background" || fileNameLower.StartsWith("background-", StringComparison.Ordinal))
        {
            return 3;
        }

        // Priority 4: art or art-N
        if (fileNameLower == "art" || fileNameLower.StartsWith("art-", StringComparison.Ordinal))
        {
            return 4;
        }

        // Priority 5: extrafanart folder
        if (normalizedPath.Contains("/extrafanart/", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith("extrafanart/", StringComparison.OrdinalIgnoreCase))
        {
            return 5;
        }

        // Priority 6: backdrop or backdropN
        if (fileNameLower == "backdrop" || fileNameLower.StartsWith("backdrop", StringComparison.Ordinal))
        {
            return 6;
        }

        // Default: lowest priority
        return UnknownImagePriority;
    }

    private static int GetNumericImageIndex(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return int.MaxValue;
        }

        var normalizedPath = path.Replace('\\', '/');
        var fileName = Path.GetFileNameWithoutExtension(normalizedPath);

        // Try to extract numeric index from various patterns
        // For extrafanart/fanart1.jpg, fanart10.jpg, etc.
        if (fileName.Length > 0)
        {
            int digitStartIndex = -1;
            for (int i = fileName.Length - 1; i >= 0; i--)
            {
                if (char.IsDigit(fileName[i]))
                {
                    digitStartIndex = i;
                }
                else if (digitStartIndex >= 0)
                {
                    break;
                }
            }

            if (digitStartIndex >= 0)
            {
                var numericPart = fileName.Substring(digitStartIndex);
                if (int.TryParse(numericPart, out var index))
                {
                    return index;
                }
            }
        }

        return int.MaxValue;
    }
}
