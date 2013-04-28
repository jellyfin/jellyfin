using MediaBrowser.Common.IO;
using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers.MediaInfo;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.ScheduledTasks
{
    /// <summary>
    /// Class VideoImagesTask
    /// </summary>
    public class VideoImagesTask : IScheduledTask
    {
        /// <summary>
        /// Gets or sets the image cache.
        /// </summary>
        /// <value>The image cache.</value>
        public FileSystemRepository ImageCache { get; set; }

        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;
        /// <summary>
        /// The _media encoder
        /// </summary>
        private readonly IMediaEncoder _mediaEncoder;

        /// <summary>
        /// The _iso manager
        /// </summary>
        private readonly IIsoManager _isoManager;

        /// <summary>
        /// The _locks
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioImagesTask" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        /// <param name="isoManager">The iso manager.</param>
        public VideoImagesTask(ILibraryManager libraryManager, IMediaEncoder mediaEncoder, IIsoManager isoManager)
        {
            _libraryManager = libraryManager;
            _mediaEncoder = mediaEncoder;
            _isoManager = isoManager;

            ImageCache = new FileSystemRepository(Kernel.Instance.FFMpegManager.VideoImagesDataPath);
        }

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return "Video image extraction"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return "Extracts images from video files that do not have external images."; }
        }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>The category.</value>
        public string Category
        {
            get { return "Library"; }
        }

        /// <summary>
        /// Executes the task
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var allItems = _libraryManager.RootFolder.RecursiveChildren.ToList();

            var localTrailers = allItems.SelectMany(i => i.LocalTrailers);
            var videoBackdrops = allItems.SelectMany(i => i.VideoBackdrops);

            var videos = allItems.OfType<Video>().ToList();

            var items = videos;
            items.AddRange(localTrailers);
            items.AddRange(videoBackdrops);
            items.AddRange(videos.OfType<Movie>().SelectMany(i => i.SpecialFeatures).ToList());

            items = items.Where(i =>
            {
                if (!string.IsNullOrEmpty(i.PrimaryImagePath))
                {
                    return false;
                }

                if (i.LocationType != LocationType.FileSystem)
                {
                    return false;
                }

                if (i.VideoType == VideoType.HdDvd)
                {
                    return false;
                }

                if (i.VideoType == VideoType.Iso && !i.IsoType.HasValue)
                {
                    return false;
                }

                return i.MediaStreams != null && i.MediaStreams.Any(m => m.Type == MediaStreamType.Video);
            }).ToList();

            progress.Report(0);

            var numComplete = 0;

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var filename = item.Id + "_" + item.DateModified.Ticks + "_primary";

                var path = ImageCache.GetResourcePath(filename, ".jpg");

                var success = true;

                if (!ImageCache.ContainsFilePath(path))
                {
                    var semaphore = GetLock(path);

                    // Acquire a lock
                    await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                    // Check again
                    if (!ImageCache.ContainsFilePath(path))
                    {
                        try
                        {
                            await ExtractImage(item, path, cancellationToken).ConfigureAwait(false);
                        }
                        catch
                        {
                            success = false;
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }
                    else
                    {
                        semaphore.Release();
                    }
                }

                numComplete++;
                double percent = numComplete;
                percent /= items.Count;

                progress.Report(100 * percent);

                if (success)
                {
                    // Image is already in the cache
                    item.PrimaryImagePath = path;

                    await _libraryManager.SaveItem(item, cancellationToken).ConfigureAwait(false);
                }
            }

            progress.Report(100);
        }

        /// <summary>
        /// Extracts the image.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="path">The path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task ExtractImage(Video video, string path, CancellationToken cancellationToken)
        {
            var isoMount = await MountIsoIfNeeded(video, cancellationToken).ConfigureAwait(false);

            try
            {
                // If we know the duration, grab it from 10% into the video. Otherwise just 10 seconds in.
                // Always use 10 seconds for dvd because our duration could be out of whack
                var imageOffset = video.VideoType != VideoType.Dvd && video.RunTimeTicks.HasValue &&
                                  video.RunTimeTicks.Value > 0
                                      ? TimeSpan.FromTicks(Convert.ToInt64(video.RunTimeTicks.Value * .1))
                                      : TimeSpan.FromSeconds(10);

                InputType type;

                var inputPath = MediaEncoderHelpers.GetInputArgument(video, isoMount, out type);

                await _mediaEncoder.ExtractImage(inputPath, type, imageOffset, path, cancellationToken).ConfigureAwait(false);

                video.PrimaryImagePath = path;
            }
            finally
            {
                if (isoMount != null)
                {
                    isoMount.Dispose();
                }
            }
        }

        /// <summary>
        /// The null mount task result
        /// </summary>
        protected readonly Task<IIsoMount> NullMountTaskResult = Task.FromResult<IIsoMount>(null);

        /// <summary>
        /// Mounts the iso if needed.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IIsoMount}.</returns>
        protected Task<IIsoMount> MountIsoIfNeeded(Video item, CancellationToken cancellationToken)
        {
            if (item.VideoType == VideoType.Iso)
            {
                return _isoManager.Mount(item.Path, cancellationToken);
            }

            return NullMountTaskResult;
        }

        /// <summary>
        /// Gets the default triggers.
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
        /// Gets the lock.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.Object.</returns>
        private SemaphoreSlim GetLock(string filename)
        {
            return _locks.GetOrAdd(filename, key => new SemaphoreSlim(1, 1));
        }
    }
}
