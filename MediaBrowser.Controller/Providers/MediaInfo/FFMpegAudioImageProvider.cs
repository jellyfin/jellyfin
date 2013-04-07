using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers.MediaInfo
{
    /// <summary>
    /// Uses ffmpeg to create video images
    /// </summary>
    public class FFMpegAudioImageProvider : BaseFFMpegProvider<Audio>
    {
        public FFMpegAudioImageProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IMediaEncoder mediaEncoder)
            : base(logManager, configurationManager, mediaEncoder)
        {
        }

        /// <summary>
        /// The true task result
        /// </summary>
        protected static readonly Task<bool> TrueTaskResult = Task.FromResult(true);

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Last; }
        }

        /// <summary>
        /// The _locks
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// Gets the lock.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.Object.</returns>
        private SemaphoreSlim GetLock(string filename)
        {
            return _locks.GetOrAdd(filename, key => new SemaphoreSlim(1, 1));
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            var success = ProviderRefreshStatus.Success;

            if (force || string.IsNullOrEmpty(item.PrimaryImagePath))
            {
                var album = item.ResolveArgs.Parent as MusicAlbum;

                if (album != null)
                {
                    // First try to use the parent's image
                    item.PrimaryImagePath = item.ResolveArgs.Parent.PrimaryImagePath;
                }

                // If it's still empty see if there's an embedded image
                if (force || string.IsNullOrEmpty(item.PrimaryImagePath))
                {
                    var audio = (Audio)item;

                    if (audio.MediaStreams != null && audio.MediaStreams.Any(s => s.Type == MediaStreamType.Video))
                    {
                        var filename = album != null && string.IsNullOrEmpty(audio.Album + album.DateModified.Ticks) ? (audio.Id.ToString() + audio.DateModified.Ticks) : audio.Album;

                        var path = Kernel.Instance.FFMpegManager.AudioImageCache.GetResourcePath(filename + "_primary", ".jpg");

                        if (!Kernel.Instance.FFMpegManager.AudioImageCache.ContainsFilePath(path))
                        {
                            var semaphore = GetLock(path);

                            // Acquire a lock
                            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                            // Check again
                            if (!Kernel.Instance.FFMpegManager.AudioImageCache.ContainsFilePath(path))
                            {
                                try
                                {
                                    await MediaEncoder.ExtractImage(new[] { audio.Path }, InputType.AudioFile, null, path, cancellationToken).ConfigureAwait(false);
                                }
                                catch
                                {
                                    success = ProviderRefreshStatus.Failure;
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

                        if (success == ProviderRefreshStatus.Success)
                        {
                            // Image is already in the cache
                            audio.PrimaryImagePath = path;
                        }
                    }
                }
            }

            SetLastRefreshed(item, DateTime.UtcNow, success);
            return true;
        }
    }
}
