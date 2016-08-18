using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Persistence;

namespace MediaBrowser.Server.Implementations.Library.Validators
{
    /// <summary>
    /// Class MusicGenresPostScanTask
    /// </summary>
    public class StudiosPostScanTask : ILibraryPostScanTask
    {
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        private readonly ILogger _logger;
        private readonly IItemRepository _itemRepo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArtistsPostScanTask" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        public StudiosPostScanTask(ILibraryManager libraryManager, ILogger logger, IItemRepository itemRepo)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _itemRepo = itemRepo;
        }

        /// <summary>
        /// Runs the specified progress.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return new StudiosValidator(_libraryManager, _logger, _itemRepo).Run(progress, cancellationToken);
        }
    }
}
