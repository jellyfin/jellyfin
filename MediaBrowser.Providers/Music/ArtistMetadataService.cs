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
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    public class ArtistMetadataService : MetadataService<MusicArtist, ItemId>
    {
        private readonly ILibraryManager _libraryManager;

        public ArtistMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IProviderRepository providerRepo, IFileSystem fileSystem, ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, providerRepo, fileSystem)
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

        protected override Task SaveItem(MusicArtist item, ItemUpdateType reason, CancellationToken cancellationToken)
        {
            return _libraryManager.UpdateItem(item, reason, cancellationToken);
        }

        protected override ItemUpdateType AfterMetadataRefresh(MusicArtist item)
        {
            var updateType = base.AfterMetadataRefresh(item);

            if (!item.IsAccessedByName && !item.LockedFields.Contains(MetadataFields.Genres))
            {
                var songs = item.RecursiveChildren.OfType<Audio>().ToList();

                var currentList = item.Genres.ToList();

                item.Genres = songs.SelectMany(i => i.Genres)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (currentList.Count != item.Genres.Count || !currentList.OrderBy(i => i).SequenceEqual(item.Genres.OrderBy(i => i), StringComparer.OrdinalIgnoreCase))
                {
                    updateType = updateType | ItemUpdateType.MetadataDownload;
                }
            } 
            
            return updateType;
        }
    }
}
