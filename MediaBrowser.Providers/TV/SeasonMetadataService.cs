using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Manager;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Providers.TV
{
    public class SeasonMetadataService : MetadataService<Season, SeasonInfo>
    {
        public SeasonMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IProviderRepository providerRepo, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, providerRepo, fileSystem, userDataManager, libraryManager)
        {
        }

        protected override async Task<ItemUpdateType> BeforeSave(Season item, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = await base.BeforeSave(item, isFullRefresh, currentUpdateType).ConfigureAwait(false);

            if (item.IndexNumber.HasValue && item.IndexNumber.Value == 0)
            {
                if (!string.Equals(item.Name, ServerConfigurationManager.Configuration.SeasonZeroDisplayName, StringComparison.OrdinalIgnoreCase))
                {
                    item.Name = ServerConfigurationManager.Configuration.SeasonZeroDisplayName;
                    updateType = updateType | ItemUpdateType.MetadataEdit;
                }
            }

            return updateType;
        }

        protected override void MergeData(MetadataResult<Season> source, MetadataResult<Season> target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);
        }
    }
}
