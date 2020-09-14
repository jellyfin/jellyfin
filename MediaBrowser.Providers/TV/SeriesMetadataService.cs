#pragma warning disable CS1591

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.TV
{
    public class SeriesMetadataService : MetadataService<Series, SeriesInfo>
    {
        private readonly ILocalizationManager _localization;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public SeriesMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<SeriesMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager,
            ILocalizationManager localization)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
            _localization = localization;
            _logger = logger;
            _fileSystem = fileSystem;
        }

        /// <inheritdoc />
        protected override async Task AfterMetadataRefresh(Series item, MetadataRefreshOptions refreshOptions, CancellationToken cancellationToken)
        {
            await base.AfterMetadataRefresh(item, refreshOptions, cancellationToken).ConfigureAwait(false);

            var seasonProvider = new DummySeasonProvider(Logger, _localization, LibraryManager, FileSystem);
            await seasonProvider.Run(item, cancellationToken).ConfigureAwait(false);

            var libraryOptions = LibraryManager.GetLibraryOptions(item);

            var providers = ((ProviderManager)ProviderManager).GetMetadataProviders<Series>(item, libraryOptions)
                .OfType<IMissingEpisodesProvider>().ToList();
            var missingEpisodeProvider = new MissingEpisodeProvider(
                Logger,
                LibraryManager,
                _localization,
                FileSystem);

            var episodes = new List<MissingEpisodeInfo>();
            foreach (var provider in providers)
            {
                if (item.IsMetadataFetcherEnabled(LibraryManager.GetLibraryOptions(item), provider.Name))
                {
                    Logger.LogDebug("Running {0} for MissingEpisodeInfo", provider.GetType().Name);
                    var providerEpisodes = await provider.GetAllEpisodes(item, cancellationToken).ConfigureAwait(false);

                    episodes.AddRange(
                        providerEpisodes
                        .Where(
                            i => !episodes.Any(p => p.episodeNumber == i.episodeNumber && p.seasonNumber == i.seasonNumber) // Ignore duplicates
                            )
                        );
                }
            }

            episodes = episodes.OrderBy(i => i.seasonNumber)
                               .ThenBy(i => i.episodeNumber)
                               .ToList();

            try
            {
                await missingEpisodeProvider.Run(item, episodes, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in DummySeasonProvider for {ItemPath}", item.Path);
            }
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
    }
}
