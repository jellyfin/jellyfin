using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.IO;
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
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        private readonly List<Video> _newlyAddedItems = new List<Video>();

        private const int NewItemDelay = 30000;

        /// <summary>
        /// The current new item timer
        /// </summary>
        /// <value>The new item timer.</value>
        private Timer NewItemTimer { get; set; }

        private readonly IItemRepository _itemRepo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChapterImagesTask" /> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="itemRepo">The item repo.</param>
        public ChapterImagesTask(ILogManager logManager, ILibraryManager libraryManager, IItemRepository itemRepo)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _libraryManager = libraryManager;
            _itemRepo = itemRepo;

            libraryManager.ItemAdded += libraryManager_ItemAdded;
            libraryManager.ItemUpdated += libraryManager_ItemAdded;
        }

        void libraryManager_ItemAdded(object sender, ItemChangeEventArgs e)
        {
            var video = e.Item as Video;

            if (video != null)
            {
                lock (_newlyAddedItems)
                {
                    _newlyAddedItems.Add(video);

                    if (NewItemTimer == null)
                    {
                        NewItemTimer = new Timer(NewItemTimerCallback, null, NewItemDelay, Timeout.Infinite);
                    }
                    else
                    {
                        NewItemTimer.Change(NewItemDelay, Timeout.Infinite);
                    }
                }
            }
        }

        private async void NewItemTimerCallback(object state)
        {
            List<Video> newItems;

            // Lock the list and release all resources
            lock (_newlyAddedItems)
            {
                newItems = _newlyAddedItems.DistinctBy(i => i.Id).ToList();
                _newlyAddedItems.Clear();

                NewItemTimer.Dispose();
                NewItemTimer = null;
            }

            // Limit to video files to reduce changes of ffmpeg crash dialog
            foreach (var item in newItems
                .Where(i => i.LocationType == LocationType.FileSystem && i.VideoType == VideoType.VideoFile && string.IsNullOrEmpty(i.PrimaryImagePath) && i.DefaultVideoStreamIndex.HasValue)
                .Take(2))
            {
                try
                {
                    var chapters = _itemRepo.GetChapters(item.Id).ToList();

                    await FFMpegManager.Instance.PopulateChapterImages(item, chapters, true, true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error creating image for {0}", ex, item.Name);
                }
            }
        }

        /// <summary>
        /// Creates the triggers that define when the task will run
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            // IMPORTANT: Make sure to update the dashboard "wizardsettings" page if this default ever changes
            
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
                .ToList();

            var numComplete = 0;

            var failHistoryPath = Path.Combine(FFMpegManager.Instance.ChapterImagesPath, "failures.txt");

            List<string> previouslyFailedImages;

            try
            {
                previouslyFailedImages = File.ReadAllText(failHistoryPath)
                    .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            }
            catch (FileNotFoundException)
            {
                previouslyFailedImages = new List<string>();
            }
            catch (DirectoryNotFoundException)
            {
                previouslyFailedImages = new List<string>();
            }

            foreach (var video in videos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var key = video.Path + video.DateModified.Ticks;

                var extract = !previouslyFailedImages.Contains(key, StringComparer.OrdinalIgnoreCase);

                var chapters = _itemRepo.GetChapters(video.Id).ToList();

                var success = await FFMpegManager.Instance.PopulateChapterImages(video, chapters, extract, true, cancellationToken);

                if (!success)
                {
                    previouslyFailedImages.Add(key);

                    var parentPath = Path.GetDirectoryName(failHistoryPath);

                    Directory.CreateDirectory(parentPath);

                    File.WriteAllText(failHistoryPath, string.Join("|", previouslyFailedImages.ToArray()));
                }

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
            get
            {
                // IMPORTANT: Make sure to update the dashboard "wizardsettings" page if this name ever changes
                return "Chapter image extraction";
            }
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
