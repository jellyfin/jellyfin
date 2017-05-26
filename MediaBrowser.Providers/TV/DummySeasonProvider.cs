using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Globalization;

namespace MediaBrowser.Providers.TV
{
    public class DummySeasonProvider
    {
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly ILocalizationManager _localization;
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public DummySeasonProvider(IServerConfigurationManager config, ILogger logger, ILocalizationManager localization, ILibraryManager libraryManager, IFileSystem fileSystem)
        {
            _config = config;
            _logger = logger;
            _localization = localization;
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
        }

        public async Task Run(Series series, CancellationToken cancellationToken)
        {
            await RemoveObsoleteSeasons(series).ConfigureAwait(false);
            
            var hasNewSeasons = await AddDummySeasonFolders(series, cancellationToken).ConfigureAwait(false);

            if (hasNewSeasons)
            {
                //var directoryService = new DirectoryService(_fileSystem);

                //await series.RefreshMetadata(new MetadataRefreshOptions(directoryService), cancellationToken).ConfigureAwait(false);

                //await series.ValidateChildren(new Progress<double>(), cancellationToken, new MetadataRefreshOptions(directoryService))
                //    .ConfigureAwait(false);
            }
        }

        private async Task<bool> AddDummySeasonFolders(Series series, CancellationToken cancellationToken)
        {
            var episodesInSeriesFolder = series.GetRecursiveChildren(i => i is Episode)
                .Cast<Episode>()
                .Where(i => !i.IsInSeasonFolder)
                .ToList();

            var hasChanges = false;

            // Loop through the unique season numbers
            foreach (var seasonNumber in episodesInSeriesFolder.Select(i => i.ParentIndexNumber ?? -1)
                .Where(i => i >= 0)
                .Distinct()
                .ToList())
            {
                var existingSeason = series.Children.OfType<Season>()
                    .FirstOrDefault(i => i.IndexNumber.HasValue && i.IndexNumber.Value == seasonNumber);

                if (existingSeason == null)
                {
                    await AddSeason(series, seasonNumber, false, cancellationToken).ConfigureAwait(false);

                    hasChanges = true;
                }
                else if (existingSeason.IsVirtualItem)
                {
                    existingSeason.IsVirtualItem = false;
                    await existingSeason.UpdateToRepository(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
                }
            }

            // Unknown season - create a dummy season to put these under
            if (episodesInSeriesFolder.Any(i => !i.ParentIndexNumber.HasValue))
            {
                var existingSeason = series.Children.OfType<Season>()
                    .FirstOrDefault(i => !i.IndexNumber.HasValue);

                if (existingSeason == null)
                {
                    await AddSeason(series, null, false, cancellationToken).ConfigureAwait(false);

                    hasChanges = true;
                }
                else if (existingSeason.IsVirtualItem)
                {
                    existingSeason.IsVirtualItem = false;
                    await existingSeason.UpdateToRepository(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
                }
            }

            return hasChanges;
        }

        /// <summary>
        /// Adds the season.
        /// </summary>
        public async Task<Season> AddSeason(Series series,
            int? seasonNumber,
            bool isVirtualItem,
            CancellationToken cancellationToken)
        {
            var seasonName = seasonNumber == 0 ?
                _config.Configuration.SeasonZeroDisplayName :
                (seasonNumber.HasValue ? string.Format(_localization.GetLocalizedString("NameSeasonNumber"), seasonNumber.Value.ToString(_usCulture)) : _localization.GetLocalizedString("NameSeasonUnknown"));

            _logger.Info("Creating Season {0} entry for {1}", seasonName, series.Name);

            var season = new Season
            {
                Name = seasonName,
                IndexNumber = seasonNumber,
                Id = _libraryManager.GetNewItemId((series.Id + (seasonNumber ?? -1).ToString(_usCulture) + seasonName), typeof(Season)),
                IsVirtualItem = isVirtualItem,
                SeriesId = series.Id,
                SeriesName = series.Name
            };

            season.SetParent(series);
            
            await series.AddChild(season, cancellationToken).ConfigureAwait(false);

            await season.RefreshMetadata(new MetadataRefreshOptions(_fileSystem), cancellationToken).ConfigureAwait(false);

            return season;
        }

        private async Task<bool> RemoveObsoleteSeasons(Series series)
        {
            var existingSeasons = series.Children.OfType<Season>().ToList();

            var physicalSeasons = existingSeasons
                .Where(i => i.LocationType != LocationType.Virtual)
                .ToList();

            var virtualSeasons = existingSeasons
                .Where(i => i.LocationType == LocationType.Virtual)
                .ToList();

            var seasonsToRemove = virtualSeasons
                .Where(i =>
                {
                    if (i.IndexNumber.HasValue)
                    {
                        var seasonNumber = i.IndexNumber.Value;

                        // If there's a physical season with the same number, delete it
                        if (physicalSeasons.Any(p => p.IndexNumber.HasValue && (p.IndexNumber.Value == seasonNumber)))
                        {
                            return true;
                        }
                    }

                    // If there are no episodes with this season number, delete it
                    if (!i.GetEpisodes().Any())
                    {
                        return true;
                    }

                    return false;
                })
                .ToList();

            var hasChanges = false;

            foreach (var seasonToRemove in seasonsToRemove)
            {
                _logger.Info("Removing virtual season {0} {1}", seasonToRemove.Series.Name, seasonToRemove.IndexNumber);

                await seasonToRemove.Delete(new DeleteOptions
                {
                    DeleteFileLocation = true

                }).ConfigureAwait(false);

                hasChanges = true;
            }

            return hasChanges;
        }
    }
}
