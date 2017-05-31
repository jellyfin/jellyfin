using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;

namespace Emby.Server.Implementations.ScheduledTasks
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

        private readonly IItemRepository _itemRepo;

        private readonly IApplicationPaths _appPaths;

        private readonly IEncodingManager _encodingManager;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChapterImagesTask" /> class.
        /// </summary>
        public ChapterImagesTask(ILogManager logManager, ILibraryManager libraryManager, IItemRepository itemRepo, IApplicationPaths appPaths, IEncodingManager encodingManager, IFileSystem fileSystem)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _libraryManager = libraryManager;
            _itemRepo = itemRepo;
            _appPaths = appPaths;
            _encodingManager = encodingManager;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Creates the triggers that define when the task will run
        /// </summary>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[] { 
            
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = TimeSpan.FromHours(2).Ticks,
                    MaxRuntimeMs = Convert.ToInt32(TimeSpan.FromHours(4).TotalMilliseconds)
                }
            };
        }

        public string Key
        {
            get { return "RefreshChapterImages"; }
        }

        /// <summary>
        /// Returns the task to be executed
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var videos = _libraryManager.GetItemList(new InternalItemsQuery
            {
                MediaTypes = new[] { MediaType.Video },
                IsFolder = false,
                Recursive = true,
                DtoOptions = new DtoOptions(false)

            })
                .OfType<Video>()
                .ToList();

            var numComplete = 0;

            var failHistoryPath = Path.Combine(_appPaths.CachePath, "chapter-failures.txt");

            List<string> previouslyFailedImages;

            try
            {
                previouslyFailedImages = _fileSystem.ReadAllText(failHistoryPath)
                    .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
            }
            catch (FileNotFoundException)
            {
                previouslyFailedImages = new List<string>();
            }
            catch (IOException)
            {
                previouslyFailedImages = new List<string>();
            }

            foreach (var video in videos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var key = video.Path + video.DateModified.Ticks;

                var extract = !previouslyFailedImages.Contains(key, StringComparer.OrdinalIgnoreCase);

                try
                {
                    var chapters = _itemRepo.GetChapters(video.Id).ToList();

                    var success = await _encodingManager.RefreshChapterImages(new ChapterImageRefreshOptions
                    {
                        SaveChapters = true,
                        ExtractImages = extract,
                        Video = video,
                        Chapters = chapters

                    }, CancellationToken.None);

                    if (!success)
                    {
                        previouslyFailedImages.Add(key);

                        var parentPath = _fileSystem.GetDirectoryName(failHistoryPath);

                        _fileSystem.CreateDirectory(parentPath);

                        _fileSystem.WriteAllText(failHistoryPath, string.Join("|", previouslyFailedImages.ToArray()));
                    }

                    numComplete++;
                    double percent = numComplete;
                    percent /= videos.Count;

                    progress.Report(100 * percent);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
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
