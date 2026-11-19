#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.EntryPoints;
using Emby.Server.Implementations.Library;
using Emby.Server.Implementations.Localization;
using Jellyfin.Database.Implementations;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks;

/// <summary>
/// Recomputes the stored inherited parental rating scores of every item.
/// </summary>
/// <remarks>
/// The scores are derived from the rating strings of the item and its parents and from the metadata country,
/// both of which can change without the item being saved: a country setting is edited or updated rating tables
/// are shipped. The recompute uses <see cref="BaseItem.OnMetadataChanged"/>, so it matches what a metadata
/// refresh computes (custom rating, parent chain, item and library country), and writes only the two score
/// columns, so no metadata savers run and nothing else of the item is rewritten. Which items need a run is
/// decided by <see cref="RatingsRecomputeHost"/>.
/// </remarks>
public class RecomputeParentalRatingScoresTask : IScheduledTask
{
    /// <summary>
    /// The number of items loaded and saved at a time.
    /// </summary>
    internal const int ChunkSize = 500;

    private readonly ILibraryManager _libraryManager;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly RatingsStateStore _stateStore;
    private readonly ILocalizationManager _localization;
    private readonly ILogger<RecomputeParentalRatingScoresTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecomputeParentalRatingScoresTask"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="dbProvider">The DB context factory.</param>
    /// <param name="stateStore">Instance of the <see cref="RatingsStateStore"/> class.</param>
    /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{RecomputeParentalRatingScoresTask}"/> interface.</param>
    public RecomputeParentalRatingScoresTask(
        ILibraryManager libraryManager,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        RatingsStateStore stateStore,
        ILocalizationManager localization,
        ILogger<RecomputeParentalRatingScoresTask> logger)
    {
        _libraryManager = libraryManager;
        _dbProvider = dbProvider;
        _stateStore = stateStore;
        _localization = localization;
        _logger = logger;
        CachedItemLookup = id => (_libraryManager as LibraryManager)?.TryGetCachedItem(id, out var item) == true ? item : null;
    }

    /// <summary>
    /// Gets or sets how an item is looked up in the library manager's cache without loading it from the database.
    /// </summary>
    internal Func<Guid, BaseItem?> CachedItemLookup { get; set; }

    /// <inheritdoc />
    public string Name => _localization.GetLocalizedString("TaskRecomputeParentalRatingScores");

    /// <inheritdoc />
    public string Description => _localization.GetLocalizedString("TaskRecomputeParentalRatingScoresDescription");

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public string Key => "RecomputeParentalRatingScores";

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        // The state is taken before the first item is read, so a change made while the run is in progress
        // leaves the stored state stale and is picked up by a following run.
        var startState = _stateStore.ComputeCurrent();

        var ids = _libraryManager.GetItemIds(new InternalItemsQuery
        {
            Recursive = true,
            IncludeOwnedItems = true,
            DtoOptions = DtoOptions.StoredColumnsOnly
        });

        _logger.LogInformation("Recomputing the parental rating scores of {Count} items", ids.Count);

        var changedCount = 0;
        var processed = 0;
        foreach (var chunk in ids.Chunk(ChunkSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Items that were deleted since the ids were read are simply not returned.
            var items = _libraryManager.GetItemList(new InternalItemsQuery
            {
                ItemIds = chunk,
                IncludeOwnedItems = true,
                DtoOptions = DtoOptions.StoredColumnsOnly
            });

            var changed = new List<BaseItem>();
            foreach (var item in items)
            {
                if (item.OnMetadataChanged() != ItemUpdateType.None)
                {
                    changed.Add(item);
                }
            }

            if (changed.Count > 0)
            {
                await UpdateScoresAsync(
                    changed.Select(i => (i.Id, i.InheritedParentalRatingValue, i.InheritedParentalRatingSubValue)).ToArray(),
                    cancellationToken).ConfigureAwait(false);
                RefreshCachedItems(changed);
                changedCount += changed.Count;
            }

            processed += chunk.Length;
            progress.Report(100d * processed / ids.Count);
        }

        _stateStore.Save(startState);
        _logger.LogInformation("Recomputed the parental rating scores: {Changed} of {Count} items changed", changedCount, ids.Count);

        // A change made during the run is picked up by RatingsRecomputeHost, which checks the state again
        // when this task completes.
        progress.Report(100);
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield break;
    }

    /// <summary>
    /// Writes the two score columns of the given items, in one transaction.
    /// </summary>
    /// <param name="scores">The item ids with the score and sub score to store; <c>null</c> clears the column.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal async Task UpdateScoresAsync(IReadOnlyList<(Guid ItemId, int? Score, int? SubScore)> scores, CancellationToken cancellationToken)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                // One statement per distinct score pair, there are only a handful of those.
                foreach (var group in scores.GroupBy(e => (e.Score, e.SubScore)))
                {
                    var (score, subScore) = group.Key;
                    var ids = group.Select(e => e.ItemId).Distinct().ToArray();
                    await dbContext.BaseItems
                        .WhereOneOrMany(ids, e => e.Id)
                        .ExecuteUpdateAsync(
                            e => e
                                .SetProperty(f => f.InheritedParentalRatingValue, score)
                                .SetProperty(f => f.InheritedParentalRatingSubValue, subScore),
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Copies the new scores to the instances the library manager has cached, so that saving one of them
    /// does not write the old scores back. Items that are not cached are skipped, they are read fresh when needed.
    /// </summary>
    /// <param name="changed">The items whose scores changed.</param>
    private void RefreshCachedItems(IEnumerable<BaseItem> changed)
    {
        foreach (var item in changed)
        {
            var cached = CachedItemLookup(item.Id);
            if (cached is not null && !ReferenceEquals(cached, item))
            {
                cached.InheritedParentalRatingValue = item.InheritedParentalRatingValue;
                cached.InheritedParentalRatingSubValue = item.InheritedParentalRatingSubValue;
            }
        }
    }
}
