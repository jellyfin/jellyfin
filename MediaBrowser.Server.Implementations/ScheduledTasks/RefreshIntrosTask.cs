using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Server.Implementations.ScheduledTasks
{
    /// <summary>
    /// Class RefreshIntrosTask
    /// </summary>
    public class RefreshIntrosTask : ILibraryPostScanTask
    {
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshIntrosTask" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        public RefreshIntrosTask(ILibraryManager libraryManager, ILogger logger, IFileSystem fileSystem)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Runs the specified progress.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var files = _libraryManager.GetAllIntroFiles().ToList();

            var numComplete = 0;

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await RefreshIntro(file, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error refreshing intro {0}", ex, file);
                }

                numComplete++;
                double percent = numComplete;
                percent /= files.Count;
                progress.Report(percent * 100);
            }
        }

        /// <summary>
        /// Refreshes the intro.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task RefreshIntro(string path, CancellationToken cancellationToken)
        {
            var item = _libraryManager.ResolvePath(_fileSystem.GetFileSystemInfo(path));

            if (item == null)
            {
                _logger.Error("Intro resolver returned null for {0}", path);
                return;
            }

            var dbItem = _libraryManager.GetItemById(item.Id);

            if (dbItem != null)
            {
                item = dbItem;
            }

            // Force the save if it's a new item
            await item.RefreshMetadata(cancellationToken).ConfigureAwait(false);
        }
    }
}
