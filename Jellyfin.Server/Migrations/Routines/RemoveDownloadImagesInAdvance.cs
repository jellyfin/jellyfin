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
        private readonly IVirtualFolderManager _virtualFolderManager;

        public RemoveDownloadImagesInAdvance(
            ILogger<RemoveDownloadImagesInAdvance> logger,
            ILibraryManager libraryManager,
            IVirtualFolderManager virtualFolderManager)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _virtualFolderManager = virtualFolderManager;
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
            _logger.LogInformation("Removing 'RemoveDownloadImagesInAdvance' settings in all the libraries");
            foreach (var virtualFolder in _virtualFolderManager.GetVirtualFolders())
            {
                // Some virtual folders don't have a proper item id.
                if (!Guid.TryParse(virtualFolder.ItemId, out var folderId))
                {
                    continue;
                }

                var libraryOptions = virtualFolder.LibraryOptions;
                var collectionFolder = (CollectionFolder)_libraryManager.GetItemById(folderId);
                // The property no longer exists in LibraryOptions, so we just re-save the options to get old data removed.
                collectionFolder.UpdateLibraryOptions(libraryOptions);
                _logger.LogInformation("Removed from '{VirtualFolder}'", virtualFolder.Name);
            }
        }
    }
}
