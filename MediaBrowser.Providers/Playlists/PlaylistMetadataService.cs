using System.Collections.Generic;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Playlists
{
    public class PlaylistMetadataService : MetadataService<Playlist, ItemLookupInfo>
    {
        public PlaylistMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<PlaylistMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
        }

        /// <inheritdoc />
        protected override bool EnableUpdatingGenresFromChildren => true;

        /// <inheritdoc />
        protected override bool EnableUpdatingOfficialRatingFromChildren => true;

        /// <inheritdoc />
        protected override bool EnableUpdatingStudiosFromChildren => true;

        /// <inheritdoc />
        protected override IList<BaseItem> GetChildrenForMetadataUpdates(Playlist item)
            => item.GetLinkedChildren();

        /// <inheritdoc />
        protected override void MergeData(MetadataResult<Playlist> source, MetadataResult<Playlist> target, MetadataFields[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (mergeMetadataSettings)
            {
                targetItem.PlaylistMediaType = sourceItem.PlaylistMediaType;
                targetItem.LinkedChildren = sourceItem.LinkedChildren;
                targetItem.Shares = sourceItem.Shares;
            }
        }
    }
}
