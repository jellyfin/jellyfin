using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Extensions;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Removes NUL characters from the item keys, provider ids and user data keys stored before they were removed
/// on save, so those rows match the values the application now computes.
/// </summary>
[JellyfinMigration("2026-10-12T12:00:00", nameof(RemoveNulFromItemKeys))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class RemoveNulFromItemKeys : IAsyncMigrationRoutine
{
    private readonly IStartupLogger<RemoveNulFromItemKeys> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveNulFromItemKeys"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    public RemoveNulFromItemKeys(
        IStartupLogger<RemoveNulFromItemKeys> logger,
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
            // NUL is looked for here and not in the query, because SQLite does not reliably match it.
            var items = new List<ItemValues>();
            await foreach (var row in context.BaseItems
                .AsNoTracking()
                .Select(e => new ItemValues(
                    e.Id,
                    e.PresentationUniqueKey,
                    e.SeriesPresentationUniqueKey,
                    e.ExternalId,
                    e.ExternalSeriesId,
                    e.SeriesName))
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                if (row.HasNul())
                {
                    items.Add(row);
                }
            }

            var providers = new List<(Guid ItemId, string ProviderId, string Value)>();
            await foreach (var row in context.BaseItemProviders
                .AsNoTracking()
                .Select(e => new { e.ItemId, e.ProviderId, e.ProviderValue })
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                if (HasNul(row.ProviderValue))
                {
                    providers.Add((row.ItemId, row.ProviderId, row.ProviderValue));
                }
            }

            var userData = new List<UserDataKey>();
            await foreach (var row in context.UserData
                .AsNoTracking()
                .Select(e => new UserDataKey(e.ItemId, e.UserId, e.CustomDataKey, e.LastPlayedDate, e.Played, e.PlayCount))
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                if (HasNul(row.Key))
                {
                    userData.Add(row);
                }
            }

            if (items.Count == 0 && providers.Count == 0 && userData.Count == 0)
            {
                _logger.LogInformation("No NUL characters found in item keys.");
                return;
            }

            _logger.LogInformation(
                "Removing NUL characters from {ItemCount} items, {ProviderCount} provider ids and {UserDataCount} user data keys",
                items.Count,
                providers.Count,
                userData.Count);

            var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                foreach (var item in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var presentationUniqueKey = Clean(item.PresentationUniqueKey);
                    var seriesPresentationUniqueKey = Clean(item.SeriesPresentationUniqueKey);
                    var externalId = Clean(item.ExternalId);
                    var externalSeriesId = Clean(item.ExternalSeriesId);
                    var seriesName = Clean(item.SeriesName);
                    var id = item.Id;

                    await context.BaseItems
                        .Where(e => e.Id.Equals(id))
                        .ExecuteUpdateAsync(
                            s => s
                                .SetProperty(e => e.PresentationUniqueKey, presentationUniqueKey)
                                .SetProperty(e => e.SeriesPresentationUniqueKey, seriesPresentationUniqueKey)
                                .SetProperty(e => e.ExternalId, externalId)
                                .SetProperty(e => e.ExternalSeriesId, externalSeriesId)
                                .SetProperty(e => e.SeriesName, seriesName),
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                foreach (var (itemId, providerId, value) in providers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var cleaned = value.RemoveNullCharacters();
                    var providerRows = context.BaseItemProviders
                        .Where(e => e.ItemId.Equals(itemId) && e.ProviderId == providerId);
                    if (cleaned.Length == 0)
                    {
                        // Saving no longer keeps a provider id that held nothing but NUL characters.
                        await providerRows.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await providerRows
                            .ExecuteUpdateAsync(s => s.SetProperty(e => e.ProviderValue, cleaned), cancellationToken)
                            .ConfigureAwait(false);
                    }
                }

                await CleanUserDataKeysAsync(context, userData, cancellationToken).ConfigureAwait(false);

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static bool HasNul(string? value) => value is not null && value.Contains('\0', StringComparison.Ordinal);

    private static string? Clean(string? value) => value?.RemoveNullCharacters();

    private async Task CleanUserDataKeysAsync(JellyfinDbContext context, List<UserDataKey> userData, CancellationToken cancellationToken)
    {
        var merged = 0;
        foreach (var group in userData.GroupBy(e => (e.ItemId, e.UserId, Key: e.Key.RemoveNullCharacters())))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (itemId, userId, cleanKey) = group.Key;
            var candidates = group.ToList();

            var clean = await context.UserData
                .AsNoTracking()
                .Where(e => e.ItemId.Equals(itemId) && e.UserId.Equals(userId) && e.CustomDataKey == cleanKey)
                .Select(e => new UserDataKey(e.ItemId, e.UserId, e.CustomDataKey, e.LastPlayedDate, e.Played, e.PlayCount))
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (clean is not null)
            {
                candidates.Add(clean);
            }

            // The most recently played row wins; with the same date, the played one, then the one with more
            // plays, then the one stored under the cleaned key. The other rows are dropped whole: combining
            // fields would count plays twice and could bring back a favourite that was removed.
            var winner = candidates
                .OrderByDescending(e => e.LastPlayedDate)
                .ThenByDescending(e => e.Played)
                .ThenByDescending(e => e.PlayCount)
                .ThenBy(e => HasNul(e.Key))
                .ThenBy(e => e.Key, StringComparer.Ordinal)
                .First();

            foreach (var loser in candidates.Where(e => !ReferenceEquals(e, winner)))
            {
                // The keys are not logged: they can hold titles and names that belong to the user.
                _logger.LogDebug(
                    "Merged user data of user {UserId} on item {ItemId}, keeping the row {KeptRow}",
                    userId,
                    itemId,
                    HasNul(winner.Key) ? "stored with NUL characters" : "stored under the cleaned key");
                merged++;

                var loserKey = loser.Key;
                await context.UserData
                    .Where(e => e.ItemId.Equals(itemId) && e.UserId.Equals(userId) && e.CustomDataKey == loserKey)
                    .ExecuteDeleteAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            if (HasNul(winner.Key))
            {
                var winnerKey = winner.Key;
                await context.UserData
                    .Where(e => e.ItemId.Equals(itemId) && e.UserId.Equals(userId) && e.CustomDataKey == winnerKey)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.CustomDataKey, cleanKey), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (merged > 0)
        {
            _logger.LogInformation("Merged {MergedCount} user data rows whose keys became equal once NUL characters were removed", merged);
        }
    }

    private sealed record ItemValues(
        Guid Id,
        string? PresentationUniqueKey,
        string? SeriesPresentationUniqueKey,
        string? ExternalId,
        string? ExternalSeriesId,
        string? SeriesName)
    {
        public bool HasNul() =>
            RemoveNulFromItemKeys.HasNul(PresentationUniqueKey)
            || RemoveNulFromItemKeys.HasNul(SeriesPresentationUniqueKey)
            || RemoveNulFromItemKeys.HasNul(ExternalId)
            || RemoveNulFromItemKeys.HasNul(ExternalSeriesId)
            || RemoveNulFromItemKeys.HasNul(SeriesName);
    }

    private sealed record UserDataKey(Guid ItemId, Guid UserId, string Key, DateTime? LastPlayedDate, bool Played, int PlayCount);
}
