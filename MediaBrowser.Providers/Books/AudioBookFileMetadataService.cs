using System;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.AudioBooks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Books
{
    /// <summary>
    /// Class to register providers for individual AudioBook files.
    /// </summary>
    public class AudioBookFileMetadataService : MetadataService<AudioBookFile, AudioBookFileInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioBookFileMetadataService"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/>.</param>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public AudioBookFileMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<AudioBookFileMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
        }

        private void SetProviderId(AudioBookFile sourceItem, AudioBookFile targetItem, bool replaceData, MetadataProvider provider)
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
        protected override void MergeData(MetadataResult<AudioBookFile> source, MetadataResult<AudioBookFile> target, MetadataField[] lockedFields, bool replaceData, bool mergeMetadataSettings)
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

            // TODO: Create and register provider specific to book information and AudioBook information
            // https://openlibrary.org/developers/api
            // https://openlibrary.org/search.json?title=war&author=sebastian+junger
        }
    }
}
