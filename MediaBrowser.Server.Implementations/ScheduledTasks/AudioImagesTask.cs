using MediaBrowser.Common.IO;
using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;
using MoreLinq;

namespace MediaBrowser.Server.Implementations.ScheduledTasks
{
    /// <summary>
    /// Class AudioImagesTask
    /// </summary>
    public class AudioImagesTask : IScheduledTask
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

        private readonly ILogger _logger;


        /// <summary>
        /// The _locks
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        private readonly List<Audio> _newlyAddedItems = new List<Audio>();

        private const int NewItemDelay = 300000;

        /// <summary>
        /// The current new item timer
        /// </summary>
        /// <value>The new item timer.</value>
        private Timer NewItemTimer { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioImagesTask" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        public AudioImagesTask(ILibraryManager libraryManager, IMediaEncoder mediaEncoder, ILogManager logManager)
        {
            _libraryManager = libraryManager;
            _mediaEncoder = mediaEncoder;
            _logger = logManager.GetLogger(GetType().Name);

            ImageCache = new FileSystemRepository(Kernel.Instance.FFMpegManager.AudioImagesDataPath);

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
            var audio = e.Item as Audio;

            if (audio != null)
            {
                lock (_newlyAddedItems)
                {
                    _newlyAddedItems.Add(audio);

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

        /// <summary>
        /// News the item timer callback.
        /// </summary>
        /// <param name="state">The state.</param>
        private async void NewItemTimerCallback(object state)
        {
            List<Audio> newSongs;

            // Lock the list and release all resources
            lock (_newlyAddedItems)
            {
                newSongs = _newlyAddedItems.DistinctBy(i => i.Id).ToList();
                _newlyAddedItems.Clear();

                NewItemTimer.Dispose();
                NewItemTimer = null;
            }

            foreach (var item in newSongs
                .Where(i => i.LocationType == LocationType.FileSystem && string.IsNullOrEmpty(i.PrimaryImagePath) && i.MediaStreams.Any(m => m.Type == MediaStreamType.Video))
                .Take(20))
            {
                try
                {
                    await CreateImagesForSong(item, CancellationToken.None).ConfigureAwait(false);
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
            get { return "Audio image extraction"; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get { return "Extracts images from audio files that do not have external images."; }
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
            var items = _libraryManager.RootFolder.RecursiveChildren
                .OfType<Audio>()
                .Where(i => i.LocationType == LocationType.FileSystem && string.IsNullOrEmpty(i.PrimaryImagePath) && i.MediaStreams.Any(m => m.Type == MediaStreamType.Video))
                .ToList();

            progress.Report(0);

            var numComplete = 0;

            foreach (var item in items)
            {
                try
                {
                    await CreateImagesForSong(item, cancellationToken).ConfigureAwait(false);
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
        /// Creates the images for song.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task CreateImagesForSong(Audio item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (item.MediaStreams.All(i => i.Type != MediaStreamType.Video))
            {
                throw new InvalidOperationException("Can't extract an image unless the audio file has an embedded image.");
            }

            var album = item.Parent as MusicAlbum;

            var filename = item.Album ?? string.Empty;

            filename += album == null ? item.Id.ToString() + item.DateModified.Ticks : album.Id.ToString() + album.DateModified.Ticks;

            var path = ImageCache.GetResourcePath(filename + "_primary", ".jpg");

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
                        await _mediaEncoder.ExtractImage(new[] {item.Path}, InputType.AudioFile, null, path, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        semaphore.Release();
                    }

                    // Image is already in the cache
                    item.PrimaryImagePath = path;

                    await _libraryManager.UpdateItem(item, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    semaphore.Release();
                }
            }
        }

        /// <summary>
        /// Gets the default triggers.
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[]
                {
                    new DailyTrigger { TimeOfDay = TimeSpan.FromHours(1) }
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
