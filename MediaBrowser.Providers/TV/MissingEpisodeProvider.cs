using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.TV
{
    class MissingEpisodeProvider
    {
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        public MissingEpisodeProvider(ILogger logger, IServerConfigurationManager config, ILibraryManager libraryManager)
        {
            _logger = logger;
            _config = config;
            _libraryManager = libraryManager;
        }

        public async Task Run(IEnumerable<IGrouping<string, Series>> series, CancellationToken cancellationToken)
        {
            foreach (var seriesGroup in series)
            {
                try
                {
                    await Run(seriesGroup, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (DirectoryNotFoundException)
                {
                    _logger.Warn("Series files missing for series id {0}", seriesGroup.Key);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in missing episode provider for series id {0}", ex, seriesGroup.Key);
                }
            }
        }

        private async Task Run(IGrouping<string, Series> group, CancellationToken cancellationToken)
        {
            var tvdbId = group.Key;

            var seriesDataPath = TvdbSeriesProvider.GetSeriesDataPath(_config.ApplicationPaths, tvdbId);

            var episodeFiles = Directory.EnumerateFiles(seriesDataPath, "*.xml", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(i => i.StartsWith("episode-", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var episodeLookup = episodeFiles
                .Select(i =>
                {
                    var parts = i.Split('-');

                    if (parts.Length == 3)
                    {
                        int seasonNumber;

                        if (int.TryParse(parts[1], NumberStyles.Integer, UsCulture, out seasonNumber))
                        {
                            int episodeNumber;

                            if (int.TryParse(parts[2], NumberStyles.Integer, UsCulture, out episodeNumber))
                            {
                                return new Tuple<int, int>(seasonNumber, episodeNumber);
                            }
                        }
                    }

                    return new Tuple<int, int>(-1, -1);
                })
                .Where(i => i.Item1 != -1 && i.Item2 != -1)
                .ToList();

            var hasBadData = HasInvalidContent(group);

            var anySeasonsRemoved = await RemoveObsoleteOrMissingSeasons(group, episodeLookup, hasBadData)
                .ConfigureAwait(false);

            var anyEpisodesRemoved = await RemoveObsoleteOrMissingEpisodes(group, episodeLookup, hasBadData)
                .ConfigureAwait(false);

            var hasNewEpisodes = false;
            var hasNewSeasons = false;

            foreach (var series in group.Where(s => s.ContainsEpisodesWithoutSeasonFolders))
            {
                hasNewSeasons = await AddDummySeasonFolders(series, cancellationToken).ConfigureAwait(false);
            }

            if (_config.Configuration.EnableInternetProviders)
            {
                var seriesConfig = _config.Configuration.MetadataOptions.FirstOrDefault(i => string.Equals(i.ItemType, typeof(Series).Name, StringComparison.OrdinalIgnoreCase));

                if (seriesConfig == null || !seriesConfig.DisabledMetadataFetchers.Contains(TvdbSeriesProvider.Current.Name, StringComparer.OrdinalIgnoreCase))
                {
                    hasNewEpisodes = await AddMissingEpisodes(group.ToList(), hasBadData, seriesDataPath, episodeLookup, cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            if (hasNewSeasons || hasNewEpisodes || anySeasonsRemoved || anyEpisodesRemoved)
            {
                foreach (var series in group)
                {
                    await series.RefreshMetadata(new MetadataRefreshOptions
                    {
                    }, cancellationToken).ConfigureAwait(false);

                    await series.ValidateChildren(new Progress<double>(), cancellationToken, new MetadataRefreshOptions(), true)
                        .ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Returns true if a series has any seasons or episodes without season or episode numbers
        /// If this data is missing no virtual items will be added in order to prevent possible duplicates
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        private bool HasInvalidContent(IEnumerable<Series> group)
        {
            var allItems = group.ToList().SelectMany(i => i.RecursiveChildren).ToList();

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

        /// <summary>
        /// For series with episodes directly under the series folder, this adds dummy seasons to enable regular browsing and metadata
        /// </summary>
        /// <param name="series"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<bool> AddDummySeasonFolders(Series series, CancellationToken cancellationToken)
        {
            var existingEpisodes = series.RecursiveChildren
                .OfType<Episode>()
                .ToList();

            var hasChanges = false;

            // Loop through the unique season numbers
            foreach (var seasonNumber in existingEpisodes.Select(i => i.ParentIndexNumber ?? -1)
                .Where(i => i >= 0)
                .Distinct()
                .ToList())
            {
                var hasSeason = series.Children.OfType<Season>()
                    .Any(i => i.IndexNumber.HasValue && i.IndexNumber.Value == seasonNumber);

                if (!hasSeason)
                {
                    await AddSeason(series, seasonNumber, cancellationToken).ConfigureAwait(false);

                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        /// <summary>
        /// Adds the missing episodes.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="seriesHasBadData">if set to <c>true</c> [series has bad data].</param>
        /// <param name="seriesDataPath">The series data path.</param>
        /// <param name="episodeLookup">The episode lookup.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task<bool> AddMissingEpisodes(List<Series> series, 
            bool seriesHasBadData,
            string seriesDataPath, 
            IEnumerable<Tuple<int, int>> episodeLookup, 
            CancellationToken cancellationToken)
        {
            // Be conservative here to avoid creating missing episodes for ones they already have
            if (!seriesHasBadData)
            {
                return false;
            }

            var existingEpisodes = (from s in series
                                    let seasonOffset = TvdbSeriesProvider.GetSeriesOffset(s.ProviderIds) ?? ((s.AnimeSeriesIndex ?? 1) - 1)
                                    from c in s.RecursiveChildren.OfType<Episode>()
                                    select new Tuple<int, Episode>((c.ParentIndexNumber ?? 0) + seasonOffset, c))
                                   .ToList();

            var lookup = episodeLookup as IList<Tuple<int, int>> ?? episodeLookup.ToList();

            var seasonCounts = (from e in lookup
                                group e by e.Item1 into g select g)
                               .ToDictionary(g => g.Key, g => g.Count());

            var hasChanges = false;

            foreach (var tuple in lookup)
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

                var airDate = GetAirDate(seriesDataPath, tuple.Item1, tuple.Item2);

                if (!airDate.HasValue)
                {
                    continue;
                }
                var now = DateTime.UtcNow;

                var targetSeries = DetermineAppropriateSeries(series, tuple.Item1);
                var seasonOffset = TvdbSeriesProvider.GetSeriesOffset(targetSeries.ProviderIds) ?? ((targetSeries.AnimeSeriesIndex ?? 1) - 1);

                if (airDate.Value < now)
                {
                    // Be conservative here to avoid creating missing episodes for ones they already have
                    if (!seriesHasBadData)
                    {
                        // tvdb has a lot of nearly blank episodes
                        _logger.Info("Creating virtual missing episode {0} {1}x{2}", targetSeries.Name, tuple.Item1, tuple.Item2);
                        await AddEpisode(targetSeries, tuple.Item1 - seasonOffset, tuple.Item2, cancellationToken).ConfigureAwait(false);

                        hasChanges = true;
                    }
                }
                else if (airDate.Value > now)
                {
                    // tvdb has a lot of nearly blank episodes
                    _logger.Info("Creating virtual unaired episode {0} {1}x{2}", targetSeries.Name, tuple.Item1, tuple.Item2);
                    await AddEpisode(targetSeries, tuple.Item1 - seasonOffset, tuple.Item2, cancellationToken).ConfigureAwait(false);

                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        private Series DetermineAppropriateSeries(IEnumerable<Series> series, int seasonNumber)
        {
            var seriesAndOffsets = series.Select(s => new { Series = s, SeasonOffset = TvdbSeriesProvider.GetSeriesOffset(s.ProviderIds) ?? ((s.AnimeSeriesIndex ?? 1) - 1) }).ToList();

            var bestMatch = seriesAndOffsets.FirstOrDefault(s => s.Series.RecursiveChildren.OfType<Season>().Any(season => (season.IndexNumber + s.SeasonOffset) == seasonNumber)) ??
                            seriesAndOffsets.FirstOrDefault(s => s.Series.RecursiveChildren.OfType<Season>().Any(season => (season.IndexNumber + s.SeasonOffset) == 1)) ??
                            seriesAndOffsets.OrderBy(s => s.Series.RecursiveChildren.OfType<Season>().Select(season => season.IndexNumber + s.SeasonOffset).Min()).First();

            return bestMatch.Series;
        }
        
        /// <summary>
        /// Removes the virtual entry after a corresponding physical version has been added
        /// </summary>
        private async Task<bool> RemoveObsoleteOrMissingEpisodes(IEnumerable<Series> series, 
            IEnumerable<Tuple<int, int>> episodeLookup, 
            bool forceRemoveAll)
        {
            var existingEpisodes = (from s in series
                                    let seasonOffset = TvdbSeriesProvider.GetSeriesOffset(s.ProviderIds) ?? ((s.AnimeSeriesIndex ?? 1) - 1)
                                   from c in s.RecursiveChildren.OfType<Episode>()
                                   select new { SeasonOffset = seasonOffset, Episode = c })
                                   .ToList();

            var physicalEpisodes = existingEpisodes
                .Where(i => i.Episode.LocationType != LocationType.Virtual)
                .ToList();

            var virtualEpisodes = existingEpisodes
                .Where(i => i.Episode.LocationType == LocationType.Virtual)
                .ToList();

            var episodesToRemove = virtualEpisodes
                .Where(i =>
                {
                    if (forceRemoveAll)
                    {
                        return true;
                    }

                    if (i.Episode.IndexNumber.HasValue && i.Episode.ParentIndexNumber.HasValue)
                    {
                        var seasonNumber = i.Episode.ParentIndexNumber.Value + i.SeasonOffset;
                        var episodeNumber = i.Episode.IndexNumber.Value;

                        // If there's a physical episode with the same season and episode number, delete it
                        if (physicalEpisodes.Any(p =>
                                p.Episode.ParentIndexNumber.HasValue && (p.Episode.ParentIndexNumber.Value + p.SeasonOffset) == seasonNumber &&
                                p.Episode.ContainsEpisodeNumber(episodeNumber)))
                        {
                            return true;
                        }

                        // If the episode no longer exists in the remote lookup, delete it
                        if (!episodeLookup.Any(e => e.Item1 == seasonNumber && e.Item2 == episodeNumber))
                        {
                            return true;
                        }

                        return false;
                    }

                    return true;
                })
                .ToList();

            var hasChanges = false;

            foreach (var episodeToRemove in episodesToRemove.Select(e => e.Episode))
            {
                _logger.Info("Removing missing/unaired episode {0} {1}x{2}", episodeToRemove.Series.Name, episodeToRemove.ParentIndexNumber, episodeToRemove.IndexNumber);

                await _libraryManager.DeleteItem(episodeToRemove).ConfigureAwait(false);

                hasChanges = true;
            }

            return hasChanges;
        }

        /// <summary>
        /// Removes the obsolete or missing seasons.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="episodeLookup">The episode lookup.</param>
        /// <param name="forceRemoveAll">if set to <c>true</c> [force remove all].</param>
        /// <returns>Task{System.Boolean}.</returns>
        private async Task<bool> RemoveObsoleteOrMissingSeasons(IEnumerable<Series> series, 
            IEnumerable<Tuple<int, int>> episodeLookup,
            bool forceRemoveAll)
        {
            var existingSeasons = (from s in series
                                   let seasonOffset = TvdbSeriesProvider.GetSeriesOffset(s.ProviderIds) ?? ((s.AnimeSeriesIndex ?? 1) - 1)
                                   from c in s.Children.OfType<Season>()
                                   select new { SeasonOffset = seasonOffset, Season = c })
                                   .ToList();

            var physicalSeasons = existingSeasons
                .Where(i => i.Season.LocationType != LocationType.Virtual)
                .ToList();

            var virtualSeasons = existingSeasons
                .Where(i => i.Season.LocationType == LocationType.Virtual)
                .ToList();

            var seasonsToRemove = virtualSeasons
                .Where(i =>
                {
                    if (forceRemoveAll)
                    {
                        return true;
                    }

                    if (i.Season.IndexNumber.HasValue)
                    {
                        var seasonNumber = i.Season.IndexNumber.Value + i.SeasonOffset;

                        // If there's a physical season with the same number, delete it
                        if (physicalSeasons.Any(p => p.Season.IndexNumber.HasValue && (p.Season.IndexNumber.Value + p.SeasonOffset) == seasonNumber))
                        {
                            return true;
                        }

                        // If the season no longer exists in the remote lookup, delete it
                        if (episodeLookup.All(e => e.Item1 != seasonNumber))
                        {
                            return true;
                        }

                        return false;
                    }

                    return true;
                })
                .ToList();

            var hasChanges = false;

            foreach (var seasonToRemove in seasonsToRemove.Select(s => s.Season))
            {
                _logger.Info("Removing virtual season {0} {1}", seasonToRemove.Series.Name, seasonToRemove.IndexNumber);

                await _libraryManager.DeleteItem(seasonToRemove).ConfigureAwait(false);

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
                season = await AddSeason(series, seasonNumber, cancellationToken).ConfigureAwait(false);
            }

            var name = string.Format("Episode {0}", episodeNumber.ToString(UsCulture));

            var episode = new Episode
            {
                Name = name,
                IndexNumber = episodeNumber,
                ParentIndexNumber = seasonNumber,
                Parent = season,
                DisplayMediaType = typeof(Episode).Name,
                Id = (series.Id + seasonNumber.ToString(UsCulture) + name).GetMBId(typeof(Episode))
            };

            await season.AddChild(episode, cancellationToken).ConfigureAwait(false);

            await episode.RefreshMetadata(new MetadataRefreshOptions
            {
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds the season.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="seasonNumber">The season number.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Season}.</returns>
        private async Task<Season> AddSeason(Series series, int seasonNumber, CancellationToken cancellationToken)
        {
            _logger.Info("Creating Season {0} entry for {1}", seasonNumber, series.Name);

            var name = seasonNumber == 0 ? _config.Configuration.SeasonZeroDisplayName : string.Format("Season {0}", seasonNumber.ToString(UsCulture));

            var season = new Season
            {
                Name = name,
                IndexNumber = seasonNumber,
                Parent = series,
                DisplayMediaType = typeof(Season).Name,
                Id = (series.Id + seasonNumber.ToString(UsCulture) + name).GetMBId(typeof(Season))
            };

            await series.AddChild(season, cancellationToken).ConfigureAwait(false);

            await season.RefreshMetadata(new MetadataRefreshOptions(), cancellationToken).ConfigureAwait(false);

            return season;
        }

        /// <summary>
        /// Gets the existing episode.
        /// </summary>
        /// <param name="existingEpisodes">The existing episodes.</param>
        /// <param name="seasonCounts"></param>
        /// <param name="tuple">The tuple.</param>
        /// <returns>Episode.</returns>
        private Episode GetExistingEpisode(IList<Tuple<int, Episode>> existingEpisodes, Dictionary<int, int> seasonCounts, Tuple<int, int> tuple)
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

        private static Episode GetExistingEpisode(IEnumerable<Tuple<int, Episode>> existingEpisodes, int season, int episode)
        {
            return existingEpisodes
                .Where(i => i.Item1 == season && i.Item2.ContainsEpisodeNumber(episode))
                .Select(i => i.Item2)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the air date.
        /// </summary>
        /// <param name="seriesDataPath">The series data path.</param>
        /// <param name="seasonNumber">The season number.</param>
        /// <param name="episodeNumber">The episode number.</param>
        /// <returns>System.Nullable{DateTime}.</returns>
        private DateTime? GetAirDate(string seriesDataPath, int seasonNumber, int episodeNumber)
        {
            // First open up the tvdb xml file and make sure it has valid data
            var filename = string.Format("episode-{0}-{1}.xml", seasonNumber.ToString(UsCulture), episodeNumber.ToString(UsCulture));

            var xmlPath = Path.Combine(seriesDataPath, filename);

            DateTime? airDate = null;

            // It appears the best way to filter out invalid entries is to only include those with valid air dates
            using (var streamReader = new StreamReader(xmlPath, Encoding.UTF8))
            {
                // Use XmlReader for best performance
                using (var reader = XmlReader.Create(streamReader, new XmlReaderSettings
                {
                    CheckCharacters = false,
                    IgnoreProcessingInstructions = true,
                    IgnoreComments = true,
                    ValidationType = ValidationType.None
                }))
                {
                    reader.MoveToContent();

                    // Loop through each element
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.Name)
                            {
                                case "EpisodeName":
                                    {
                                        var val = reader.ReadElementContentAsString();
                                        if (string.IsNullOrWhiteSpace(val))
                                        {
                                            // Not valid, ignore these
                                            return null;
                                        }
                                        break;
                                    }
                                case "FirstAired":
                                    {
                                        var val = reader.ReadElementContentAsString();

                                        if (!string.IsNullOrWhiteSpace(val))
                                        {
                                            DateTime date;
                                            if (DateTime.TryParse(val, out date))
                                            {
                                                airDate = date.ToUniversalTime();
                                            }
                                        }

                                        break;
                                    }

                                default:
                                    reader.Skip();
                                    break;
                            }
                        }
                    }
                }
            }

            return airDate;
        }
    }
}
