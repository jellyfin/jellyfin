using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageCleanupTask" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="appPaths">The app paths.</param>
        public ImageCleanupTask(Kernel kernel, ILogger logger, ILibraryManager libraryManager, IServerApplicationPaths appPaths)
        {
            _kernel = kernel;
            _logger = logger;
            _libraryManager = libraryManager;
            _appPaths = appPaths;
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
            await EnsureChapterImages(cancellationToken).ConfigureAwait(false);

            // First gather all image files
            var files = GetFiles(_kernel.FFMpegManager.AudioImagesDataPath)
                .Concat(GetFiles(_kernel.FFMpegManager.VideoImagesDataPath))
                .Concat(GetFiles(_appPaths.DownloadedImagesDataPath))
                .ToList();

            // Now gather all items
            var items = _libraryManager.RootFolder.RecursiveChildren.ToList();
            items.Add(_libraryManager.RootFolder);

            // Determine all possible image paths
            var pathsInUse = items.SelectMany(GetPathsInUse)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToDictionary(p => p, StringComparer.OrdinalIgnoreCase);

            var numComplete = 0;

            var tasks = files.Select(file => Task.Run(() =>
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
                lock (progress)
                {
                    numComplete++;
                    double percent = numComplete;
                    percent /= files.Count;

                    progress.Report(100 * percent);
                }
            }));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Ensures the chapter images.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private Task EnsureChapterImages(CancellationToken cancellationToken)
        {
            var videos = _libraryManager.RootFolder.RecursiveChildren.OfType<Video>().Where(v => v.Chapters != null).ToList();

            var tasks = videos.Select(v => Task.Run(async () =>
            {
                await _kernel.FFMpegManager.PopulateChapterImages(v, cancellationToken, false, true);
            }));

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Gets the paths in use.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        private IEnumerable<string> GetPathsInUse(BaseItem item)
        {
            IEnumerable<string> images = new List<string>();

            if (item.Images != null)
            {
                images = images.Concat(item.Images.Values);
            }

            if (item.BackdropImagePaths != null)
            {
                images = images.Concat(item.BackdropImagePaths);
            }

            if (item.ScreenshotImagePaths != null)
            {
                images = images.Concat(item.ScreenshotImagePaths);
            }

            var video = item as Video;

            if (video != null && video.Chapters != null)
            {
                images = images.Concat(video.Chapters.Where(i => !string.IsNullOrEmpty(i.ImagePath)).Select(i => i.ImagePath));
            }

            if (item.LocalTrailers != null)
            {
                foreach (var subItem in item.LocalTrailers)
                {
                    images = images.Concat(GetPathsInUse(subItem));
                }
            }

            var movie = item as Movie;

            if (movie != null && movie.SpecialFeatures != null)
            {
                foreach (var subItem in movie.SpecialFeatures)
                {
                    images = images.Concat(GetPathsInUse(subItem));
                }
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
            return Directory.EnumerateFiles(path, "*.jpg", SearchOption.AllDirectories).Concat(Directory.EnumerateFiles(path, "*.png", SearchOption.AllDirectories));
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
