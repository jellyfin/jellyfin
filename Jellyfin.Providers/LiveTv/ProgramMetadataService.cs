using Jellyfin.Controller.Configuration;
using Jellyfin.Controller.Library;
using Jellyfin.Controller.LiveTv;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.Entities;
using Jellyfin.Model.IO;
using Jellyfin.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Providers.LiveTv
{
    public class LiveTvMetadataService : MetadataService<LiveTvChannel, ItemLookupInfo>
    {
        protected override void MergeData(MetadataResult<LiveTvChannel> source, MetadataResult<LiveTvChannel> target, MetadataFields[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);
        }

        public LiveTvMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, fileSystem, userDataManager, libraryManager)
        {
        }
    }
}
