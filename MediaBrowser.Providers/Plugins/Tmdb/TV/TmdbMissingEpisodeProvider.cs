using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using TMDbLib.Objects.Search;

namespace MediaBrowser.Providers.Plugins.Tmdb.TV
{
    /// <summary>
    /// Creates virtual (metadata-only) entries for missing and unaired episodes.
    /// </summary>
    public class TmdbMissingEpisodeProvider : ICustomMetadataProvider<Series>, IHasItemChangeMonitor, IHasOrder
    {
        private readonly TmdbClientManager _tmdbClientManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<TmdbMissingEpisodeProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbMissingEpisodeProvider"/> class.
        /// </summary>
        /// <param name="tmdbClientManager">The <see cref="TmdbClientManager"/>.</param>
        /// <param name="libraryManager">The <see cref="ILibraryManager"/>.</param>
        /// <param name="fileSystem">The <see cref="IFileSystem"/>.</param>
        /// <param name="logger">The <see cref="ILogger{TmdbMissingEpisodeProvider}"/>.</param>
        public TmdbMissingEpisodeProvider(
            TmdbClientManager tmdbClientManager,
            ILibraryManager libraryManager,
            IFileSystem fileSystem,
            ILogger<TmdbMissingEpisodeProvider> logger)
        {
            _tmdbClientManager = tmdbClientManager;
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => TmdbUtils.ProviderName;

        /// <inheritdoc />
        // Run after the remote series provider so the TMDb id and other metadata are available.
        public int Order => 100;

        /// <inheritdoc />
        public bool HasChanged(BaseItem item, IDirectoryService directoryService)
        {
            // Reporting a change makes this provider (and only this provider) run during an otherwise incremental refresh.
            if (Plugin.Instance?.Configuration is null)
            {
                return false;
            }

            return item is Series series && series.HasProviderId(MetadataProvider.Tmdb);
        }

        /// <inheritdoc />
        public async Task<ItemUpdateType> FetchAsync(Series item, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            var configuration = Plugin.Instance?.Configuration;
            var importUnaired = (configuration?.ImportUnairedEpisodes).GetValueOrDefault();
            var importMissing = (configuration?.ImportMissingEpisodes).GetValueOrDefault();

            // The provider is inactive for this series when both global imports are off, or the series'
            // library has been opted out. In either case remove every virtual episode (unaired and missing
            // alike) it previously created, so disabling the feature cleans up on the next library scan.
            if ((!importUnaired && !importMissing) || !IsEnabledForLibrary(item))
            {
                if (!PruneAllVirtualEpisodes(item))
                {
                    return ItemUpdateType.None;
                }

                item.Children = null;
                return ItemUpdateType.MetadataImport;
            }

            var tmdbId = item.GetProviderId(MetadataProvider.Tmdb);
            if (string.IsNullOrEmpty(tmdbId)
                || !int.TryParse(tmdbId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seriesTmdbId)
                || seriesTmdbId <= 0)
            {
                return ItemUpdateType.None;
            }

            var language = item.GetPreferredMetadataLanguage();
            var countryCode = item.GetPreferredMetadataCountryCode();
            var imageLanguages = TmdbUtils.GetImageLanguagesParam(language, countryCode);

            var tmdbSeries = await _tmdbClientManager
                .GetSeriesAsync(seriesTmdbId, language, imageLanguages, countryCode, cancellationToken)
                .ConfigureAwait(false);

            if (tmdbSeries?.Seasons is null)
            {
                return ItemUpdateType.None;
            }

            var today = DateTime.UtcNow.Date;

            var importSpecials = (configuration?.ImportSpecials).GetValueOrDefault();
            var gracePeriodDays = Math.Max(0, (configuration?.UpcomingEpisodeGracePeriodDays).GetValueOrDefault());

            // Track every (season, episode) number that already exists (physical or virtual) so we never
            // create a duplicate.
            // When missing episodes are disabled, this pass also prunes virtual episodes that aired more
            // than the grace period ago, as well as any specials when specials are not wanted.
            var (existingEpisodes, updatableEpisodes) = GetExistingEpisodes(item, !importMissing, today, gracePeriodDays, importSpecials, out var prunedEpisodes);

            var seasonsByNumber = item.GetRecursiveChildren(i => i is Season)
                .OfType<Season>()
                .Where(s => s.IndexNumber.HasValue)
                .GroupBy(s => s.IndexNumber!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var addedEpisodes = false;
            var updatedEpisodes = false;

            foreach (var seasonInfo in tmdbSeries.Seasons)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var seasonNumber = seasonInfo.SeasonNumber;
                var tmdbSeason = await _tmdbClientManager
                    .GetSeasonAsync(seriesTmdbId, seasonNumber, language, imageLanguages, countryCode, cancellationToken)
                    .ConfigureAwait(false);

                if (tmdbSeason?.Episodes is null)
                {
                    continue;
                }

                foreach (var tmdbEpisode in tmdbSeason.Episodes)
                {
                    var episodeNumber = (int)tmdbEpisode.EpisodeNumber;
                    var premiereDate = GetPremiereDate(tmdbEpisode);

                    // Skips undated episodes, unaired (upcoming) ones unless upcoming import is enabled,
                    // already aired ones unless missing import is enabled, and unaired specials entirely.
                    if (!ShouldImportEpisode(premiereDate, today, importUnaired, importMissing, seasonNumber == 0, importSpecials))
                    {
                        continue;
                    }

                    var key = (seasonNumber, episodeNumber);

                    // Already have a virtual episode this provider created, keep metadata in sync with TMDb.
                    if (updatableEpisodes.TryGetValue(key, out var existingEpisode))
                    {
                        var season = await GetOrCreateSeasonAsync(item, seasonNumber, tmdbSeason.Name, seasonsByNumber, cancellationToken).ConfigureAwait(false);
                        var changed = UpdateVirtualEpisode(existingEpisode, tmdbEpisode, premiereDate);

                        if (!existingEpisode.ParentId.Equals(season.Id))
                        {
                            existingEpisode.SetParent(season);
                            existingEpisode.SeasonId = season.Id;
                            existingEpisode.SeasonName = season.Name;
                            changed = true;
                        }

                        if (string.IsNullOrEmpty(existingEpisode.PresentationUniqueKey))
                        {
                            existingEpisode.PresentationUniqueKey = existingEpisode.CreatePresentationUniqueKey();
                            changed = true;
                        }

                        if (changed)
                        {
                            await existingEpisode.UpdateToRepositoryAsync(ItemUpdateType.MetadataImport, cancellationToken).ConfigureAwait(false);
                            updatedEpisodes = true;
                        }

                        continue;
                    }

                    if (!existingEpisodes.Add(key))
                    {
                        continue;
                    }

                    var targetSeason = await GetOrCreateSeasonAsync(item, seasonNumber, tmdbSeason.Name, seasonsByNumber, cancellationToken).ConfigureAwait(false);
                    AddVirtualEpisode(item, targetSeason, tmdbEpisode, premiereDate);
                    addedEpisodes = true;
                }
            }

            if (!addedEpisodes && !prunedEpisodes && !updatedEpisodes)
            {
                return ItemUpdateType.None;
            }

            // Invalidate the cached children so that the season creation / cleanup that runs later in
            // SeriesMetadataService.AfterMetadataRefresh observes the newly created (and pruned) episodes.
            item.Children = null;

            return ItemUpdateType.MetadataImport;
        }

        /// <summary>
        /// Returns the series' season with the given number, creating (and refreshing) a virtual season
        /// when the whole season is missing from the library.
        /// </summary>
        private async Task<Season> GetOrCreateSeasonAsync(Series series, int seasonNumber, string? seasonName, Dictionary<int, Season> seasonsByNumber, CancellationToken cancellationToken)
        {
            if (seasonsByNumber.TryGetValue(seasonNumber, out var existingSeason))
            {
                return existingSeason;
            }

            _logger.LogInformation("Creating virtual season {SeasonNumber} for series {SeriesName}", seasonNumber, series.Name);

            var season = new Season
            {
                Name = seasonName,
                IndexNumber = seasonNumber,
                Id = _libraryManager.GetNewItemId(
                    series.Id.ToString("N", CultureInfo.InvariantCulture) + "Season" + seasonNumber.ToString(CultureInfo.InvariantCulture),
                    typeof(Season)),
                IsVirtualItem = true,
                SeriesId = series.Id,
                SeriesName = series.Name,
                SeriesPresentationUniqueKey = series.GetPresentationUniqueKey()
            };

            series.AddChild(season);
            await season.RefreshMetadata(new MetadataRefreshOptions(new DirectoryService(_fileSystem)), cancellationToken).ConfigureAwait(false);

            seasonsByNumber[seasonNumber] = season;
            return season;
        }

        private bool IsEnabledForLibrary(BaseItem item)
        {
            var disabledLibraries = Plugin.Instance?.Configuration.DisabledMissingEpisodeLibraries;
            if (disabledLibraries is null || disabledLibraries.Length == 0)
            {
                return true;
            }

            // A series can live under more than one collection folder; treat it as disabled only when
            // every containing library is opted out.
            var collectionFolders = _libraryManager.GetCollectionFolders(item);
            if (collectionFolders.Count == 0)
            {
                return true;
            }

            return collectionFolders.Any(folder =>
                !disabledLibraries.Contains(folder.Id.ToString("N", CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase));
        }

        private (HashSet<(int Season, int Episode)> Keys, Dictionary<(int Season, int Episode), Episode> Updatable) GetExistingEpisodes(Series series, bool pruneAgedOut, DateTime today, int gracePeriodDays, bool importSpecials, out bool pruned)
        {
            var keys = new HashSet<(int Season, int Episode)>();
            var updatable = new Dictionary<(int Season, int Episode), Episode>();
            pruned = false;

            // Enumerate by parent rather than via Series.GetEpisodes: on an initial scan the episodes'
            // SeriesPresentationUniqueKey is not set yet, so the presentation-key based query would miss
            // them. GetRecursiveChildren walks the actual child tree and sees them regardless.
            foreach (var episode in series.GetRecursiveChildren(i => i is Episode).OfType<Episode>())
            {
                // The series is refreshed before its episodes during an initial scan, so a freshly
                // resolved physical episode may not have its numbers populated yet. Resolve them from
                // the path (in memory, mirroring CreateSeasonsAsync) so we can dedupe against episodes
                // the user actually has files for instead of creating virtual duplicates.
                if (episode.IsFileProtocol && (!episode.ParentIndexNumber.HasValue || !episode.IndexNumber.HasValue))
                {
                    try
                    {
                        _libraryManager.FillMissingEpisodeNumbersFromPath(episode, false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error resolving episode number from path for {Path}", episode.Path);
                    }
                }

                // Virtual episodes this provider created are candidates for metadata sync (and pruning).
                var isOurs = episode.IsVirtualItem && episode.HasProviderId(MetadataProvider.Tmdb);

                if (ShouldPrune(episode, pruneAgedOut, today, gracePeriodDays, importSpecials))
                {
                    DeleteEpisode(episode, "no longer upcoming and missing episodes are disabled");
                    pruned = true;
                    continue;
                }

                if (episode.ParentIndexNumber.HasValue && episode.IndexNumber.HasValue)
                {
                    var key = (episode.ParentIndexNumber.Value, episode.IndexNumber.Value);
                    keys.Add(key);

                    // Virtual episodes this provider created are candidates for metadata sync.
                    if (isOurs)
                    {
                        updatable[key] = episode;
                    }
                }
            }

            return (keys, updatable);
        }

        /// <summary>
        /// Removes every virtual episode this provider previously created in the series.
        /// </summary>
        /// <param name="series">The series to clean up.</param>
        /// <returns><c>true</c> if any episode was removed; otherwise <c>false</c>.</returns>
        private bool PruneAllVirtualEpisodes(Series series)
        {
            var pruned = false;
            foreach (var episode in series.GetRecursiveChildren(i => i is Episode).OfType<Episode>())
            {
                if (episode.IsVirtualItem && episode.HasProviderId(MetadataProvider.Tmdb))
                {
                    DeleteEpisode(episode, "the TMDb missing episode provider is disabled for this library");
                    pruned = true;
                }
            }

            return pruned;
        }

        private void DeleteEpisode(Episode episode, string reason)
        {
            _logger.LogInformation(
                "Removing virtual episode S{SeasonNumber}E{EpisodeNumber} in series {SeriesName}: {Reason}",
                episode.ParentIndexNumber,
                episode.IndexNumber,
                episode.SeriesName,
                reason);

            _libraryManager.DeleteItem(
                episode,
                new DeleteOptions { DeleteFileLocation = false },
                false);
        }

        /// <summary>
        /// Determines whether a TMDb episode should be imported as a virtual item, based on its air date
        /// and the enabled options. Undated episodes are never imported; unaired (today or later) episodes
        /// require <paramref name="importUnaired"/>; already aired episodes require <paramref name="importMissing"/>.
        /// Specials (season 0) are only imported when <paramref name="importSpecials"/> is enabled.
        /// </summary>
        /// <param name="premiereDate">The episode air date (UTC), or null if unknown.</param>
        /// <param name="today">The current UTC date.</param>
        /// <param name="importUnaired">Whether unaired (upcoming) episodes should be imported.</param>
        /// <param name="importMissing">Whether already aired missing episodes should be imported.</param>
        /// <param name="isSpecial">Whether the episode belongs to the specials season (season 0).</param>
        /// <param name="importSpecials">Whether specials should be included.</param>
        /// <returns><c>true</c> if the episode should be imported; otherwise <c>false</c>.</returns>
        internal static bool ShouldImportEpisode(DateTime? premiereDate, DateTime today, bool importUnaired, bool importMissing, bool isSpecial, bool importSpecials)
        {
            if (!premiereDate.HasValue)
            {
                return false;
            }

            // Specials are only imported when the user opts in.
            if (isSpecial && !importSpecials)
            {
                return false;
            }

            var isUnaired = premiereDate.Value.Date >= today;
            return isUnaired ? importUnaired : importMissing;
        }

        /// <summary>
        /// Determines whether an existing virtual episode created by this provider (carries a TMDb id)
        /// should be pruned. Specials are removed entirely unless <paramref name="importSpecials"/> is
        /// enabled. Otherwise, when missing episodes are not wanted, an entry is pruned once its air date
        /// is more than <paramref name="gracePeriodDays"/> in the past; the grace period keeps recently
        /// aired episodes in place to allow for the delay between an episode airing and its file being
        /// added to the library.
        /// </summary>
        /// <param name="episode">The episode to evaluate.</param>
        /// <param name="pruneAgedOut">Whether aged-out virtual episodes should be pruned (missing import disabled).</param>
        /// <param name="today">The current UTC date.</param>
        /// <param name="gracePeriodDays">The number of days an aired episode is retained before pruning.</param>
        /// <param name="importSpecials">Whether specials should be kept.</param>
        /// <returns><c>true</c> if the episode should be pruned; otherwise <c>false</c>.</returns>
        internal static bool ShouldPrune(Episode episode, bool pruneAgedOut, DateTime today, int gracePeriodDays, bool importSpecials)
        {
            if (!episode.IsVirtualItem || !episode.HasProviderId(MetadataProvider.Tmdb))
            {
                return false;
            }

            // Specials are removed entirely unless the user opts in.
            if (episode.ParentIndexNumber == 0 && !importSpecials)
            {
                return true;
            }

            // When missing episodes are not wanted, prune placeholders for episodes that aired more than
            // the grace period ago.
            return pruneAgedOut
                && episode.PremiereDate.HasValue
                && episode.PremiereDate.Value.Date < today.AddDays(-gracePeriodDays);
        }

        internal static DateTime? GetPremiereDate(TvSeasonEpisode tmdbEpisode)
        {
            return tmdbEpisode.AirDate.HasValue
                ? DateTime.SpecifyKind(tmdbEpisode.AirDate.Value, DateTimeKind.Local).ToUniversalTime()
                : null;
        }

        internal static bool UpdateVirtualEpisode(Episode episode, TvSeasonEpisode tmdbEpisode, DateTime? premiereDate)
        {
            var changed = false;

            if (!string.IsNullOrEmpty(tmdbEpisode.Name) && !string.Equals(episode.Name, tmdbEpisode.Name, StringComparison.Ordinal))
            {
                episode.Name = tmdbEpisode.Name;
                changed = true;
            }

            if (!string.IsNullOrEmpty(tmdbEpisode.Overview) && !string.Equals(episode.Overview, tmdbEpisode.Overview, StringComparison.Ordinal))
            {
                episode.Overview = tmdbEpisode.Overview;
                changed = true;
            }

            if (premiereDate.HasValue && episode.PremiereDate != premiereDate)
            {
                episode.PremiereDate = premiereDate;
                episode.ProductionYear = tmdbEpisode.AirDate?.Year;
                changed = true;
            }

            return changed;
        }

        private void AddVirtualEpisode(Series series, Season season, TvSeasonEpisode tmdbEpisode, DateTime? premiereDate)
        {
            var seasonNumber = season.IndexNumber.GetValueOrDefault();
            var episodeNumber = (int)tmdbEpisode.EpisodeNumber;

            // Leaving Path unset makes the item a virtual (metadata-only) episode.
            var episode = new Episode
            {
                Name = tmdbEpisode.Name,
                IndexNumber = episodeNumber,
                ParentIndexNumber = seasonNumber,
                Id = _libraryManager.GetNewItemId(
                    series.Id.ToString("N", CultureInfo.InvariantCulture)
                        + "Season" + seasonNumber.ToString(CultureInfo.InvariantCulture)
                        + "Episode" + episodeNumber.ToString(CultureInfo.InvariantCulture),
                    typeof(Episode)),
                IsVirtualItem = true,
                PremiereDate = premiereDate,
                ProductionYear = tmdbEpisode.AirDate?.Year,
                Overview = tmdbEpisode.Overview,
                SeasonId = season.Id,
                SeasonName = season.Name,
                SeriesId = series.Id,
                SeriesName = series.Name,
                SeriesPresentationUniqueKey = series.GetPresentationUniqueKey()
            };

            episode.PresentationUniqueKey = episode.CreatePresentationUniqueKey();

            if (tmdbEpisode.Id > 0)
            {
                episode.SetProviderId(MetadataProvider.Tmdb, tmdbEpisode.Id.ToString(CultureInfo.InvariantCulture));
            }

            _logger.LogInformation(
                "Creating virtual episode S{SeasonNumber}E{EpisodeNumber} for series {SeriesName}",
                seasonNumber,
                episodeNumber,
                series.Name);

            season.AddChild(episode);
        }
    }
}
