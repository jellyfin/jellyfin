#pragma warning disable CS1591

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
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

        /// <inheritdoc />
        protected override async Task AfterMetadataRefresh(Series item, MetadataRefreshOptions refreshOptions, CancellationToken cancellationToken)
        {
            await base.AfterMetadataRefresh(item, refreshOptions, cancellationToken).ConfigureAwait(false);

            RemoveObsoleteSeasons(item);
            await FillInMissingSeasonsAsync(item, cancellationToken).ConfigureAwait(false);
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
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);

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

            if (replaceData || targetItem.AirDays == null || targetItem.AirDays.Length == 0)
            {
                targetItem.AirDays = sourceItem.AirDays;
            }
        }

        private void RemoveObsoleteSeasons(Series series)
        {
            // TODO Legacy. It's not really "physical" seasons as any virtual seasons are always converted to non-virtual in FillInMissingSeasonsAsync.
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
                    || !virtualSeason.GetEpisodes().Any())
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

        /// <summary>
        /// Creates seasons for all episodes that aren't in a season folder.
        /// If no season number can be determined, a dummy season will be created.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The async task.</returns>
        private async Task FillInMissingSeasonsAsync(Series series, CancellationToken cancellationToken)
        {
            var episodesInSeriesFolder = series.GetRecursiveChildren(i => i is Episode)
                .Cast<Episode>()
                .Where(i => !i.IsInSeasonFolder);

            List<Season> seasons = series.Children.OfType<Season>().ToList();

            // Loop through the unique season numbers
            foreach (var episode in episodesInSeriesFolder)
            {
                // Null season numbers will have a 'dummy' season created because seasons are always required.
                var seasonNumber = episode.ParentIndexNumber >= 0 ? episode.ParentIndexNumber : null;
                var existingSeason = seasons.FirstOrDefault(i => i.IndexNumber == seasonNumber);

                if (existingSeason == null)
                {
                    var season = await CreateSeasonAsync(series, seasonNumber, cancellationToken).ConfigureAwait(false);
                    seasons.Add(season);
                }
                else if (existingSeason.IsVirtualItem)
                {
                    existingSeason.IsVirtualItem = false;
                    await existingSeason.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Creates a new season, adds it to the database by linking it to the [series] and refreshes the metadata.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="seasonNumber">The season number.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The newly created season.</returns>
        private async Task<Season> CreateSeasonAsync(
            Series series,
            int? seasonNumber,
            CancellationToken cancellationToken)
        {
            string seasonName = seasonNumber switch
            {
                null => _localizationManager.GetLocalizedString("NameSeasonUnknown"),
                0 => LibraryManager.GetLibraryOptions(series).SeasonZeroDisplayName,
                _ => string.Format(CultureInfo.InvariantCulture, _localizationManager.GetLocalizedString("NameSeasonNumber"), seasonNumber.Value)
            };

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

            series.AddChild(season, cancellationToken);

            await season.RefreshMetadata(new MetadataRefreshOptions(new DirectoryService(FileSystem)), cancellationToken).ConfigureAwait(false);

            return season;
        }
    }
}
