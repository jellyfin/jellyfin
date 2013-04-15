using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.ScheduledTasks
{
    /// <summary>
    /// Class ChapterImagesTask
    /// </summary>
    class ChapterImagesTask : IScheduledTask
    {
        /// <summary>
        /// The _kernel
        /// </summary>
        private readonly Kernel _kernel;
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChapterImagesTask" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="libraryManager">The library manager.</param>
        public ChapterImagesTask(Kernel kernel, ILogger logger, ILibraryManager libraryManager)
        {
            _kernel = kernel;
            _logger = logger;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Creates the triggers that define when the task will run
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[]
                {
                    new DailyTrigger { TimeOfDay = TimeSpan.FromHours(4) }
                };
        }

        /// <summary>
        /// Returns the task to be executed
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var videos = _libraryManager.RootFolder.RecursiveChildren
                .OfType<Video>()
                .Where(v => v.Chapters != null && v.Chapters.Count != 0)
                .ToList();

            var numComplete = 0;

            foreach (var video in videos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await _kernel.FFMpegManager.PopulateChapterImages(video, cancellationToken, true, true);

                numComplete++;
                double percent = numComplete;
                percent /= videos.Count;

                progress.Report(100 * percent);
            }
        }

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Chapter image extraction"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return "Creates thumbnails for videos that have chapters."; }
        }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>The category.</value>
        public string Category
        {
            get
            {
                return "Library";
            }
        }
    }
}
