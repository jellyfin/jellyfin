using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.TV;

/// <summary>
/// Service to manage series metadata.
/// </summary>
public class SeriesMetadataService : MetadataService<Series, SeriesInfo>
{
    private readonly ILocalizationManager _localizationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeriesMetadataService"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="localizationManager">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="externalDataManager">Instance of the <see cref="IExternalDataManager"/> interface.</param>
    /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
    public SeriesMetadataService(
        IServerConfigurationManager serverConfigurationManager,
        ILogger<SeriesMetadataService> logger,
        IProviderManager providerManager,
        IFileSystem fileSystem,
        ILibraryManager libraryManager,
        ILocalizationManager localizationManager,
        IExternalDataManager externalDataManager,
        IItemRepository itemRepository)
        : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager, externalDataManager, itemRepository)
    {
        _localizationManager = localizationManager;
    }

    /// <inheritdoc />
    public override async Task<ItemUpdateType> RefreshMetadata(BaseItem item, MetadataRefreshOptions refreshOptions, CancellationToken cancellationToken)
    {
        if (item is Series series)
        {
            var seasons = series.GetRecursiveChildren(i => i is Season).ToList();

            foreach (var season in seasons)
            {
                var hasUpdate = refreshOptions is not null && season.BeforeMetadataRefresh(refreshOptions.ReplaceAllMetadata);
                if (hasUpdate)
                {
                    await season.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return await base.RefreshMetadata(item, refreshOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task AfterMetadataRefresh(Series item, MetadataRefreshOptions refreshOptions, CancellationToken cancellationToken)
    {
        await base.AfterMetadataRefresh(item, refreshOptions, cancellationToken).ConfigureAwait(false);

        // Note that this only updates the children's SeriesPresentationUniqueKey and SeasonId, not the ParentIndexNumber
        if (LibraryManager.GetLibraryOptions(item).EnableAutomaticSeriesGrouping)
        {
            await UpdateSeriesChildrenInfoAsync(item, cancellationToken).ConfigureAwait(false);
        }

        RemoveObsoleteEpisodes(item);
        RemoveObsoleteSeasons(item);
        await CreateSeasonsAsync(item, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reconciles seasons and episodes with the series' finalized state.
    /// </summary>
    /// <remarks>
    /// The series' presentation unique key can change during a refresh once provider ids become
    /// available - notably with <c>EnableAutomaticSeriesGrouping</c>, where the key is derived from
    /// the provider id and the owning libraries instead of the (immutable) item id. Seasons and
    /// episodes cache this value in <see cref="IHasSeries.SeriesPresentationUniqueKey"/> and are
    /// matched to (and displayed under) the series by it, so any child left with a stale key - or an
    /// episode not yet linked to a freshly created season - stays hidden until a later scan. Syncing
    /// them against the series here lets everything appear within a single scan.
    /// </remarks>
    /// <param name="series">The series.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The async task.</returns>
    private async Task UpdateSeriesChildrenInfoAsync(Series series, CancellationToken cancellationToken)
    {
        // Reload children so episode numbers / seasons persisted earlier in the refresh are seen.
        series.Children = null;
        var seriesKey = series.GetPresentationUniqueKey();
        var children = series.GetRecursiveChildren(i => i is Season || i is Episode);
        var seasons = children.OfType<Season>().ToList();

        foreach (var child in children)
        {
            var updateType = ItemUpdateType.None;

            if (child is IHasSeries hasSeries
                && !string.Equals(hasSeries.SeriesPresentationUniqueKey, seriesKey, StringComparison.Ordinal))
            {
                hasSeries.SeriesPresentationUniqueKey = seriesKey;
                updateType |= ItemUpdateType.MetadataImport;
            }

            if (child is Episode episode)
            {
                var seasonId = episode.FindSeasonId();
                if (seasonId.IsEmpty() && episode.ParentIndexNumber.HasValue)
                {
                    seasonId = seasons.Find(s => s.IndexNumber == episode.ParentIndexNumber)?.Id ?? Guid.Empty;
                }

                if (!seasonId.IsEmpty() && !episode.SeasonId.Equals(seasonId))
                {
                    episode.SeasonId = seasonId;
                    updateType |= ItemUpdateType.MetadataImport;
                }
            }

            if (updateType > ItemUpdateType.None)
            {
                await child.UpdateToRepositoryAsync(updateType, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <inheritdoc />
    protected override void MergeData(MetadataResult<Series> source, MetadataResult<Series> target, MetadataField[] lockedFields, bool replaceData, bool mergeMetadataSettings)
    {
        base.MergeData(source, target, lockedFields, replaceData, mergeMetadataSettings);

        var sourceItem = source.Item;
        var targetItem = target.Item;

        if (replaceData || string.IsNullOrEmpty(targetItem.AirTime))
        {
            targetItem.AirTime = sourceItem.AirTime;
        }

        if (replaceData || !targetItem.Status.HasValue)
        {
            targetItem.Status = sourceItem.Status;
        }

        if (replaceData || targetItem.AirDays is null || targetItem.AirDays.Length == 0)
        {
            targetItem.AirDays = sourceItem.AirDays;
        }
    }

    private void RemoveObsoleteSeasons(Series series)
    {
        // TODO Legacy. It's not really "physical" seasons as any virtual seasons are always converted to non-virtual in CreateSeasonsAsync.
        var physicalSeasonNumbers = new HashSet<int>();
        var virtualSeasons = new List<Season>();
        foreach (var existingSeason in series.Children.OfType<Season>())
        {
            if (existingSeason.LocationType != LocationType.Virtual && existingSeason.IndexNumber.HasValue)
            {
                physicalSeasonNumbers.Add(existingSeason.IndexNumber.Value);
            }
            else if (existingSeason.LocationType == LocationType.Virtual)
            {
                virtualSeasons.Add(existingSeason);
            }
        }

        foreach (var virtualSeason in virtualSeasons)
        {
            var seasonNumber = virtualSeason.IndexNumber;
            // If there's a physical season with the same number or no episodes in the season, delete it
            if ((seasonNumber.HasValue && physicalSeasonNumbers.Contains(seasonNumber.Value))
                || virtualSeason.GetEpisodes().Count == 0)
            {
                Logger.LogInformation("Removing virtual season {SeasonNumber} in series {SeriesName}", virtualSeason.IndexNumber, series.Name);

                LibraryManager.DeleteItem(
                    virtualSeason,
                    new DeleteOptions
                    {
                        // Internal metadata paths are removed regardless of this.
                        DeleteFileLocation = false
                    },
                    false);
            }
        }
    }

    private void RemoveObsoleteEpisodes(Series series)
    {
        var episodesBySeason = series.GetEpisodes(null, new DtoOptions(), true)
                        .OfType<Episode>()
                        .GroupBy(e => e.ParentIndexNumber)
                        .ToList();

        foreach (var seasonEpisodes in episodesBySeason)
        {
            List<Episode> nonPhysicalEpisodes = [];
            List<Episode> physicalEpisodes = [];
            foreach (var episode in seasonEpisodes)
            {
                if (episode.IsVirtualItem || episode.IsMissingEpisode)
                {
                    nonPhysicalEpisodes.Add(episode);
                    continue;
                }

                physicalEpisodes.Add(episode);
            }

            // Only consider non-physical episodes
            foreach (var episode in nonPhysicalEpisodes)
            {
                // Episodes without an episode number are practically orphaned and should be deleted
                // Episodes with a physical equivalent should be deleted (they are no longer missing)
                var shouldKeep = episode.IndexNumber.HasValue && !physicalEpisodes.Any(e => e.ContainsEpisodeNumber(episode.IndexNumber.Value));

                if (shouldKeep)
                {
                    continue;
                }

                DeleteEpisode(episode);
            }
        }
    }

    private void DeleteEpisode(Episode episode)
    {
        Logger.LogInformation(
            "Removing virtual episode S{SeasonNumber}E{EpisodeNumber} in series {SeriesName}",
            episode.ParentIndexNumber,
            episode.IndexNumber,
            episode.SeriesName);

        LibraryManager.DeleteItem(
            episode,
            new DeleteOptions
            {
                // Internal metadata paths are removed regardless of this.
                DeleteFileLocation = false
            },
            false);
    }

    private static bool NeedsVirtualSeason(Episode episode, HashSet<Guid> physicalSeasonIds, HashSet<string> physicalSeasonPaths)
    {
        // Episode has a known season number, needs a season
        if (episode.ParentIndexNumber.HasValue)
        {
            return true;
        }

        // Episode has been processed and linked to a season, only needs a virtual season
        // if it isn't already linked to a known physical season by ID or path
        if (!episode.SeasonId.IsEmpty())
        {
            return !physicalSeasonIds.Contains(episode.SeasonId)
                && !physicalSeasonPaths.Contains(System.IO.Path.GetDirectoryName(episode.Path) ?? string.Empty);
        }

        // Episode not yet linked, check if it's in a physical season folder
        // If yes then skip it, processing not finished
        // If no then include it, needs Season Unknown
        var episodeDirectory = System.IO.Path.GetDirectoryName(episode.Path) ?? string.Empty;
        return !physicalSeasonPaths.Contains(episodeDirectory);
    }

    /// <summary>
    /// Creates seasons for all episodes if they don't exist.
    /// If no season number can be determined, a dummy season will be created.
    /// </summary>
    /// <param name="series">The series.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The async task.</returns>
    private async Task CreateSeasonsAsync(Series series, CancellationToken cancellationToken)
    {
        var seriesChildren = series.GetRecursiveChildren(i => i is Episode || i is Season);

        // CreateSeasonsAsync can run before the episodes themselves have been refreshed during an
        // initial scan, so their ParentIndexNumber may still be unset. Resolve the season number
        // from the path first to avoid creating a premature "Season Unknown" instead of the real
        // season for episodes that live directly in a flat series folder.
        foreach (var episode in seriesChildren.OfType<Episode>())
        {
            if (episode.ParentIndexNumber.HasValue)
            {
                continue;
            }

            try
            {
                LibraryManager.FillMissingEpisodeNumbersFromPath(episode, false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error resolving season number from path for {Path}", episode.Path);
            }
        }

        var seasons = seriesChildren.OfType<Season>().ToList();
        var episodes = seriesChildren.OfType<Episode>().ToList();

        var physicalSeasonIds = seasons
            .Where(e => e.LocationType != LocationType.Virtual)
            .Select(e => e.Id)
            .ToHashSet();

        var physicalSeasonPathSet = seasons
            .Where(e => e.LocationType != LocationType.Virtual && !string.IsNullOrEmpty(e.Path))
            .Select(e => e.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var uniqueSeasonNumbers = seriesChildren
            .OfType<Episode>()
            .Where(e => NeedsVirtualSeason(e, physicalSeasonIds, physicalSeasonPathSet))
            .Select(e => e.ParentIndexNumber >= 0 ? e.ParentIndexNumber : null)
            .Distinct();

        // Loop through the unique season numbers
        foreach (var seasonNumber in uniqueSeasonNumbers)
        {
            // Null season numbers will have a 'dummy' season created because seasons are always required.
            var existingSeason = seasons.FirstOrDefault(i => i.IndexNumber == seasonNumber);
            if (existingSeason is null)
            {
                var seasonName = GetValidSeasonNameForSeries(series, null, seasonNumber);
                var season = await CreateSeasonAsync(series, seasonName, seasonNumber, cancellationToken).ConfigureAwait(false);
                seasons.Add(season);
            }
            else if (existingSeason.IsVirtualItem)
            {
                var episodeCount = episodes.Count(e => e.ParentIndexNumber == seasonNumber && !e.IsMissingEpisode);
                if (episodeCount > 0)
                {
                    existingSeason.IsVirtualItem = false;
                    await existingSeason.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        // Loop through episodes
        foreach (var episode in episodes)
        {
            var season = seasons.FirstOrDefault(i => i.IndexNumber == episode.ParentIndexNumber);
            if (season is null || episode.SeasonId.Equals(season.Id))
            {
                continue;
            }

            // Assign the correct season id and name to episode.
            episode.SeasonId = season.Id;
            episode.SeasonName = season.Name;
            await episode.UpdateToRepositoryAsync(ItemUpdateType.MetadataImport, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Creates a new season, adds it to the database by linking it to the [series] and refreshes the metadata.
    /// </summary>
    /// <param name="series">The series.</param>
    /// <param name="seasonName">The season name.</param>
    /// <param name="seasonNumber">The season number.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The newly created season.</returns>
    private async Task<Season> CreateSeasonAsync(
        Series series,
        string? seasonName,
        int? seasonNumber,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("Creating Season {SeasonName} entry for {SeriesName}", seasonName, series.Name);

        var season = new Season
        {
            Name = seasonName,
            IndexNumber = seasonNumber,
            Id = LibraryManager.GetNewItemId(
                series.Id + (seasonNumber ?? -1).ToString(CultureInfo.InvariantCulture) + seasonName,
                typeof(Season)),
            IsVirtualItem = false,
            SeriesId = series.Id,
            SeriesName = series.Name,
            SeriesPresentationUniqueKey = series.GetPresentationUniqueKey()
        };

        series.AddChild(season);
        await season.RefreshMetadata(new MetadataRefreshOptions(new DirectoryService(FileSystem)), cancellationToken).ConfigureAwait(false);

        return season;
    }

    private string GetValidSeasonNameForSeries(Series series, string? seasonName, int? seasonNumber)
    {
        if (string.IsNullOrEmpty(seasonName))
        {
            seasonName = seasonNumber switch
            {
                null => _localizationManager.GetLocalizedString("NameSeasonUnknown"),
                0 => LibraryManager.GetLibraryOptions(series).SeasonZeroDisplayName,
                _ => string.Format(CultureInfo.InvariantCulture, _localizationManager.GetLocalizedString("NameSeasonNumber"), seasonNumber.Value)
            };
        }

        return seasonName;
    }
}
