using System.Collections.Generic;
using Jellyfin.Controller.Configuration;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Entities.Audio;
using Jellyfin.Controller.Library;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.Entities;
using Jellyfin.Model.IO;
using Jellyfin.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Providers.Music
{
    public class ArtistMetadataService : MetadataService<MusicArtist, ArtistInfo>
    {
        protected override IList<BaseItem> GetChildrenForMetadataUpdates(MusicArtist item)
        {
            return item.IsAccessedByName ?
                item.GetTaggedItems(new InternalItemsQuery
                {
                    Recursive = true,
                    IsFolder = false
                }) :
                item.GetRecursiveChildren(i => i is IHasArtist && !i.IsFolder);
        }

        protected override bool EnableUpdatingGenresFromChildren => true;

        protected override void MergeData(MetadataResult<MusicArtist> source, MetadataResult<MusicArtist> target, MetadataFields[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);
        }

        public ArtistMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, fileSystem, userDataManager, libraryManager)
        {
        }
    }
}
