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

        /// <summary>
        /// The _locks
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioImagesTask" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        public AudioImagesTask(ILibraryManager libraryManager, IMediaEncoder mediaEncoder)
        {
            _libraryManager = libraryManager;
            _mediaEncoder = mediaEncoder;

            ImageCache = new FileSystemRepository(Kernel.Instance.FFMpegManager.AudioImagesDataPath);
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
                .Where(i => i.LocationType == LocationType.FileSystem && string.IsNullOrEmpty(i.PrimaryImagePath) && i.MediaStreams != null && i.MediaStreams.Any(m => m.Type == MediaStreamType.Video))
                .ToList();

            progress.Report(0);

            var numComplete = 0;

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var album = item.Parent as MusicAlbum;

                var filename = item.Album ?? string.Empty;

                filename += album == null ? item.Id.ToString() + item.DateModified.Ticks : album.Id.ToString() + album.DateModified.Ticks;

                var path = ImageCache.GetResourcePath(filename + "_primary", ".jpg");

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
                            await _mediaEncoder.ExtractImage(new[] { item.Path }, InputType.AudioFile, null, path, cancellationToken).ConfigureAwait(false);
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

                    await _libraryManager.UpdateItem(item, cancellationToken).ConfigureAwait(false);
                }
            }

            progress.Report(100);
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
