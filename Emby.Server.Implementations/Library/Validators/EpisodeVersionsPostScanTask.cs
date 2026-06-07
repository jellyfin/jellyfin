using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Validators;

/// <summary>
/// Post-scan task that automatically merges same-numbered episodes of a series that is spread  across multiple folders (or multiple same-numbered season folders) into version groups.
/// Removes scan-created version links that are no longer valid.
/// </summary>
public class EpisodeVersionsPostScanTask : ILibraryPostScanTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IVideoVersionManager _videoVersionManager;
    private readonly ILinkedChildrenService _linkedChildrenService;
    private readonly ILogger<EpisodeVersionsPostScanTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EpisodeVersionsPostScanTask" /> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="videoVersionManager">The video version manager.</param>
    /// <param name="linkedChildrenService">The linked children service.</param>
    /// <param name="logger">The logger.</param>
    public EpisodeVersionsPostScanTask(
        ILibraryManager libraryManager,
        IVideoVersionManager videoVersionManager,
        ILinkedChildrenService linkedChildrenService,
        ILogger<EpisodeVersionsPostScanTask> logger)
    {
        _libraryManager = libraryManager;
        _videoVersionManager = videoVersionManager;
        _linkedChildrenService = linkedChildrenService;
        _logger = logger;
    }

    /// <summary>
    /// Runs the task.
    /// </summary>
    /// <param name="progress">The progress.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task.</returns>
    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var seriesGroups = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Series],
            GroupByPresentationUniqueKey = false
        })
            .OfType<Series>()
            .GroupBy(s => s.GetPresentationUniqueKey(), StringComparer.Ordinal)
            .ToList();

        var seasonIndexesBySeriesKey = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Season],
            GroupByPresentationUniqueKey = false
        })
            .OfType<Season>()
            .Where(s => !string.IsNullOrEmpty(s.SeriesPresentationUniqueKey))
            .GroupBy(s => s.SeriesPresentationUniqueKey, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(s => s.IndexNumber).ToList(), StringComparer.Ordinal);

        // Same-numbered episodes can only exist as separate items when the logical series spans
        // multiple series folders or contains multiple seasons with the same index.
        var candidates = seriesGroups
            .Where(g => g.Count() > 1
                || (seasonIndexesBySeriesKey.TryGetValue(g.Key, out var seasonIndexes)
                    && seasonIndexes.Where(i => i.HasValue).GroupBy(i => i!.Value).Any(d => d.Count() > 1)))
            .ToList();

        var reconciledPrimaryIds = new HashSet<Guid>();
        var numComplete = 0;
        foreach (var group in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await ReconcileSeriesGroup(group.Key, reconciledPrimaryIds, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error reconciling episode versions for series {SeriesName}", group.First().Name);
            }

            numComplete++;
            progress.Report(95.0 * numComplete / candidates.Count);
        }

        await CleanupOrphanedAutoLinks(reconciledPrimaryIds, cancellationToken).ConfigureAwait(false);

        progress.Report(100);
    }

    private async Task ReconcileSeriesGroup(string seriesKey, HashSet<Guid> reconciledPrimaryIds, CancellationToken cancellationToken)
    {
        var episodes = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Episode],
            SeriesPresentationUniqueKey = seriesKey,
            GroupByPresentationUniqueKey = false,
            IsVirtualItem = false
        })
            .OfType<Episode>()
            .Where(e => e.OwnerId.IsEmpty() && !string.IsNullOrEmpty(e.Path))
            .ToList();

        var membersByNumber = episodes
            .Where(e => e.ParentIndexNumber.HasValue && e.IndexNumber.HasValue)
            .GroupBy(e => (Season: e.ParentIndexNumber!.Value, Number: e.IndexNumber!.Value, NumberEnd: e.IndexNumberEnd))
            .ToDictionary(g => g.Key, g => g.ToList());

        // Drop auto links that no longer match the episode numbering.
        var unlinkedIds = new HashSet<Guid>();
        foreach (var episode in episodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var autoLinks = episode.LinkedAlternateVersions
                .Where(l => l.Type == LinkedChildType.AutoLinkedAlternateVersion && l.ItemId.HasValue)
                .ToList();
            if (autoLinks.Count == 0)
            {
                continue;
            }

            var validIds = episode.ParentIndexNumber.HasValue
                && episode.IndexNumber.HasValue
                && membersByNumber.TryGetValue((episode.ParentIndexNumber.Value, episode.IndexNumber.Value, episode.IndexNumberEnd), out var members)
                    ? members.Select(m => m.Id).ToHashSet()
                    : new HashSet<Guid>();

            foreach (var link in autoLinks)
            {
                if (validIds.Contains(link.ItemId!.Value))
                {
                    continue;
                }

                _logger.LogInformation(
                    "Removing stale episode version link from {PrimaryName} ({PrimaryId}) to {AlternateId}",
                    episode.Name,
                    episode.Id,
                    link.ItemId.Value);

                await _videoVersionManager.RemoveVersionLinkAsync(episode, link.ItemId.Value, cancellationToken).ConfigureAwait(false);

                unlinkedIds.Add(link.ItemId.Value);
            }
        }

        // Merge the episodes of every number group into one version group.
        foreach (var (key, members) in membersByNumber)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (members.Count < 2)
            {
                continue;
            }

            // Re-fetch members whose links were just dropped so the merge sees their current state.
            for (var i = 0; i < members.Count; i++)
            {
                if (unlinkedIds.Contains(members[i].Id) && _libraryManager.GetItemById(members[i].Id) is Episode refreshed)
                {
                    members[i] = refreshed;
                }
            }

            var memberIds = members.Select(m => m.Id).ToHashSet();
            var mergeable = members
                .Where(m => !m.PrimaryVersionId.HasValue || memberIds.Contains(m.PrimaryVersionId.Value))
                .ToList();
            if (mergeable.Count < 2)
            {
                continue;
            }

            // Skip groups that are already merged to avoid needless writes on every scan.
            var primaries = mergeable.Where(m => !m.PrimaryVersionId.HasValue).ToList();
            if (primaries.Count == 1
                && mergeable.TrueForAll(m => m.Id.Equals(primaries[0].Id) || (m.PrimaryVersionId.HasValue && m.PrimaryVersionId.Value.Equals(primaries[0].Id))))
            {
                reconciledPrimaryIds.Add(primaries[0].Id);
                continue;
            }

            _logger.LogInformation(
                "Merging {Count} versions of {SeriesName} S{Season:00}E{Episode:00}",
                mergeable.Count,
                mergeable[0].SeriesName,
                key.Season,
                key.Number);

            var primary = await _videoVersionManager.MergeVersionsAsync(mergeable, true, cancellationToken).ConfigureAwait(false);
            if (primary is not null)
            {
                reconciledPrimaryIds.Add(primary.Id);
            }
        }
    }

    /// <summary>
    /// Unlinks auto-created version links whose primary was not part of any reconciled group.
    /// </summary>
    private async Task CleanupOrphanedAutoLinks(HashSet<Guid> reconciledPrimaryIds, CancellationToken cancellationToken)
    {
        var parentIds = _linkedChildrenService.GetParentIdsWithChildType(LinkedChildType.AutoLinkedAlternateVersion);
        foreach (var parentId in parentIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reconciledPrimaryIds.Contains(parentId) || _libraryManager.GetItemById(parentId) is not Video primary)
            {
                continue;
            }

            var autoLinks = primary.LinkedAlternateVersions
                .Where(l => l.Type == LinkedChildType.AutoLinkedAlternateVersion && l.ItemId.HasValue)
                .ToList();
            if (autoLinks.Count == 0)
            {
                continue;
            }

            _logger.LogInformation(
                "Removing {Count} orphaned episode version links from {PrimaryName} ({PrimaryId})",
                autoLinks.Count,
                primary.Name,
                primary.Id);

            foreach (var link in autoLinks)
            {
                await _videoVersionManager.RemoveVersionLinkAsync(primary, link.ItemId!.Value, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
