#pragma warning disable CS1591

using System.Collections.Generic;
using System.Linq;
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
        protected override IReadOnlyList<BaseItem> GetChildrenForMetadataUpdates(Playlist item)
            => item.GetLinkedChildren();

        /// <inheritdoc />
        protected override void MergeData(MetadataResult<Playlist> source, MetadataResult<Playlist> target, MetadataField[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            base.MergeData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (mergeMetadataSettings)
            {
                targetItem.PlaylistMediaType = sourceItem.PlaylistMediaType;

                if (replaceData || targetItem.LinkedChildren.Length == 0)
                {
                    targetItem.LinkedChildren = sourceItem.LinkedChildren;
                }
                else
                {
                    targetItem.LinkedChildren = sourceItem.LinkedChildren.Concat(targetItem.LinkedChildren).Distinct().ToArray();
                }

                if (replaceData || targetItem.Shares.Count == 0)
                {
                    targetItem.Shares = sourceItem.Shares;
                }
                else
                {
                    targetItem.Shares = sourceItem.Shares.Concat(targetItem.Shares).DistinctBy(s => s.UserId).ToArray();
                }
            }
        }
    }
}
