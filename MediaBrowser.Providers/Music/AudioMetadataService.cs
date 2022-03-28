#pragma warning disable CS1591

using System;
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
    public class AudioMetadataService : MetadataService<Audio, SongInfo>
    {
        public AudioMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<AudioMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
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

            if (replaceData || string.IsNullOrEmpty(targetItem.Album))
            {
                targetItem.Album = sourceItem.Album;
            }

            var targetAlbumArtistId = targetItem.GetProviderId(MetadataProvider.MusicBrainzAlbumArtist);
            if (replaceData || string.IsNullOrEmpty(targetAlbumArtistId))
            {
                var sourceAlbumArtistId = sourceItem.GetProviderId(MetadataProvider.MusicBrainzAlbumArtist);

                if (!string.IsNullOrEmpty(sourceAlbumArtistId)
                    && (string.IsNullOrEmpty(targetAlbumArtistId)
                        || !targetAlbumArtistId.Equals(sourceAlbumArtistId, StringComparison.Ordinal)))
                {
                    targetItem.SetProviderId(MetadataProvider.MusicBrainzAlbumArtist, sourceAlbumArtistId);
                }
            }

            var targetAlbumId = targetItem.GetProviderId(MetadataProvider.MusicBrainzAlbum);
            if (replaceData || string.IsNullOrEmpty(targetAlbumId))
            {
                var sourceAlbumId = sourceItem.GetProviderId(MetadataProvider.MusicBrainzAlbum);

                if (!string.IsNullOrEmpty(sourceAlbumId)
                    && (string.IsNullOrEmpty(targetAlbumId)
                        || !targetAlbumId.Equals(sourceAlbumId, StringComparison.Ordinal)))
                {
                    targetItem.SetProviderId(MetadataProvider.MusicBrainzAlbum, sourceAlbumId);
                }
            }

            var targetReleaseGroupId = targetItem.GetProviderId(MetadataProvider.MusicBrainzReleaseGroup);
            if (replaceData || string.IsNullOrEmpty(targetReleaseGroupId))
            {
                var sourceReleaseGroupId = sourceItem.GetProviderId(MetadataProvider.MusicBrainzReleaseGroup);

                if (!string.IsNullOrEmpty(sourceReleaseGroupId)
                    && (string.IsNullOrEmpty(targetReleaseGroupId)
                        || !targetReleaseGroupId.Equals(sourceReleaseGroupId, StringComparison.Ordinal)))
                {
                    targetItem.SetProviderId(MetadataProvider.MusicBrainzReleaseGroup, sourceReleaseGroupId);
                }
            }
        }
    }
}
