using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Migrations.Routines
{
    /// <summary>
    /// Removes the old 'RemoveDownloadImagesInAdvance' from library options.
    /// </summary>
    public partial class RemoveDownloadImagesInAdvance : IPostStartupMigrationRoutine
    {
        private readonly ILogger<RemoveDownloadImagesInAdvance> _logger;
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveDownloadImagesInAdvance"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="libraryManager">The library manager.</param>
        public RemoveDownloadImagesInAdvance(ILogger<RemoveDownloadImagesInAdvance> logger, ILibraryManager libraryManager)
        {
            _logger = logger;
            _libraryManager = libraryManager;
        }

        /// <inheritdoc/>
        public Guid Id => Guid.Parse("{A81F75E0-8F43-416F-A5E8-516CCAB4D8CC}");

        /// <inheritdoc/>
        public string Name => "RemoveDownloadImagesInAdvance";

        /// <inheritdoc />
        public string Timestamp => "20231125220011";

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
                var collectionFolder = (CollectionFolder)_libraryManager.GetItemById(folderId);
                // The property no longer exists in LibraryOptions, so we just re-save the options to get old data removed.
                collectionFolder.UpdateLibraryOptions(libraryOptions);
                _logger.LogInformation("Removed from '{VirtualFolder}'", virtualFolder.Name);
            }
        }
    }
}
