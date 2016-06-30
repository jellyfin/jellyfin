using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Manager;
using System.Collections.Generic;
using CommonIO;

namespace MediaBrowser.Providers.Games
{
    public class GameMetadataService : MetadataService<Game, GameInfo>
    {
        protected override void MergeData(MetadataResult<Game> source, MetadataResult<Game> target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (replaceData || string.IsNullOrEmpty(targetItem.GameSystem))
            {
                targetItem.GameSystem = sourceItem.GameSystem;
            }

            if (replaceData || !targetItem.PlayersSupported.HasValue)
            {
                targetItem.PlayersSupported = sourceItem.PlayersSupported;
            }
        }

        public GameMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, fileSystem, userDataManager, libraryManager)
        {
        }
    }
}
