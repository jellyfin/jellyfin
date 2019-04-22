using Jellyfin.Controller.Configuration;
using Jellyfin.Controller.Entities.Audio;
using Jellyfin.Controller.Library;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.Entities;
using Jellyfin.Model.IO;
using Jellyfin.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Providers.MusicGenres
{
    public class MusicGenreMetadataService : MetadataService<MusicGenre, ItemLookupInfo>
    {
        protected override void MergeData(MetadataResult<MusicGenre> source, MetadataResult<MusicGenre> target, MetadataFields[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);
        }

        public MusicGenreMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, fileSystem, userDataManager, libraryManager)
        {
        }
    }
}
