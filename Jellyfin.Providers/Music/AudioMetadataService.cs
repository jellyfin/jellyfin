using Jellyfin.Controller.Configuration;
using Jellyfin.Controller.Entities.Audio;
using Jellyfin.Controller.Library;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.Entities;
using Jellyfin.Model.IO;
using Jellyfin.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Providers.Music
{
    public class AudioMetadataService : MetadataService<Audio, SongInfo>
    {
        protected override void MergeData(MetadataResult<Audio> source, MetadataResult<Audio> target, MetadataFields[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (replaceData || targetItem.Artists.Length == 0)
            {
                targetItem.Artists = sourceItem.Artists;
            }

            if (replaceData || string.IsNullOrEmpty(targetItem.Album))
            {
                targetItem.Album = sourceItem.Album;
            }
        }

        public AudioMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, fileSystem, userDataManager, libraryManager)
        {
        }
    }
}
