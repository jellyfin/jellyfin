using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Removes the old 'RemoveDownloadImagesInAdvance' from library options.
    /// </summary>
    internal class RemoveDownloadImagesInAdvance : IMigrationRoutine
    {
        private readonly ILogger<RemoveDownloadImagesInAdvance> _logger;
        private readonly ILibraryManager _libraryManager;

        public RemoveDownloadImagesInAdvance(ILogger<RemoveDownloadImagesInAdvance> logger, ILibraryManager libraryManager)
        {
            _logger = logger;
            _libraryManager = libraryManager;
        }

        /// <inheritdoc/>
        public Guid Id => Guid.Parse("{A81F75E0-8F43-416F-A5E8-516CCAB4D8CC}");

        /// <inheritdoc/>
        public string Name => "RemoveDownloadImagesInAdvance";

        /// <inheritdoc/>
        public bool PerformOnNewInstall => false;

        /// <inheritdoc/>
        public void Perform()
        {
            var virtualFolders = _libraryManager.GetVirtualFolders(false);
            _logger.LogInformation("Removing 'RemoveDownloadImagesInAdvance' settings in all the libraries");
            foreach (var virtualFolder in virtualFolders)
            {
                // Some virtual folders don't have a proper item id.
                if (!Guid.TryParse(virtualFolder.ItemId, out var folderId))
                {
                    continue;
                }

                var libraryOptions = virtualFolder.LibraryOptions;
                var collectionFolder = _libraryManager.GetItemById<CollectionFolder>(folderId) ?? throw new InvalidOperationException("Failed to find CollectionFolder");
                // The property no longer exists in LibraryOptions, so we just re-save the options to get old data removed.
                collectionFolder.UpdateLibraryOptions(libraryOptions);
                _logger.LogInformation("Removed from '{VirtualFolder}'", virtualFolder.Name);
            }
        }
    }
}
