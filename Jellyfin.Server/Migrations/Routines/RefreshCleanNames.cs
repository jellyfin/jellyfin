using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Jellyfin.Database.Implementations;
using Jellyfin.Extensions;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Migration to refresh CleanName values for all library items.
/// </summary>
[JellyfinMigration("2025-10-08T12:00:00", nameof(RefreshCleanNames))]
public class RefreshCleanNames : IDatabaseMigrationRoutine
{
    private readonly IStartupLogger<RefreshCleanNames> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshCleanNames"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbProvider">Instance of the <see cref="IDbContextFactory{JellyfinDbContext}"/> interface.</param>
    public RefreshCleanNames(
        IStartupLogger<RefreshCleanNames> logger,
        IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _logger = logger;
        _dbProvider = dbProvider;
    }

    /// <inheritdoc />
    public void Perform()
    {
        const int batchSize = 1000;
        int itemCount = 0, offset = 0;

        var sw = Stopwatch.StartNew();

        using var context = _dbProvider.CreateDbContext();
        var totalRecords = context.BaseItems.Count(b => !string.IsNullOrEmpty(b.Name));
        _logger.LogInformation("Refreshing CleanName for {Count} library items", totalRecords);

        do
        {
            var batch = context.BaseItems
                .Where(b => !string.IsNullOrEmpty(b.Name))
                .OrderBy(e => e.Id)
                .Skip(offset)
                .Take(batchSize)
                .ToList();

            if (batch.Count == 0)
            {
                break;
            }

            foreach (var item in batch)
            {
                try
                {
                    var newCleanName = GetCleanValue(item.Name);
                    if (newCleanName != item.CleanName)
                    {
                        _logger.LogDebug(
                            "Updating CleanName for item {Id}: '{OldValue}' -> '{NewValue}'",
                            item.Id,
                            item.CleanName,
                            newCleanName);
                        item.CleanName = newCleanName;
                        itemCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update CleanName for item {Id} ({Name})", item.Id, item.Name);
                }
            }

            context.SaveChanges();
            offset += batch.Count;

            _logger.LogInformation(
                "Processed: {Offset}/{Total} - Updated: {UpdatedCount} - Time: {Elapsed}",
                offset,
                totalRecords,
                itemCount,
                sw.Elapsed);
        } while (offset < totalRecords);

        _logger.LogInformation(
            "Refreshed CleanName for {UpdatedCount} out of {TotalCount} items in {Time}",
            itemCount,
            totalRecords,
            sw.Elapsed);
    }

    /// <summary>
    /// Gets the clean value for search and sorting purposes.
    /// This is a copy of the GetCleanValue logic from BaseItemRepository.
    /// </summary>
    /// <param name="value">The value to clean.</param>
    /// <returns>The cleaned value.</returns>
    private static string? GetCleanValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var noDiacritics = value.RemoveDiacritics();

        // Build a string where any punctuation or symbol is treated as a separator (space).
        var sb = new StringBuilder(noDiacritics.Length);
        var previousWasSpace = false;
        foreach (var ch in noDiacritics)
        {
            char outCh;
            if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
            {
                outCh = ch;
            }
            else
            {
                outCh = ' ';
            }

            // normalize any whitespace character to a single ASCII space.
            if (char.IsWhiteSpace(outCh))
            {
                if (!previousWasSpace)
                {
                    sb.Append(' ');
                    previousWasSpace = true;
                }
            }
            else
            {
                sb.Append(outCh);
                previousWasSpace = false;
            }
        }

        // trim leading/trailing spaces that may have been added.
        var collapsed = sb.ToString().Trim();
        return collapsed.ToLowerInvariant();
    }
}
