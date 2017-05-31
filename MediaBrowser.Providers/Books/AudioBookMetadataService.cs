using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Manager;
using System.Collections.Generic;
using System.Linq;

using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Providers.Books
{
    public class AudioBookMetadataService : MetadataService<AudioBook, SongInfo>
    {
        protected override void MergeData(MetadataResult<AudioBook> source, MetadataResult<AudioBook> target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (replaceData || targetItem.Artists.Count == 0)
            {
                targetItem.Artists = sourceItem.Artists.ToList();
            }

            if (replaceData || string.IsNullOrEmpty(targetItem.Album))
            {
                targetItem.Album = sourceItem.Album;
            }
        }

        public AudioBookMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, fileSystem, userDataManager, libraryManager)
        {
        }
    }
}
