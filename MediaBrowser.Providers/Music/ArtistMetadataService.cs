using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Providers.Music
{
    public class ArtistMetadataService : MetadataService<MusicArtist, ArtistInfo>
    {
        protected override async Task<ItemUpdateType> BeforeSave(MusicArtist item, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = await base.BeforeSave(item, isFullRefresh, currentUpdateType).ConfigureAwait(false);

            if (isFullRefresh || currentUpdateType > ItemUpdateType.None)
            {
                if (!item.IsLocked)
                {
                    var taggedItems = item.IsAccessedByName ?
                        item.GetTaggedItems(new Controller.Entities.InternalItemsQuery()
                        {
                            Recursive = true,
                            IsFolder = false
                        }) :
                        item.GetRecursiveChildren(i => i is IHasArtist && !i.IsFolder).ToList();

                    if (!item.LockedFields.Contains(MetadataFields.Genres))
                    {
                        var currentList = item.Genres.ToList();

                        item.Genres = taggedItems.SelectMany(i => i.Genres)
                            .DistinctNames()
                            .ToList();

                        if (currentList.Count != item.Genres.Count || !currentList.OrderBy(i => i).SequenceEqual(item.Genres.OrderBy(i => i), StringComparer.OrdinalIgnoreCase))
                        {
                            updateType = updateType | ItemUpdateType.MetadataEdit;
                        }
                    }
                }
            }
            
            return updateType;
        }

        protected override void MergeData(MetadataResult<MusicArtist> source, MetadataResult<MusicArtist> target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);
        }

        public ArtistMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, fileSystem, userDataManager, libraryManager)
        {
        }
    }
}
