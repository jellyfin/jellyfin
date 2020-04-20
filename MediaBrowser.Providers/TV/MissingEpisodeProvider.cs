using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Plugins.TheTvdb;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.TV
{
    public class MissingEpisodeProvider
    {
        private const double UnairedEpisodeThresholdDays = 2;

        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly ILocalizationManager _localization;
        private readonly IFileSystem _fileSystem;
        private readonly TvdbClientManager _tvdbClientManager;

        public MissingEpisodeProvider(
            ILogger logger,
            IServerConfigurationManager config,
            ILibraryManager libraryManager,
            ILocalizationManager localization,
            IFileSystem fileSystem,
            TvdbClientManager tvdbClientManager)
        {
            _logger = logger;
            _config = config;
            _libraryManager = libraryManager;
            _localization = localization;
            _fileSystem = fileSystem;
            _tvdbClientManager = tvdbClientManager;
        }

        public async Task<bool> Run(Series series, bool addNewItems, CancellationToken cancellationToken)
        {
            var tvdbId = series.GetProviderId(MetadataProviders.Tvdb);
            if (string.IsNullOrEmpty(tvdbId))
            {
                return false;
            }

            var episodes = await _tvdbClientManager.GetAllEpisodesAsync(Convert.ToInt32(tvdbId), series.GetPreferredMetadataLanguage(), cancellationToken);

            var episodeLookup = episodes
                .Select(i =>
                {
                    DateTime.TryParse(i.FirstAired, out var firstAired);
                    var seasonNumber = i.AiredSeason.GetValueOrDefault(-1);
                    var episodeNumber = i.AiredEpisodeNumber.GetValueOrDefault(-1);
                    return (seasonNumber, episodeNumber, firstAired);
                })
                .Where(i => i.seasonNumber != -1 && i.episodeNumber != -1)
                .OrderBy(i => i.seasonNumber)
                .ThenBy(i => i.episodeNumber)
                .ToList();

            var allRecursiveChildren = series.GetRecursiveChildren();

            var hasBadData = HasInvalidContent(allRecursiveChildren);

            // Be conservative here to avoid creating missing episodes for ones they already have
            var addMissingEpisodes = !hasBadData && _libraryManager.GetLibraryOptions(series).ImportMissingEpisodes;

            var anySeasonsRemoved = RemoveObsoleteOrMissingSeasons(allRecursiveChildren, episodeLookup);

            if (anySeasonsRemoved)
            {
                // refresh this
                allRecursiveChildren = series.GetRecursiveChildren();
            }

            var anyEpisodesRemoved = RemoveObsoleteOrMissingEpisodes(allRecursiveChildren, episodeLookup, addMissingEpisodes);

            if (anyEpisodesRemoved)
            {
                // refresh this
                allRecursiveChildren = series.GetRecursiveChildren();
            }

            var hasNewEpisodes = false;

            if (addNewItems && series.IsMetadataFetcherEnabled(_libraryManager.GetLibraryOptions(series), TvdbSeriesProvider.Current.Name))
            {
                hasNewEpisodes = await AddMissingEpisodes(series, allRecursiveChildren, addMissingEpisodes, episodeLookup, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (hasNewEpisodes || anySeasonsRemoved || anyEpisodesRemoved)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if a series has any seasons or episodes without season or episode numbers
        /// If this data is missing no virtual items will be added in order to prevent possible duplicates
        /// </summary>
        private bool HasInvalidContent(IList<BaseItem> allItems)
        {
            return allItems.OfType<Season>().Any(i => !i.IndexNumber.HasValue) ||
                   allItems.OfType<Episode>().Any(i =>
                   {
                       if (!i.ParentIndexNumber.HasValue)
                       {
                           return true;
                       }

                       // You could have episodes under season 0 with no number
                       return false;
                   });
        }

        private async Task<bool> AddMissingEpisodes(
            Series series,
            IEnumerable<BaseItem> allItems,
            bool addMissingEpisodes,
            IReadOnlyCollection<(int seasonNumber, int episodenumber, DateTime firstAired)> episodeLookup,
            CancellationToken cancellationToken)
        {
            var existingEpisodes = allItems.OfType<Episode>().ToList();

            var seasonCounts = episodeLookup.GroupBy(e => e.seasonNumber).ToDictionary(g => g.Key, g => g.Count());

            var hasChanges = false;

            foreach (var tuple in episodeLookup)
            {
                if (tuple.seasonNumber <= 0 || tuple.episodenumber <= 0)
                {
                    // Ignore episode/season zeros
                    continue;
                }

                var existingEpisode = GetExistingEpisode(existingEpisodes, seasonCounts, tuple);

                if (existingEpisode != null)
                {
                    continue;
                }

                var airDate = tuple.firstAired;

                var now = DateTime.UtcNow.AddDays(-UnairedEpisodeThresholdDays);

                if (airDate < now && addMissingEpisodes || airDate > now)
                {
                    // tvdb has a lot of nearly blank episodes
                    _logger.LogInformation("Creating virtual missing/unaired episode {0} {1}x{2}", series.Name, tuple.seasonNumber, tuple.episodenumber);
                    await AddEpisode(series, tuple.seasonNumber, tuple.episodenumber, cancellationToken).ConfigureAwait(false);

                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        /// <summary>
        /// Removes the virtual entry after a corresponding physical version has been added
        /// </summary>
        private bool RemoveObsoleteOrMissingEpisodes(
            IEnumerable<BaseItem> allRecursiveChildren,
            IEnumerable<(int seasonNumber, int episodeNumber, DateTime firstAired)> episodeLookup,
            bool allowMissingEpisodes)
        {
            var existingEpisodes = allRecursiveChildren.OfType<Episode>();

            var physicalEpisodes = new List<Episode>();
            var virtualEpisodes = new List<Episode>();
            foreach (var episode in existingEpisodes)
            {
                if (episode.LocationType == LocationType.Virtual)
                {
                    virtualEpisodes.Add(episode);
                }
                else
                {
                    physicalEpisodes.Add(episode);
                }
            }

            var episodesToRemove = virtualEpisodes
                .Where(i =>
                {
                    if (!i.IndexNumber.HasValue || !i.ParentIndexNumber.HasValue)
                    {
                        return true;
                    }

                    var seasonNumber = i.ParentIndexNumber.Value;
                    var episodeNumber = i.IndexNumber.Value;

                    // If there's a physical episode with the same season and episode number, delete it
                    if (physicalEpisodes.Any(p =>
                        p.ParentIndexNumber.HasValue && p.ParentIndexNumber.Value == seasonNumber &&
                        p.ContainsEpisodeNumber(episodeNumber)))
                    {
                        return true;
                    }

                    // If the episode no longer exists in the remote lookup, delete it
                    if (!episodeLookup.Any(e => e.seasonNumber == seasonNumber && e.episodeNumber == episodeNumber))
                    {
                        return true;
                    }

                    // If it's missing, but not unaired, remove it
                    return !allowMissingEpisodes && i.IsMissingEpisode &&
                           (!i.PremiereDate.HasValue ||
                            i.PremiereDate.Value.ToLocalTime().Date.AddDays(UnairedEpisodeThresholdDays) <
                            DateTime.Now.Date);
                });

            var hasChanges = false;

            foreach (var episodeToRemove in episodesToRemove)
            {
                _libraryManager.DeleteItem(episodeToRemove, new DeleteOptions
                {
                    DeleteFileLocation = true
                }, false);

                hasChanges = true;
            }

            return hasChanges;
        }

        /// <summary>
        /// Removes the obsolete or missing seasons.
        /// </summary>
        /// <param name="allRecursiveChildren"></param>
        /// <param name="episodeLookup">The episode lookup.</param>
        /// <returns><see cref="bool" />.</returns>
        private bool RemoveObsoleteOrMissingSeasons(
            IList<BaseItem> allRecursiveChildren,
            IEnumerable<(int seasonNumber, int episodeNumber, DateTime firstAired)> episodeLookup)
        {
            var existingSeasons = allRecursiveChildren.OfType<Season>().ToList();

            var physicalSeasons = new List<Season>();
            var virtualSeasons = new List<Season>();
            foreach (var season in existingSeasons)
            {
                if (season.LocationType == LocationType.Virtual)
                {
                    virtualSeasons.Add(season);
                }
                else
                {
                    physicalSeasons.Add(season);
                }
            }

            var allEpisodes = allRecursiveChildren.OfType<Episode>().ToList();

            var seasonsToRemove = virtualSeasons
                .Where(i =>
                {
                    if (i.IndexNumber.HasValue)
                    {
                        var seasonNumber = i.IndexNumber.Value;

                        // If there's a physical season with the same number, delete it
                        if (physicalSeasons.Any(p => p.IndexNumber.HasValue && p.IndexNumber.Value == seasonNumber && string.Equals(p.Series.PresentationUniqueKey, i.Series.PresentationUniqueKey, StringComparison.Ordinal)))
                        {
                            return true;
                        }

                        // If the season no longer exists in the remote lookup, delete it, but only if an existing episode doesn't require it
                        return episodeLookup.All(e => e.seasonNumber != seasonNumber) && allEpisodes.All(s => s.ParentIndexNumber != seasonNumber || s.IsInSeasonFolder);
                    }

                    // Season does not have a number
                    // Remove if there are no episodes directly in series without a season number
                    return allEpisodes.All(s => s.ParentIndexNumber.HasValue || s.IsInSeasonFolder);
                });

            var hasChanges = false;

            foreach (var seasonToRemove in seasonsToRemove)
            {
                _libraryManager.DeleteItem(seasonToRemove, new DeleteOptions
                {
                    DeleteFileLocation = true
                }, false);

                hasChanges = true;
            }

            return hasChanges;
        }

        /// <summary>
        /// Adds the episode.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="seasonNumber">The season number.</param>
        /// <param name="episodeNumber">The episode number.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task AddEpisode(Series series, int seasonNumber, int episodeNumber, CancellationToken cancellationToken)
        {
            var season = series.Children.OfType<Season>()
                .FirstOrDefault(i => i.IndexNumber.HasValue && i.IndexNumber.Value == seasonNumber);

            if (season == null)
            {
                var provider = new DummySeasonProvider(_logger, _localization, _libraryManager, _fileSystem);
                season = await provider.AddSeason(series, seasonNumber, true, cancellationToken).ConfigureAwait(false);
            }

            var name = "Episode " + episodeNumber.ToString(CultureInfo.InvariantCulture);

            var episode = new Episode
            {
                Name = name,
                IndexNumber = episodeNumber,
                ParentIndexNumber = seasonNumber,
                Id = _libraryManager.GetNewItemId(
                    series.Id + seasonNumber.ToString(CultureInfo.InvariantCulture) + name,
                    typeof(Episode)),
                IsVirtualItem = true,
                SeasonId = season?.Id ?? Guid.Empty,
                SeriesId = series.Id
            };

            season.AddChild(episode, cancellationToken);

            await episode.RefreshMetadata(new MetadataRefreshOptions(new DirectoryService(_fileSystem)), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the existing episode.
        /// </summary>
        /// <param name="existingEpisodes">The existing episodes.</param>
        /// <param name="seasonCounts"></param>
        /// <param name="episodeTuple"></param>
        /// <returns>Episode.</returns>
        private Episode GetExistingEpisode(IEnumerable<Episode> existingEpisodes, IReadOnlyDictionary<int, int> seasonCounts, (int seasonNumber, int episodeNumber, DateTime firstAired) episodeTuple)
        {
            var seasonNumber = episodeTuple.seasonNumber;
            var episodeNumber = episodeTuple.episodeNumber;

            while (true)
            {
                var episode = GetExistingEpisode(existingEpisodes, seasonNumber, episodeNumber);
                if (episode != null)
                {
                    return episode;
                }

                seasonNumber--;

                if (seasonCounts.ContainsKey(seasonNumber))
                {
                    episodeNumber += seasonCounts[seasonNumber];
                }
                else
                {
                    break;
                }
            }

            return null;
        }

        private Episode GetExistingEpisode(IEnumerable<Episode> existingEpisodes, int season, int episode)
            => existingEpisodes.FirstOrDefault(i => i.ParentIndexNumber == season && i.ContainsEpisodeNumber(episode));
    }
}
