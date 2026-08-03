using System;
using System.Diagnostics;
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
/// Recomputes the presentation unique key for every series so existing items adopt the folder-set-free key format.
/// </summary>
[JellyfinMigration("2026-07-23T12:00:00", nameof(RecomputeSeriesPresentationKey))]
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

                var oldKey = item.PresentationUniqueKey;
                var newKey = item.CreatePresentationUniqueKey();
                if (string.Equals(oldKey, newKey, StringComparison.Ordinal))
                {
                    continue;
                }

                // Write only the changed column instead of re-persisting the whole item.
                var id = item.Id;
                await dbContext.BaseItems
                    .Where(e => e.Id.Equals(id))
                    .ExecuteUpdateAsync(e => e.SetProperty(f => f.PresentationUniqueKey, newKey), cancellationToken)
                    .ConfigureAwait(false);

                // Seasons and episodes cache the series key in SeriesPresentationUniqueKey and are matched
                // to the series by it. Re-point every child still carrying the old key in a single set-based
                // update so they stay attached without waiting for the next scan.
                if (!string.IsNullOrEmpty(oldKey))
                {
                    await dbContext.BaseItems
                        .Where(e => e.SeriesPresentationUniqueKey == oldKey)
                        .ExecuteUpdateAsync(e => e.SetProperty(f => f.SeriesPresentationUniqueKey, newKey), cancellationToken)
                        .ConfigureAwait(false);
                }

                updated++;
            }
        }

        _logger.LogInformation("Recomputed presentation unique key for {Updated} of {Count} series in {Elapsed}", updated, series.Length, sw.Elapsed);
    }
}
