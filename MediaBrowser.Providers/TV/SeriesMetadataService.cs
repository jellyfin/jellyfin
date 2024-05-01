#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.TV
{
    public class SeriesMetadataService : MetadataService<Series, SeriesInfo>
    {
        private readonly ILocalizationManager _localizationManager;

        public SeriesMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<SeriesMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager,
            ILocalizationManager localizationManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
            _localizationManager = localizationManager;
        }

        public override async Task<ItemUpdateType> RefreshMetadata(BaseItem item, MetadataRefreshOptions refreshOptions, CancellationToken cancellationToken)
        {
            if (item is Series series)
            {
                var seasons = series.GetRecursiveChildren(i => i is Season).ToList();

                foreach (var season in seasons)
                {
                    var hasUpdate = refreshOptions != null && season.BeforeMetadataRefresh(refreshOptions.ReplaceAllMetadata);
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

            RemoveObsoleteEpisodes(item);
            RemoveObsoleteSeasons(item);
            await UpdateAndCreateSeasonsAsync(item, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        protected override bool IsFullLocalMetadata(Series item)
        {
            if (string.IsNullOrWhiteSpace(item.Overview))
            {
                return false;
            }

            if (!item.ProductionYear.HasValue)
            {
                return false;
            }

            return base.IsFullLocalMetadata(item);
        }

        /// <inheritdoc />
        protected override void MergeData(MetadataResult<Series> source, MetadataResult<Series> target, MetadataField[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            base.MergeData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;
            var sourceSeasonNames = sourceItem.SeasonNames;
            var targetSeasonNames = targetItem.SeasonNames;

            if (replaceData || targetSeasonNames.Count == 0)
            {
                targetItem.SeasonNames = sourceSeasonNames;
            }
            else if (targetSeasonNames.Count != sourceSeasonNames.Count || !sourceSeasonNames.Keys.All(targetSeasonNames.ContainsKey))
            {
                foreach (var (number, name) in sourceSeasonNames)
                {
                    targetSeasonNames.TryAdd(number, name);
                }
            }

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
            // TODO Legacy. It's not really "physical" seasons as any virtual seasons are always converted to non-virtual in UpdateAndCreateSeasonsAsync.
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
                            DeleteFileLocation = true
                        },
                        false);
                }
            }
        }

        private void RemoveObsoleteEpisodes(Series series)
        {
            var episodes = series.GetEpisodes(null, new DtoOptions()).OfType<Episode>().ToList();
            var numberOfEpisodes = episodes.Count;
            // TODO: O(n^2), but can it be done faster without overcomplicating it?
            for (var i = 0; i < numberOfEpisodes; i++)
            {
                var currentEpisode = episodes[i];
                // The outer loop only examines virtual episodes
                if (!currentEpisode.IsVirtualItem)
                {
                    continue;
                }

                // Virtual episodes without an episode number are practically orphaned and should be deleted
                if (!currentEpisode.IndexNumber.HasValue)
                {
                    DeleteEpisode(currentEpisode);
                    continue;
                }

                for (var j = i + 1; j < numberOfEpisodes; j++)
                {
                    var comparisonEpisode = episodes[j];
                    // The inner loop is only for "physical" episodes
                    if (comparisonEpisode.IsVirtualItem
                        || currentEpisode.ParentIndexNumber != comparisonEpisode.ParentIndexNumber
                        || !comparisonEpisode.ContainsEpisodeNumber(currentEpisode.IndexNumber.Value))
                    {
                        continue;
                    }

                    DeleteEpisode(currentEpisode);
                    break;
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
                    DeleteFileLocation = true
                },
                false);
        }

        /// <summary>
        /// Creates seasons for all episodes if they don't exist.
        /// If no season number can be determined, a dummy season will be created.
        /// Updates seasons names.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The async task.</returns>
        private async Task UpdateAndCreateSeasonsAsync(Series series, CancellationToken cancellationToken)
        {
            var seasonNames = series.SeasonNames;
            var seriesChildren = series.GetRecursiveChildren(i => i is Episode || i is Season);
            var seasons = seriesChildren.OfType<Season>().ToList();
            var uniqueSeasonNumbers = seriesChildren
                .OfType<Episode>()
                .Select(e => e.ParentIndexNumber >= 0 ? e.ParentIndexNumber : null)
                .Distinct();

            // Loop through the unique season numbers
            foreach (var seasonNumber in uniqueSeasonNumbers)
            {
                // Null season numbers will have a 'dummy' season created because seasons are always required.
                var existingSeason = seasons.FirstOrDefault(i => i.IndexNumber == seasonNumber);

                if (!seasonNumber.HasValue || !seasonNames.TryGetValue(seasonNumber.Value, out var seasonName))
                {
                    seasonName = GetValidSeasonNameForSeries(series, null, seasonNumber);
                }

                if (existingSeason is null)
                {
                    var season = await CreateSeasonAsync(series, seasonName, seasonNumber, cancellationToken).ConfigureAwait(false);
                    series.AddChild(season);
                }
                else if (!string.Equals(existingSeason.Name, seasonName, StringComparison.Ordinal))
                {
                    existingSeason.Name = seasonName;
                    await existingSeason.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
                }
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
                SeriesName = series.Name
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
}
