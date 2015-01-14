using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.TV
{
    public class DummySeasonProvider
    {
        private readonly IServerConfigurationManager _config;
        private readonly ILogger _logger;
        private readonly ILocalizationManager _localization;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public DummySeasonProvider(IServerConfigurationManager config, ILogger logger, ILocalizationManager localization)
        {
            _config = config;
            _logger = logger;
            _localization = localization;
        }

        public async Task Run(Series series, CancellationToken cancellationToken)
        {
            var hasNewSeasons = await AddDummySeasonFolders(series, cancellationToken).ConfigureAwait(false);

            if (hasNewSeasons)
            {
                var directoryService = new DirectoryService();

                //await series.RefreshMetadata(new MetadataRefreshOptions(directoryService), cancellationToken).ConfigureAwait(false);

                //await series.ValidateChildren(new Progress<double>(), cancellationToken, new MetadataRefreshOptions(directoryService))
                //    .ConfigureAwait(false);
            }
        }

        private async Task<bool> AddDummySeasonFolders(Series series, CancellationToken cancellationToken)
        {
            var episodesInSeriesFolder = series.RecursiveChildren
                .OfType<Episode>()
                .Where(i => !i.IsInSeasonFolder)
                .ToList();

            var hasChanges = false;

            // Loop through the unique season numbers
            foreach (var seasonNumber in episodesInSeriesFolder.Select(i => i.ParentIndexNumber ?? -1)
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

            // Unknown season - create a dummy season to put these under
            if (episodesInSeriesFolder.Any(i => !i.ParentIndexNumber.HasValue))
            {
                var hasSeason = series.Children.OfType<Season>()
                    .Any(i => !i.IndexNumber.HasValue);

                if (!hasSeason)
                {
                    await AddSeason(series, null, cancellationToken).ConfigureAwait(false);

                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        /// <summary>
        /// Adds the season.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="seasonNumber">The season number.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Season}.</returns>
        public async Task<Season> AddSeason(Series series,
            int? seasonNumber,
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
                Parent = series,
                DisplayMediaType = typeof(Season).Name,
                Id = (series.Id + (seasonNumber ?? -1).ToString(_usCulture) + seasonName).GetMBId(typeof(Season))
            };

            await series.AddChild(season, cancellationToken).ConfigureAwait(false);

            await season.RefreshMetadata(new MetadataRefreshOptions(), cancellationToken).ConfigureAwait(false);

            return season;
        }
    }
}
