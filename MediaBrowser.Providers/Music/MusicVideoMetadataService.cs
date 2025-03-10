#pragma warning disable CS1591

using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Music
{
    public class MusicVideoMetadataService : MetadataService<MusicVideo, MusicVideoInfo>
    {
        public MusicVideoMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<MusicVideoMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
        }

        /// <inheritdoc />
        protected override void MergeData(
            MetadataResult<MusicVideo> source,
            MetadataResult<MusicVideo> target,
            IReadOnlyList<MetadataField> lockedFields,
            bool replaceData,
            bool mergeMetadataSettings)
        {
            base.MergeData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (replaceData || string.IsNullOrEmpty(targetItem.Album))
            {
                targetItem.Album = sourceItem.Album;
            }

            if (replaceData || targetItem.Artists.Count == 0)
            {
                targetItem.Artists = sourceItem.Artists;
            }
            else
            {
                targetItem.Artists = targetItem.Artists.Concat(sourceItem.Artists).Distinct().ToArray();
            }
        }
    }
}
