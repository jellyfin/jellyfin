using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using MediaBrowser.Providers.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Providers.Music
{
    public class ArtistMetadataService : MetadataService<MusicArtist, ArtistInfo>
    {
        protected override IList<BaseItem> GetChildrenForMetadataUpdates(MusicArtist item)
        {
            return item.IsAccessedByName ?
                item.GetTaggedItems(new Controller.Entities.InternalItemsQuery
                {
                    Recursive = true,
                    IsFolder = false
                }) :
                item.GetRecursiveChildren(i => i is IHasArtist && !i.IsFolder);
        }

        protected override bool EnableUpdatingGenresFromChildren
        {
            get
            {
                return true;
            }
        }

        protected override void MergeData(MetadataResult<MusicArtist> source, MetadataResult<MusicArtist> target, MetadataFields[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);
        }

        public ArtistMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, fileSystem, userDataManager, libraryManager)
        {
        }
    }
}
