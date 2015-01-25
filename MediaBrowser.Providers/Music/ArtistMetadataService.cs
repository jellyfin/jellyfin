using MediaBrowser.Common.IO;
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

namespace MediaBrowser.Providers.Music
{
    public class ArtistMetadataService : MetadataService<MusicArtist, ArtistInfo>
    {
        private readonly ILibraryManager _libraryManager;

        public ArtistMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IProviderRepository providerRepo, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, providerRepo, fileSystem, userDataManager)
        {
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Merges the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <param name="lockedFields">The locked fields.</param>
        /// <param name="replaceData">if set to <c>true</c> [replace data].</param>
        /// <param name="mergeMetadataSettings">if set to <c>true</c> [merge metadata settings].</param>
        protected override void MergeData(MusicArtist source, MusicArtist target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);
        }

        protected override async Task<ItemUpdateType> BeforeSave(MusicArtist item, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = await base.BeforeSave(item, isFullRefresh, currentUpdateType).ConfigureAwait(false);

            if (isFullRefresh || currentUpdateType > ItemUpdateType.None)
            {
                if (!item.IsLocked)
                {
                    var taggedItems = item.IsAccessedByName ?
                        _libraryManager.RootFolder.GetRecursiveChildren(i => !i.IsFolder && item.ItemFilter(i)).ToList() :
                        item.GetRecursiveChildren(i => i is IHasArtist && !i.IsFolder).ToList();

                    if (!item.LockedFields.Contains(MetadataFields.Genres))
                    {
                        var currentList = item.Genres.ToList();

                        item.Genres = taggedItems.SelectMany(i => i.Genres)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
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
    }
}
