using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Manager;
using System.Collections.Generic;
using System.Linq;
using CommonIO;

namespace MediaBrowser.Providers.Music
{
    class MusicVideoMetadataService : MetadataService<MusicVideo, MusicVideoInfo>
    {
        protected override void MergeData(MetadataResult<MusicVideo> source, MetadataResult<MusicVideo> target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (replaceData || string.IsNullOrEmpty(targetItem.Album))
            {
                targetItem.Album = sourceItem.Album;
            }

            if (replaceData || targetItem.Artists.Count == 0)
            {
                targetItem.Artists = sourceItem.Artists.ToList();
            }
        }

        public MusicVideoMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, fileSystem, userDataManager, libraryManager)
        {
        }
    }
}
