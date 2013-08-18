using MediaBrowser.Common.IO;
using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MoreLinq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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

        private readonly IItemRepository _itemRepo;
        
        private readonly ILogger _logger;
        
        /// <summary>
        /// The _locks
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        private readonly List<BaseItem> _newlyAddedItems = new List<BaseItem>();

        private const int NewItemDelay = 60000;

        /// <summary>
        /// The current new item timer
        /// </summary>
        /// <value>The new item timer.</value>
        private Timer NewItemTimer { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioImagesTask" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        /// <param name="isoManager">The iso manager.</param>
        public VideoImagesTask(ILibraryManager libraryManager, ILogManager logManager, IMediaEncoder mediaEncoder, IIsoManager isoManager, IItemRepository itemRepo)
        {
            _libraryManager = libraryManager;
            _mediaEncoder = mediaEncoder;
            _isoManager = isoManager;
            _itemRepo = itemRepo;
            _logger = logManager.GetLogger(GetType().Name);

            ImageCache = new FileSystemRepository(Kernel.Instance.FFMpegManager.VideoImagesDataPath);

            libraryManager.ItemAdded += libraryManager_ItemAdded;
            libraryManager.ItemUpdated += libraryManager_ItemAdded;
        }

        /// <summary>
        /// Handles the ItemAdded event of the libraryManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemChangeEventArgs"/> instance containing the event data.</param>
        void libraryManager_ItemAdded(object sender, ItemChangeEventArgs e)
        {
            lock (_newlyAddedItems)
            {
                _newlyAddedItems.Add(e.Item);

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

        /// <summary>
        /// News the item timer callback.
        /// </summary>
        /// <param name="state">The state.</param>
        private async void NewItemTimerCallback(object state)
        {
            List<BaseItem> newItems;

            // Lock the list and release all resources
            lock (_newlyAddedItems)
            {
                newItems = _newlyAddedItems.DistinctBy(i => i.Id).ToList();
                _newlyAddedItems.Clear();

                NewItemTimer.Dispose();
                NewItemTimer = null;
            }

            foreach (var item in GetItemsForExtraction(newItems.Take(5)))
            {
                try
                {
                    await ExtractImage(item, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error creating image for {0}", ex, item.Name);
                }
            }
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
            var items = GetItemsForExtraction(_libraryManager.RootFolder.RecursiveChildren).ToList();

            progress.Report(0);

            var numComplete = 0;

            foreach (var item in items)
            {
                try
                {
                    await ExtractImage(item, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Already logged at lower levels.
                    // Just don't let the task fail
                }

                numComplete++;
                double percent = numComplete;
                percent /= items.Count;

                progress.Report(100 * percent);
            }

            progress.Report(100);
        }

        /// <summary>
        /// Gets the items for extraction.
        /// </summary>
        /// <param name="sourceItems">The source items.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private IEnumerable<Video> GetItemsForExtraction(IEnumerable<BaseItem> sourceItems)
        {
            var allItems = sourceItems.ToList();

            var localTrailers = allItems.SelectMany(i => i.LocalTrailerIds.Select(_itemRepo.RetrieveItem)).Cast<Video>();

            var themeVideos = allItems.SelectMany(i => i.ThemeVideoIds.Select(_itemRepo.RetrieveItem)).Cast<Video>();

            var videos = allItems.OfType<Video>().ToList();

            var items = videos.ToList();

            items.AddRange(localTrailers);

            items.AddRange(themeVideos);

            items.AddRange(videos.SelectMany(i => i.AdditionalPartIds).Select(_itemRepo.RetrieveItem).Cast<Video>());
            items.AddRange(videos.OfType<Movie>().SelectMany(i => i.SpecialFeatureIds).Select(_itemRepo.RetrieveItem).Cast<Video>());

            return items.Where(i =>
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
            });
        }

        /// <summary>
        /// Extracts the image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task ExtractImage(Video item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var filename = item.Path + "_" + item.DateModified.Ticks + "_primary";

            var path = ImageCache.GetResourcePath(filename, ".jpg");

            if (!File.Exists(path))
            {
                var semaphore = GetLock(path);

                // Acquire a lock
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                // Check again
                if (!File.Exists(path))
                {
                    try
                    {
                        var parentPath = Path.GetDirectoryName(path);

                        if (!Directory.Exists(parentPath))
                        {
                            Directory.CreateDirectory(parentPath);
                        }

                        await ExtractImageInternal(item, path, cancellationToken).ConfigureAwait(false);
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

            // Image is already in the cache
            item.PrimaryImagePath = path;

            await _libraryManager.UpdateItem(item, ItemUpdateType.ImageUpdate, cancellationToken).ConfigureAwait(false);
        }
        
        /// <summary>
        /// Extracts the image.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="path">The path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task ExtractImageInternal(Video video, string path, CancellationToken cancellationToken)
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

                await _mediaEncoder.ExtractImage(inputPath, type, video.Video3DFormat, imageOffset, path, cancellationToken).ConfigureAwait(false);

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
