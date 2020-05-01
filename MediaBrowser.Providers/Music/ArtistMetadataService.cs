using System.Collections.Generic;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Music
{
    public class ArtistMetadataService : MetadataService<MusicArtist, ArtistInfo>
    {
        public ArtistMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<ArtistMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
        }

        /// <inheritdoc />
        protected override bool EnableUpdatingGenresFromChildren => true;

        /// <inheritdoc />
        protected override IList<BaseItem> GetChildrenForMetadataUpdates(MusicArtist item)
        {
            return item.IsAccessedByName
                ? item.GetTaggedItems(new InternalItemsQuery
                {
                    Recursive = true,
                    IsFolder = false
                })
                : item.GetRecursiveChildren(i => i is IHasArtist && !i.IsFolder);
        }

        /// <inheritdoc />
        protected override void MergeData(MetadataResult<MusicArtist> source, MetadataResult<MusicArtist> target, MetadataFields[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);
        }
    }
}
