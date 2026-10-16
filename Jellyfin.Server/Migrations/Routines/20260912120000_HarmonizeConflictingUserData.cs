using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Collapses conflicting user data rows so every row an item holds for a user says the same thing.
/// </summary>
[JellyfinMigration("2026-09-12T12:00:00", nameof(HarmonizeConflictingUserData))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class HarmonizeConflictingUserData : IAsyncMigrationRoutine
{
    private const int BatchSize = 500;

    private readonly IStartupLogger<HarmonizeConflictingUserData> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="HarmonizeConflictingUserData"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    public HarmonizeConflictingUserData(
        IStartupLogger<HarmonizeConflictingUserData> logger,
        IDbContextFactory<JellyfinDbContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            var conflicts = await context.UserData
                .Where(e => !e.ItemId.Equals(BaseItemRepository.PlaceholderId))
                .GroupBy(e => new { e.ItemId, e.UserId })
                .Where(g => g.Count() > 1
                    && (g.Min(e => e.PlaybackPositionTicks) != g.Max(e => e.PlaybackPositionTicks)
                        || g.Min(e => e.PlayCount) != g.Max(e => e.PlayCount)
                        || g.Min(e => e.Played ? 1 : 0) != g.Max(e => e.Played ? 1 : 0)
                        || g.Min(e => e.IsFavorite ? 1 : 0) != g.Max(e => e.IsFavorite ? 1 : 0)
                        || g.Min(e => e.LastPlayedDate) != g.Max(e => e.LastPlayedDate)))
                .Select(g => new { g.Key.ItemId, g.Key.UserId })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (conflicts.Count == 0)
            {
                _logger.LogInformation("No conflicting user data found.");
                return;
            }

            var conflictKeys = conflicts.ConvertAll(e => new ConflictKey(e.ItemId, e.UserId));

            _logger.LogInformation("Harmonizing user data for {Count} item/user combinations.", conflictKeys.Count);

            var harmonized = 0;
            foreach (var batch in conflictKeys.Chunk(BatchSize))
            {
                harmonized += await HarmonizeBatchAsync(context, batch, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Updated {Count} user data rows.", harmonized);
        }
    }

    private static async Task<int> HarmonizeBatchAsync(JellyfinDbContext context, ConflictKey[] batch, CancellationToken cancellationToken)
    {
        var itemIds = batch.Select(e => e.ItemId).Distinct().ToArray();
        var wanted = batch.ToHashSet();

        var rows = await context.UserData
            .WhereOneOrMany(itemIds, e => e.ItemId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var updated = 0;
        foreach (var group in rows.GroupBy(e => new ConflictKey(e.ItemId, e.UserId)))
        {
            if (!wanted.Contains(group.Key))
            {
                continue;
            }

            // The most recent play is the state the user last produced; the others are fossils of
            // earlier incarnations of the same item.
            var winner = group
                .OrderByDescending(e => e.LastPlayedDate)
                .ThenByDescending(e => e.PlayCount)
                .ThenByDescending(e => e.PlaybackPositionTicks)
                .First();

            foreach (var row in group)
            {
                if (ReferenceEquals(row, winner))
                {
                    continue;
                }

                row.AudioStreamIndex = winner.AudioStreamIndex;
                row.IsFavorite = winner.IsFavorite;
                row.LastPlayedDate = winner.LastPlayedDate;
                row.Likes = winner.Likes;
                row.PlaybackPositionTicks = winner.PlaybackPositionTicks;
                row.PlayCount = winner.PlayCount;
                row.Played = winner.Played;
                row.Rating = winner.Rating;
                row.SubtitleStreamIndex = winner.SubtitleStreamIndex;
                updated++;
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        context.ChangeTracker.Clear();

        return updated;
    }

    private readonly record struct ConflictKey(Guid ItemId, Guid UserId);
}
