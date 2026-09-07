using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Recomputes the presentation unique key of every series and season so merged series are scoped to their own library.
/// </summary>
[JellyfinMigration("2026-08-21T12:00:00", nameof(RecomputeSeriesPresentationKey))]
[JellyfinMigrationBackup(JellyfinDb = true)]
internal class RecomputeSeriesPresentationKey : IAsyncMigrationRoutine
{
    private readonly IStartupLogger<RecomputeSeriesPresentationKey> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecomputeSeriesPresentationKey"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="dbProvider">The database context factory.</param>
    public RecomputeSeriesPresentationKey(
        IStartupLogger<RecomputeSeriesPresentationKey> logger,
        ILibraryManager libraryManager,
        IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _dbProvider = dbProvider;
    }

    /// <inheritdoc />
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var series = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Series]
        }).OfType<Series>().ToArray();

        _logger.LogInformation("Recomputing presentation unique key for {Count} series", series.Length);

        const int ProgressInterval = 250;
        var sw = Stopwatch.StartNew();
        var newSeriesKeys = new Dictionary<Guid, string>();
        var processed = 0;
        var updated = 0;

        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            foreach (var item in series)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (++processed % ProgressInterval == 0)
                {
                    _logger.LogInformation("Processed {Processed}/{Total} series - Updated: {Updated} - Time: {Elapsed}", processed, series.Length, updated, sw.Elapsed);
                }

                var newKey = item.CreatePresentationUniqueKey();
                newSeriesKeys[item.Id] = newKey;

                if (string.Equals(item.PresentationUniqueKey, newKey, StringComparison.Ordinal))
                {
                    continue;
                }

                // Write only the changed column instead of re-persisting the whole item.
                var id = item.Id;
                await dbContext.BaseItems
                    .Where(e => e.Id.Equals(id))
                    .ExecuteUpdateAsync(e => e.SetProperty(f => f.PresentationUniqueKey, newKey), cancellationToken)
                    .ConfigureAwait(false);

                // Seasons and episodes are matched to their series by SeriesPresentationUniqueKey, so
                // re-point them here instead of waiting for the next scan. Scoped by SeriesId rather than
                // by the old key: that key can be shared by every library holding the series, so matching
                // on it would drag the other libraries' children along.
                await dbContext.BaseItems
                    .Where(e => e.SeriesId.HasValue && e.SeriesId.Value.Equals(id))
                    .ExecuteUpdateAsync(e => e.SetProperty(f => f.SeriesPresentationUniqueKey, newKey), cancellationToken)
                    .ConfigureAwait(false);

                updated++;
            }

            var updatedSeasons = await RecomputeSeasonsAsync(dbContext, newSeriesKeys, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Recomputed presentation unique key for {Updated} of {Count} series and {UpdatedSeasons} seasons in {Elapsed}",
                updated,
                series.Length,
                updatedSeasons,
                sw.Elapsed);
        }
    }

    private async Task<int> RecomputeSeasonsAsync(JellyfinDbContext dbContext, Dictionary<Guid, string> newSeriesKeys, CancellationToken cancellationToken)
    {
        // A season's own key embeds its series' key, so it goes stale with it.
        var seasons = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Season]
        }).OfType<Season>().ToArray();

        var updated = 0;

        foreach (var season in seasons)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Without an index number the season keeps the base key, which carries no series key at all.
            if (!season.IndexNumber.HasValue
                || !newSeriesKeys.TryGetValue(season.SeriesId, out var seriesKey))
            {
                continue;
            }

            // Mirrors Season.CreatePresentationUniqueKey.
            var newKey = seriesKey + "-" + season.IndexNumber.Value.ToString("000", CultureInfo.InvariantCulture);
            if (string.Equals(season.PresentationUniqueKey, newKey, StringComparison.Ordinal))
            {
                continue;
            }

            var id = season.Id;
            await dbContext.BaseItems
                .Where(e => e.Id.Equals(id))
                .ExecuteUpdateAsync(e => e.SetProperty(f => f.PresentationUniqueKey, newKey), cancellationToken)
                .ConfigureAwait(false);

            updated++;
        }

        return updated;
    }
}
