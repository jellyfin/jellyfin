using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Controller.Configuration;
using Jellyfin.Controller.Entities.TV;
using Jellyfin.Controller.Library;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.Entities;
using Jellyfin.Model.Globalization;
using Jellyfin.Model.IO;
using Jellyfin.Providers.Manager;
using Jellyfin.Providers.TV.TheTVDB;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Providers.TV
{
    public class SeriesMetadataService : MetadataService<Series, SeriesInfo>
    {
        private readonly ILocalizationManager _localization;
        private readonly TvDbClientManager _tvDbClientManager;

        public SeriesMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            IUserDataManager userDataManager,
            ILibraryManager libraryManager,
            ILocalizationManager localization,
            TvDbClientManager tvDbClientManager
            )
            : base(serverConfigurationManager, logger, providerManager, fileSystem, userDataManager, libraryManager)
        {
            _localization = localization;
            _tvDbClientManager = tvDbClientManager;
        }

        protected override async Task AfterMetadataRefresh(Series item, MetadataRefreshOptions refreshOptions, CancellationToken cancellationToken)
        {
            await base.AfterMetadataRefresh(item, refreshOptions, cancellationToken).ConfigureAwait(false);

            var seasonProvider = new DummySeasonProvider(ServerConfigurationManager, Logger, _localization, LibraryManager, FileSystem);
            await seasonProvider.Run(item, cancellationToken).ConfigureAwait(false);

            // TODO why does it not register this itself omg
            var provider = new MissingEpisodeProvider(Logger,
                ServerConfigurationManager,
                LibraryManager,
                _localization,
                FileSystem,
                _tvDbClientManager);

            try
            {
                await provider.Run(item, true, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in DummySeasonProvider");
            }
        }

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

        protected override void MergeData(MetadataResult<Series> source, MetadataResult<Series> target, MetadataFields[] lockedFields, bool replaceData, bool mergeMetadataSettings)
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
