using System;
using System.Linq;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Music
{
    /// <summary>
    /// The audio metadata service.
    /// </summary>
    public class AudioMetadataService : MetadataService<Audio, SongInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioMetadataService"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/>.</param>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public AudioMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<AudioMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
        }

        private void SetProviderId(Audio sourceItem, Audio targetItem, bool replaceData, MetadataProvider provider)
        {
            var target = targetItem.GetProviderId(provider);
            if (replaceData || string.IsNullOrEmpty(target))
            {
                var source = sourceItem.GetProviderId(provider);
                if (!string.IsNullOrEmpty(source)
                    && (string.IsNullOrEmpty(target)
                        || !target.Equals(source, StringComparison.Ordinal)))
                {
                    targetItem.SetProviderId(provider, source);
                }
            }
        }

        /// <inheritdoc />
        protected override void MergeData(MetadataResult<Audio> source, MetadataResult<Audio> target, MetadataField[] lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            base.MergeData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (replaceData || targetItem.Artists.Count == 0)
            {
                targetItem.Artists = sourceItem.Artists;
            }
            else
            {
                targetItem.Artists = targetItem.Artists.Concat(sourceItem.Artists).Distinct().ToArray();
            }

            if (replaceData || string.IsNullOrEmpty(targetItem.Album))
            {
                targetItem.Album = sourceItem.Album;
            }

            SetProviderId(sourceItem, targetItem, replaceData, MetadataProvider.MusicBrainzAlbumArtist);
            SetProviderId(sourceItem, targetItem, replaceData, MetadataProvider.MusicBrainzAlbum);
            SetProviderId(sourceItem, targetItem, replaceData, MetadataProvider.MusicBrainzReleaseGroup);
        }
    }
}
