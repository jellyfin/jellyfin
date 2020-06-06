using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Globalization;

namespace Emby.Server.Implementations.ScheduledTasks
{
    /// <summary>
    /// Class ChapterImagesTask.
    /// </summary>
    public class ChapterImagesTask : IScheduledTask
    {
        /// <summary>
        /// The _logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The _library manager.
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        private readonly IItemRepository _itemRepo;

        private readonly IApplicationPaths _appPaths;

        private readonly IEncodingManager _encodingManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILocalizationManager _localization;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChapterImagesTask" /> class.
        /// </summary>
        public ChapterImagesTask(
            ILoggerFactory loggerFactory,
            ILibraryManager libraryManager,
            IItemRepository itemRepo,
            IApplicationPaths appPaths,
            IEncodingManager encodingManager,
            IFileSystem fileSystem,
            ILocalizationManager localization)
        {
            _logger = loggerFactory.CreateLogger(GetType().Name);
            _libraryManager = libraryManager;
            _itemRepo = itemRepo;
            _appPaths = appPaths;
            _encodingManager = encodingManager;
            _fileSystem = fileSystem;
            _localization = localization;
        }

        /// <summary>
        /// Creates the triggers that define when the task will run.
        /// </summary>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfo.TriggerDaily,
                    TimeOfDayTicks = TimeSpan.FromHours(2).Ticks,
                    MaxRuntimeTicks = TimeSpan.FromHours(4).Ticks
                }
            };
        }

        /// <summary>
        /// Returns the task to be executed.
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
                {
                    EnableImages = false
                },
                SourceTypes = new SourceType[] { SourceType.Library },
                HasChapterImages = false,
                IsVirtualItem = false
            })
                .OfType<Video>()
                .ToList();

            var numComplete = 0;

            var failHistoryPath = Path.Combine(_appPaths.CachePath, "chapter-failures.txt");

            List<string> previouslyFailedImages;

            if (File.Exists(failHistoryPath))
            {
                try
                {
                    previouslyFailedImages = File.ReadAllText(failHistoryPath)
                        .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                }
                catch (IOException)
                {
                    previouslyFailedImages = new List<string>();
                }
            }
            else
            {
                previouslyFailedImages = new List<string>();
            }

            var directoryService = new DirectoryService(_fileSystem);

            foreach (var video in videos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var key = video.Path + video.DateModified.Ticks;

                var extract = !previouslyFailedImages.Contains(key, StringComparer.OrdinalIgnoreCase);

                try
                {
                    var chapters = _itemRepo.GetChapters(video);

                    var success = await _encodingManager.RefreshChapterImages(video, directoryService, chapters, extract, true, cancellationToken).ConfigureAwait(false);

                    if (!success)
                    {
                        previouslyFailedImages.Add(key);

                        var parentPath = Path.GetDirectoryName(failHistoryPath);

                        Directory.CreateDirectory(parentPath);

                        string text = string.Join("|", previouslyFailedImages);
                        File.WriteAllText(failHistoryPath, text);
                    }

                    numComplete++;
                    double percent = numComplete;
                    percent /= videos.Count;

                    progress.Report(100 * percent);
                }
                catch (ObjectDisposedException)
                {
                    //TODO Investigate and properly fix.
                    break;
                }
            }
        }

        /// <inheritdoc />
        public string Name => _localization.GetLocalizedString("TaskRefreshChapterImages");

        /// <inheritdoc />
        public string Description => _localization.GetLocalizedString("TaskRefreshChapterImagesDescription");

        /// <inheritdoc />
        public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

        /// <inheritdoc />
        public string Key => "RefreshChapterImages";

        /// <inheritdoc />
        public bool IsHidden => false;

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public bool IsLogged => true;
    }
}
