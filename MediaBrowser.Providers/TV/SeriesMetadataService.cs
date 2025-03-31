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
    /// <summary>
    /// Service to manage series metadata.
    /// </summary>
    public class SeriesMetadataService : MetadataService<Series, SeriesInfo>
    {
        private readonly ILocalizationManager _localizationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesMetadataService"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{SeasonMetadataService}"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="localizationManager">Instance of the <see cref="ILocalizationManager"/> interface.</param>
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

        /// <inheritdoc />
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
            await CreateSeasonsAsync(item, cancellationToken).ConfigureAwait(false);
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
                if (existingSeason is null)
                {
                    var seasonName = GetValidSeasonNameForSeries(series, null, seasonNumber);
                    await CreateSeasonAsync(series, seasonName, seasonNumber, cancellationToken).ConfigureAwait(false);
                }
                else if (existingSeason.IsVirtualItem)
                {
                    var episodeCount = seriesChildren.OfType<Episode>().Count(e => e.ParentIndexNumber == seasonNumber && !e.IsMissingEpisode);
                    if (episodeCount > 0)
                    {
                        existingSeason.IsVirtualItem = false;
                        await existingSeason.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
                    }
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
        private async Task CreateSeasonAsync(
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
