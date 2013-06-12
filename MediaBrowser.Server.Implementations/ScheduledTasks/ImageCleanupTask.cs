using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.ScheduledTasks
{
    /// <summary>
    /// Class ImageCleanupTask
    /// </summary>
    public class ImageCleanupTask : IScheduledTask
    {
        /// <summary>
        /// The _kernel
        /// </summary>
        private readonly Kernel _kernel;
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IServerApplicationPaths _appPaths;
        private readonly IItemRepository _itemRepo;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageCleanupTask" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="appPaths">The app paths.</param>
        public ImageCleanupTask(Kernel kernel, ILogger logger, ILibraryManager libraryManager, IServerApplicationPaths appPaths, IItemRepository itemRepo)
        {
            _kernel = kernel;
            _logger = logger;
            _libraryManager = libraryManager;
            _appPaths = appPaths;
            _itemRepo = itemRepo;
        }

        /// <summary>
        /// Creates the triggers that define when the task will run
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[]
                {
                    new DailyTrigger { TimeOfDay = TimeSpan.FromHours(2) }
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
            var items = _libraryManager.RootFolder.RecursiveChildren.ToList();

            foreach (var video in items.OfType<Video>().Where(v => v.Chapters != null))
            {
                await _kernel.FFMpegManager.PopulateChapterImages(video, cancellationToken, false, true).ConfigureAwait(false);
            }

            // First gather all image files
            var files = GetFiles(_kernel.FFMpegManager.AudioImagesDataPath)
                .Concat(GetFiles(_kernel.FFMpegManager.VideoImagesDataPath))
                .Concat(GetFiles(_appPaths.DownloadedImagesDataPath))
                .ToList();

            // Now gather all items
            items.Add(_libraryManager.RootFolder);

            // Determine all possible image paths
            var pathsInUse = items.SelectMany(GetPathsInUse)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToDictionary(p => p, StringComparer.OrdinalIgnoreCase);

            var numComplete = 0;

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!pathsInUse.ContainsKey(file))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        File.Delete(file);
                    }
                    catch (IOException ex)
                    {
                        _logger.ErrorException("Error deleting {0}", ex, file);
                    }
                }

                // Update progress
                numComplete++;
                double percent = numComplete;
                percent /= files.Count;

                progress.Report(100 * percent);
            }
        }

        /// <summary>
        /// Gets the paths in use.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        private IEnumerable<string> GetPathsInUse(BaseItem item)
        {
            IEnumerable<string> images = item.Images.Values.ToList();

            images = images.Concat(item.BackdropImagePaths);

            images = images.Concat(item.ScreenshotImagePaths);

            var localTrailers = _itemRepo.GetItems(item.LocalTrailerIds).ToList();
            images = localTrailers.Aggregate(images, (current, subItem) => current.Concat(GetPathsInUse(subItem)));

            var themeSongs = _itemRepo.GetItems(item.ThemeSongIds).ToList();
            images = themeSongs.Aggregate(images, (current, subItem) => current.Concat(GetPathsInUse(subItem)));

            var themeVideos = _itemRepo.GetItems(item.ThemeVideoIds).ToList();
            images = themeVideos.Aggregate(images, (current, subItem) => current.Concat(GetPathsInUse(subItem)));

            var video = item as Video;

            if (video != null && video.Chapters != null)
            {
                images = images.Concat(video.Chapters.Where(i => !string.IsNullOrEmpty(i.ImagePath)).Select(i => i.ImagePath));
            }

            var movie = item as Movie;

            if (movie != null)
            {
                var specialFeatures = _itemRepo.GetItems(movie.SpecialFeatureIds).ToList();
                images = specialFeatures.Aggregate(images, (current, subItem) => current.Concat(GetPathsInUse(subItem)));
            }

            return images;
        }

        /// <summary>
        /// Gets the files.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        private IEnumerable<string> GetFiles(string path)
        {
            try
            {
                return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    .Where(i =>
                    {
                        var ext = Path.GetExtension(i);

                        return !string.IsNullOrEmpty(ext) && BaseItem.SupportedImageExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
                    });
            }
            catch (DirectoryNotFoundException)
            {
                return new string[] { };
            }
        }

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Images cleanup"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return "Deletes downloaded and extracted images that are no longer being used."; }
        }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>The category.</value>
        public string Category
        {
            get
            {
                return "Maintenance";
            }
        }
    }
}
