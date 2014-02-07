using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Manager;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.LiveTv
{
    public class ChannelMetadataService : MetadataService<LiveTvChannel, ItemLookupInfo>
    {
        private readonly ILibraryManager _libraryManager;

        public ChannelMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IProviderRepository providerRepo, IFileSystem fileSystem, ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, providerRepo, fileSystem)
        {
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Merges the specified source.
        /// </summary>
        protected override void MergeData(LiveTvChannel source, LiveTvChannel target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);
        }

        protected override Task SaveItem(LiveTvChannel item, ItemUpdateType reason, CancellationToken cancellationToken)
        {
            return _libraryManager.UpdateItem(item, reason, cancellationToken);
        }
    }
}
