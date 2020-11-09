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
            var virtual_folders = _libraryManager.GetVirtualFolders(false);
            _logger.LogInformation("Removing 'RemoveDownloadImagesInAdvance' settings in all the libraries");
            foreach (var virtual_folder in virtual_folders)
            {
                var library_options = virtual_folder.LibraryOptions;
                var collectionFolder = (CollectionFolder)_libraryManager.GetItemById(virtual_folder.ItemId);
                // The property no longer exists in LibraryOptions, so we just re-save the options to get old data removed.
                collectionFolder.UpdateLibraryOptions(library_options);
                _logger.LogInformation("Removed from '{VirtualFolder}'", virtual_folder.Name);
            }
        }
    }
}
