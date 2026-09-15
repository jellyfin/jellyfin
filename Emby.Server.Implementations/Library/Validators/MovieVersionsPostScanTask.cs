using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Validators;

/// <summary>
/// Post-scan task that automatically merges copies of the same movie that are spread across multiple folders into version groups.
/// Removes scan-created version links that are no longer valid.
/// </summary>
public class MovieVersionsPostScanTask : ILibraryPostScanTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IVideoVersionManager _videoVersionManager;
    private readonly ILinkedChildrenService _linkedChildrenService;
    private readonly ILogger<MovieVersionsPostScanTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovieVersionsPostScanTask" /> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="videoVersionManager">The video version manager.</param>
    /// <param name="linkedChildrenService">The linked children service.</param>
    /// <param name="logger">The logger.</param>
    public MovieVersionsPostScanTask(
        ILibraryManager libraryManager,
        IVideoVersionManager videoVersionManager,
        ILinkedChildrenService linkedChildrenService,
        ILogger<MovieVersionsPostScanTask> logger)
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
        // Copies of the same movie can only exist as separate items when they live in different folders;
        // multiple files inside one movie folder are resolved into local alternate versions during the scan.
        var movies = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            GroupByPresentationUniqueKey = false,
            IsVirtualItem = false
        })
            .OfType<Movie>()
            .Where(m => m.OwnerId.IsEmpty() && !m.ExtraType.HasValue && !string.IsNullOrEmpty(m.Path))
            .ToList();

        // The library option is only consulted for the copies that could actually be merged, since
        // resolving an item's library is far more expensive than computing its grouping key.
        var candidates = movies
            .Select(m => (Movie: m, Key: GetVersionGroupKey(m)))
            .Where(m => m.Key is not null)
            .GroupBy(m => m.Key!, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Select(m => m.Movie).Where(IsAutoGroupingEnabled).ToList())
            .Where(g => g.Count > 1)
            .ToList();

        var exclusions = _linkedChildrenService.GetAutoMergeExclusions();
        var reconciledPrimaryIds = new HashSet<Guid>();
        var numComplete = 0;
        foreach (var group in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await ReconcileMovieGroup(group, exclusions, reconciledPrimaryIds, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error reconciling movie versions for {MovieName}", group[0].Name);
            }

            numComplete++;
            progress.Report(95.0 * numComplete / candidates.Count);
        }

        await CleanupOrphanedAutoLinks(reconciledPrimaryIds, cancellationToken).ConfigureAwait(false);

        progress.Report(100);
    }

    private bool IsAutoGroupingEnabled(Movie movie)
    {
        return _libraryManager.GetLibraryOptions(movie).EnableAutomaticMovieVersionGrouping;
    }

    /// <summary>
    /// Gets the identity shared by all copies of one movie, or <c>null</c> when the movie cannot be
    /// identified well enough to be merged automatically.
    /// </summary>
    private static string? GetVersionGroupKey(Movie movie)
    {
        // A provider id is the only stable cross-folder identity.
        if (movie.TryGetProviderId(MetadataProvider.Tmdb, out var tmdbId))
        {
            return "tmdb-" + tmdbId;
        }

        if (movie.TryGetProviderId(MetadataProvider.Imdb, out var imdbId))
        {
            return "imdb-" + imdbId;
        }

        // Unidentified movies fall back to their name, which requires the production year to
        // disambiguate: same-named remakes are common and merging those would be wrong.
        return movie.ProductionYear.HasValue && !string.IsNullOrEmpty(movie.Name)
            ? "movie-" + movie.Name.ToLowerInvariant() + "-" + movie.ProductionYear.Value.ToString(CultureInfo.InvariantCulture)
            : null;
    }

    private async Task ReconcileMovieGroup(
        List<Movie> members,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> exclusions,
        HashSet<Guid> reconciledPrimaryIds,
        CancellationToken cancellationToken)
    {
        var memberIds = members.Select(m => m.Id).ToHashSet();

        // Drop auto links that no longer point at a copy of the same movie, e.g. because the alternate
        // was re-identified. Only links leaving the group are stale, so no member state is invalidated.
        foreach (var movie in members)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var staleLinks = movie.LinkedAlternateVersions
                .Where(l => l.Type == LinkedChildType.AutoLinkedAlternateVersion && l.ItemId.HasValue && !memberIds.Contains(l.ItemId.Value))
                .ToList();

            foreach (var link in staleLinks)
            {
                _logger.LogInformation(
                    "Removing stale movie version link from {PrimaryName} ({PrimaryId}) to {AlternateId}",
                    movie.Name,
                    movie.Id,
                    link.ItemId!.Value);

                await _videoVersionManager.RemoveVersionLinkAsync(movie, link.ItemId.Value, cancellationToken).ConfigureAwait(false);
            }
        }

        var mergeable = FilterUserSplitVersions(
            members.Where(m => !m.PrimaryVersionId.HasValue || memberIds.Contains(m.PrimaryVersionId.Value)),
            exclusions);
        if (mergeable.Count < 2)
        {
            return;
        }

        // Skip groups that are already merged to avoid needless writes on every scan.
        var primaries = mergeable.Where(m => !m.PrimaryVersionId.HasValue).ToList();
        if (primaries.Count == 1
            && mergeable.TrueForAll(m => m.Id.Equals(primaries[0].Id) || (m.PrimaryVersionId.HasValue && m.PrimaryVersionId.Value.Equals(primaries[0].Id))))
        {
            reconciledPrimaryIds.Add(primaries[0].Id);
            return;
        }

        _logger.LogInformation(
            "Merging {Count} versions of {MovieName} ({ProductionYear})",
            mergeable.Count,
            mergeable[0].Name,
            mergeable[0].ProductionYear);

        var primary = await _videoVersionManager.MergeVersionsAsync(mergeable, true, cancellationToken).ConfigureAwait(false);
        if (primary is not null)
        {
            reconciledPrimaryIds.Add(primary.Id);
        }
    }

    /// <summary>
    /// Drops the members of a version group that the user split apart from one another.
    /// </summary>
    private static List<Movie> FilterUserSplitVersions(
        IEnumerable<Movie> members,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> exclusions)
    {
        var candidates = members.ToList();

        // The group's current primary claims its slot first, then the unmerged members, so an
        // exclusion drops the copy the user split away instead of breaking an untouched merge.
        var groupPrimaryIds = candidates
            .Where(m => m.PrimaryVersionId.HasValue)
            .Select(m => m.PrimaryVersionId!.Value)
            .ToHashSet();

        var mergeable = new List<Movie>();
        foreach (var member in candidates
            .OrderBy(m => groupPrimaryIds.Contains(m.Id) ? 0 : 1)
            .ThenBy(m => m.PrimaryVersionId.HasValue)
            .ThenBy(m => m.Id))
        {
            if (exclusions.TryGetValue(member.Id, out var excludedIds)
                && mergeable.Exists(m => excludedIds.Contains(m.Id)))
            {
                continue;
            }

            mergeable.Add(member);
        }

        return mergeable;
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

            // Only movies; auto links of other item types are owned by their own post-scan task.
            if (reconciledPrimaryIds.Contains(parentId) || _libraryManager.GetItemById(parentId) is not Movie primary)
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
                "Removing {Count} orphaned movie version links from {PrimaryName} ({PrimaryId})",
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
