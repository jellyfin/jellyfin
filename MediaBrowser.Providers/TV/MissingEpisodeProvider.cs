using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Xml;
using MediaBrowser.Providers.TV.TheTVDB;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.TV
{
    public class MissingEpisodeProvider
    {
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly ILocalizationManager _localization;
        private readonly IFileSystem _fileSystem;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public MissingEpisodeProvider(ILogger logger, IServerConfigurationManager config, ILibraryManager libraryManager, ILocalizationManager localization, IFileSystem fileSystem)
        {
            _logger = logger;
            _config = config;
            _libraryManager = libraryManager;
            _localization = localization;
            _fileSystem = fileSystem;
        }

        public async Task<bool> Run(Series series, bool addNewItems, CancellationToken cancellationToken)
        {
            var tvdbId = series.GetProviderId(MetadataProviders.Tvdb);

            // Todo: Support series by imdb id
            var seriesProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [MetadataProviders.Tvdb.ToString()] = tvdbId
            };

            var episodes = await TvDbClientManager.Instance.GetAllEpisodesAsync(Convert.ToInt32(tvdbId), cancellationToken);

            var episodeLookup = episodes
                .Select(i =>
                {
                    DateTime.TryParse(i.FirstAired, out var firstAired);
                    return new ValueTuple<int, int, DateTime>(
                        i.AiredSeason.GetValueOrDefault(-1), i.AiredEpisodeNumber.GetValueOrDefault(-1), firstAired);
                })
                .Where(i => i.Item2 != -1 && i.Item2 != -1)
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

        private const double UnairedEpisodeThresholdDays = 2;


        private async Task<bool> AddMissingEpisodes(
            Series series,
            IList<BaseItem> allItems,
            bool addMissingEpisodes,
            List<ValueTuple<int, int, DateTime>> episodeLookup,
            CancellationToken cancellationToken)
        {
            var existingEpisodes = allItems.OfType<Episode>()
                                   .ToList();

            var seasonCounts = (from e in episodeLookup
                                group e by e.Item1 into g
                                select g)
                               .ToDictionary(g => g.Key, g => g.Count());

            var hasChanges = false;

            foreach (var tuple in episodeLookup)
            {
                if (tuple.Item1 <= 0)
                {
                    // Ignore season zeros
                    continue;
                }

                if (tuple.Item2 <= 0)
                {
                    // Ignore episode zeros
                    continue;
                }

                var existingEpisode = GetExistingEpisode(existingEpisodes, seasonCounts, tuple);

                if (existingEpisode != null)
                {
                    continue;
                }

                var airDate = tuple.Item3;

                var now = DateTime.UtcNow.AddDays(0 - UnairedEpisodeThresholdDays);

                if (airDate < now && addMissingEpisodes || airDate > now)
                {
                    // tvdb has a lot of nearly blank episodes
                    _logger.LogInformation("Creating virtual missing/unaired episode {0} {1}x{2}", series.Name, tuple.Item1, tuple.Item2);
                    await AddEpisode(series, tuple.Item1, tuple.Item2, cancellationToken).ConfigureAwait(false);

                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        /// <summary>
        /// Removes the virtual entry after a corresponding physical version has been added
        /// </summary>
        private bool RemoveObsoleteOrMissingEpisodes(
            IList<BaseItem> allRecursiveChildren,
            IEnumerable<ValueTuple<int, int, DateTime>> episodeLookup,
            bool allowMissingEpisodes)
        {
            var existingEpisodes = allRecursiveChildren.OfType<Episode>()
                                   .ToList();

            var physicalEpisodes = existingEpisodes
                .Where(i => i.LocationType != LocationType.Virtual)
                .ToList();

            var virtualEpisodes = existingEpisodes
                .Where(i => i.LocationType == LocationType.Virtual)
                .ToList();

            var episodesToRemove = virtualEpisodes
                .Where(i =>
                {
                    if (i.IndexNumber.HasValue && i.ParentIndexNumber.HasValue)
                    {
                        var seasonNumber = i.ParentIndexNumber.Value;
                        var episodeNumber = i.IndexNumber.Value;

                        // If there's a physical episode with the same season and episode number, delete it
                        if (physicalEpisodes.Any(p =>
                                p.ParentIndexNumber.HasValue && (p.ParentIndexNumber.Value) == seasonNumber &&
                                p.ContainsEpisodeNumber(episodeNumber)))
                        {
                            return true;
                        }

                        // If the episode no longer exists in the remote lookup, delete it
                        if (!episodeLookup.Any(e => e.Item1 == seasonNumber && e.Item2 == episodeNumber))
                        {
                            return true;
                        }

                        if (!allowMissingEpisodes && i.IsMissingEpisode)
                        {
                            // If it's missing, but not unaired, remove it
                            if (!i.PremiereDate.HasValue || i.PremiereDate.Value.ToLocalTime().Date.AddDays(UnairedEpisodeThresholdDays) < DateTime.Now.Date)
                            {
                                return true;
                            }
                        }

                        return false;
                    }

                    return true;
                })
                .ToList();

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
        /// <returns>Task{System.Boolean}.</returns>
        private bool RemoveObsoleteOrMissingSeasons(IList<BaseItem> allRecursiveChildren,
            IEnumerable<(int, int, DateTime)> episodeLookup)
        {
            var existingSeasons = allRecursiveChildren.OfType<Season>().ToList();

            var physicalSeasons = existingSeasons
                .Where(i => i.LocationType != LocationType.Virtual)
                .ToList();

            var virtualSeasons = existingSeasons
                .Where(i => i.LocationType == LocationType.Virtual)
                .ToList();

            var allEpisodes = allRecursiveChildren.OfType<Episode>().ToList();

            var seasonsToRemove = virtualSeasons
                .Where(i =>
                {
                    if (i.IndexNumber.HasValue)
                    {
                        var seasonNumber = i.IndexNumber.Value;

                        // If there's a physical season with the same number, delete it
                        if (physicalSeasons.Any(p => p.IndexNumber.HasValue && (p.IndexNumber.Value) == seasonNumber && string.Equals(p.Series.PresentationUniqueKey, i.Series.PresentationUniqueKey, StringComparison.Ordinal)))
                        {
                            return true;
                        }

                        // If the season no longer exists in the remote lookup, delete it, but only if an existing episode doesn't require it
                        if (episodeLookup.All(e => e.Item1 != seasonNumber))
                        {
                            if (allEpisodes.All(s => s.ParentIndexNumber != seasonNumber || s.IsInSeasonFolder))
                            {
                                return true;
                            }
                        }

                        return false;
                    }

                    // Season does not have a number
                    // Remove if there are no episodes directly in series without a season number
                    return allEpisodes.All(s => s.ParentIndexNumber.HasValue || s.IsInSeasonFolder);
                })
                .ToList();

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
                var provider = new DummySeasonProvider(_config, _logger, _localization, _libraryManager, _fileSystem);
                season = await provider.AddSeason(series, seasonNumber, true, cancellationToken).ConfigureAwait(false);
            }

            var name = $"Episode {episodeNumber.ToString(_usCulture)}";

            var episode = new Episode
            {
                Name = name,
                IndexNumber = episodeNumber,
                ParentIndexNumber = seasonNumber,
                Id = _libraryManager.GetNewItemId((series.Id + seasonNumber.ToString(_usCulture) + name), typeof(Episode)),
                IsVirtualItem = true,
                SeasonId = season?.Id ?? Guid.Empty,
                SeriesId = series.Id
            };

            episode.SetParent(season);

            season.AddChild(episode, cancellationToken);

            await episode.RefreshMetadata(new MetadataRefreshOptions(new DirectoryService(_logger, _fileSystem)), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the existing episode.
        /// </summary>
        /// <param name="existingEpisodes">The existing episodes.</param>
        /// <param name="seasonCounts"></param>
        /// <param name="tuple">The tuple.</param>
        /// <returns>Episode.</returns>
        private Episode GetExistingEpisode(IList<Episode> existingEpisodes, Dictionary<int, int> seasonCounts, ValueTuple<int, int, DateTime> tuple)
        {
            var s = tuple.Item1;
            var e = tuple.Item2;

            while (true)
            {
                var episode = GetExistingEpisode(existingEpisodes, s, e);
                if (episode != null)
                    return episode;

                s--;

                if (seasonCounts.ContainsKey(s))
                    e += seasonCounts[s];
                else
                    break;
            }

            return null;
        }

        private Episode GetExistingEpisode(IEnumerable<Episode> existingEpisodes, int season, int episode)
        {
            return existingEpisodes
                .FirstOrDefault(i => i.ParentIndexNumber == season && i.ContainsEpisodeNumber(episode));
        }
    }
}
