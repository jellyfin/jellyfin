using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.TV
{
    public class DummySeasonProvider
    {
        private readonly ILogger _logger;
        private readonly ILocalizationManager _localization;
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;

        public DummySeasonProvider(
            ILogger logger,
            ILocalizationManager localization,
            ILibraryManager libraryManager,
            IFileSystem fileSystem)
        {
            _logger = logger;
            _localization = localization;
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
        }

        public async Task<bool> Run(Series series, CancellationToken cancellationToken)
        {
            var seasonsRemoved = RemoveObsoleteSeasons(series);

            var hasNewSeasons = await AddDummySeasonFolders(series, cancellationToken).ConfigureAwait(false);

            if (hasNewSeasons)
            {
                //var directoryService = new DirectoryService(_fileSystem);

                //await series.RefreshMetadata(new MetadataRefreshOptions(directoryService), cancellationToken).ConfigureAwait(false);

                //await series.ValidateChildren(new SimpleProgress<double>(), cancellationToken, new MetadataRefreshOptions(directoryService))
                //    .ConfigureAwait(false);
            }

            return seasonsRemoved || hasNewSeasons;
        }

        private async Task<bool> AddDummySeasonFolders(Series series, CancellationToken cancellationToken)
        {
            var episodesInSeriesFolder = series.GetRecursiveChildren(i => i is Episode)
                .Cast<Episode>()
                .Where(i => !i.IsInSeasonFolder)
                .ToList();

            var hasChanges = false;

            List<Season> seasons = null;

            // Loop through the unique season numbers
            foreach (var seasonNumber in episodesInSeriesFolder.Select(i => i.ParentIndexNumber ?? -1)
                .Where(i => i >= 0)
                .Distinct()
                .ToList())
            {
                if (seasons == null)
                {
                    seasons = series.Children.OfType<Season>().ToList();
                }
                var existingSeason = seasons
                    .FirstOrDefault(i => i.IndexNumber.HasValue && i.IndexNumber.Value == seasonNumber);

                if (existingSeason == null)
                {
                    await AddSeason(series, seasonNumber, false, cancellationToken).ConfigureAwait(false);
                    hasChanges = true;
                    seasons = null;
                }
                else if (existingSeason.IsVirtualItem)
                {
                    existingSeason.IsVirtualItem = false;
                    existingSeason.UpdateToRepository(ItemUpdateType.MetadataEdit, cancellationToken);
                    seasons = null;
                }
            }

            // Unknown season - create a dummy season to put these under
            if (episodesInSeriesFolder.Any(i => !i.ParentIndexNumber.HasValue))
            {
                if (seasons == null)
                {
                    seasons = series.Children.OfType<Season>().ToList();
                }

                var existingSeason = seasons
                    .FirstOrDefault(i => !i.IndexNumber.HasValue);

                if (existingSeason == null)
                {
                    await AddSeason(series, null, false, cancellationToken).ConfigureAwait(false);

                    hasChanges = true;
                    seasons = null;
                }
                else if (existingSeason.IsVirtualItem)
                {
                    existingSeason.IsVirtualItem = false;
                    existingSeason.UpdateToRepository(ItemUpdateType.MetadataEdit, cancellationToken);
                    seasons = null;
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
            string seasonName;
            if (seasonNumber == null)
            {
                seasonName = _localization.GetLocalizedString("NameSeasonUnknown");
            }
            else if (seasonNumber == 0)
            {
                seasonName = _libraryManager.GetLibraryOptions(series).SeasonZeroDisplayName;
            }
            else
            {
                seasonName = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("NameSeasonNumber"),
                    seasonNumber.Value);
            }

            _logger.LogInformation("Creating Season {0} entry for {1}", seasonName, series.Name);

            var season = new Season
            {
                Name = seasonName,
                IndexNumber = seasonNumber,
                Id = _libraryManager.GetNewItemId(
                    series.Id + (seasonNumber ?? -1).ToString(CultureInfo.InvariantCulture) + seasonName,
                    typeof(Season)),
                IsVirtualItem = isVirtualItem,
                SeriesId = series.Id,
                SeriesName = series.Name
            };

            season.SetParent(series);

            series.AddChild(season, cancellationToken);

            await season.RefreshMetadata(new MetadataRefreshOptions(new DirectoryService(_fileSystem)), cancellationToken).ConfigureAwait(false);

            return season;
        }

        private bool RemoveObsoleteSeasons(Series series)
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
                _logger.LogInformation("Removing virtual season {0} {1}", series.Name, seasonToRemove.IndexNumber);

                _libraryManager.DeleteItem(seasonToRemove, new DeleteOptions
                {
                    DeleteFileLocation = true

                }, false);

                hasChanges = true;
            }

            return hasChanges;
        }
    }
}
