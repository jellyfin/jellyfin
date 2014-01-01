using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.MediaInfo
{
    /// <summary>
    /// Uses ffmpeg to create video images
    /// </summary>
    public class AudioImageProvider : BaseMetadataProvider
    {
        /// <summary>
        /// The _locks
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// The _media encoder
        /// </summary>
        private readonly IMediaEncoder _mediaEncoder;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseMetadataProvider" /> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        public AudioImageProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IMediaEncoder mediaEncoder)
            : base(logManager, configurationManager)
        {
            _mediaEncoder = mediaEncoder;
        }

        /// <summary>
        /// Gets a value indicating whether [refresh on version change].
        /// </summary>
        /// <value><c>true</c> if [refresh on version change]; otherwise, <c>false</c>.</value>
        protected override bool RefreshOnVersionChange
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the provider version.
        /// </summary>
        /// <value>The provider version.</value>
        protected override string ProviderVersion
        {
            get
            {
                return "1";
            }
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item.LocationType == LocationType.FileSystem && item is Audio;
        }

        /// <summary>
        /// Override this to return the date that should be compared to the last refresh date
        /// to determine if this provider should be re-fetched.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>DateTime.</returns>
        protected override DateTime CompareDate(BaseItem item)
        {
            return item.DateModified;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Last; }
        }

        public override ItemUpdateType ItemUpdateType
        {
            get
            {
                return ItemUpdateType.ImageUpdate;
            }
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            item.ValidateImages();

            var audio = (Audio)item;

            if (string.IsNullOrEmpty(audio.PrimaryImagePath) && audio.HasEmbeddedImage)
            {
                try
                {
                    await CreateImagesForSong(audio, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error extracting image for {0}", ex, item.Name);
                }
            }

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return true;
        }

        /// <summary>
        /// Creates the images for song.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.InvalidOperationException">Can't extract an image unless the audio file has an embedded image.</exception>
        private async Task CreateImagesForSong(Audio item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var path = GetAudioImagePath(item);

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

                        Directory.CreateDirectory(parentPath);

                        await _mediaEncoder.ExtractImage(new[] { item.Path }, InputType.AudioFile, null, null, path, cancellationToken).ConfigureAwait(false);
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
        }

        /// <summary>
        /// Gets the audio image path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        private string GetAudioImagePath(Audio item)
        {
            var album = item.Parent as MusicAlbum;

            var filename = item.Album ?? string.Empty;
            filename += item.Artists.FirstOrDefault() ?? string.Empty;
            filename += album == null ? item.Id.ToString("N") + "_primary" + item.DateModified.Ticks : album.Id.ToString("N") + album.DateModified.Ticks + "_primary";

            filename = filename.GetMD5() + ".jpg";

            var prefix = filename.Substring(0, 1);

            return Path.Combine(AudioImagesPath, prefix, filename);
        }

        /// <summary>
        /// Gets the audio images data path.
        /// </summary>
        /// <value>The audio images data path.</value>
        public string AudioImagesPath
        {
            get
            {
                return Path.Combine(ConfigurationManager.ApplicationPaths.DataPath, "extracted-audio-images");
            }
        }

        /// <summary>
        /// Gets the lock.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>SemaphoreSlim.</returns>
        private SemaphoreSlim GetLock(string filename)
        {
            return _locks.GetOrAdd(filename, key => new SemaphoreSlim(1, 1));
        }
    }
}
