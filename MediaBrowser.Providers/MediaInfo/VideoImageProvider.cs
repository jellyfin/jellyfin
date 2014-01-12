using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.MediaInfo
{
    class VideoImageProvider : BaseMetadataProvider
    {
        /// <summary>
        /// The _locks
        /// </summary>
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// The _media encoder
        /// </summary>
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IIsoManager _isoManager;

        public VideoImageProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IMediaEncoder mediaEncoder, IIsoManager isoManager)
            : base(logManager, configurationManager)
        {
            _mediaEncoder = mediaEncoder;
            _isoManager = isoManager;
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
            return item.LocationType == LocationType.FileSystem && item is Video;
        }

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            var video = (Video)item;

            if (!QualifiesForExtraction(video))
            {
                return false;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        /// <summary>
        /// Qualifieses for extraction.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private bool QualifiesForExtraction(Video item)
        {
            if (!ConfigurationManager.Configuration.EnableVideoImageExtraction)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(item.PrimaryImagePath))
            {
                return false;
            }

            // No support for this
            if (item.VideoType == VideoType.HdDvd)
            {
                return false;
            }

            // Can't extract from iso's if we weren't unable to determine iso type
            if (item.VideoType == VideoType.Iso && !item.IsoType.HasValue)
            {
                return false;
            }

            // Can't extract if we didn't find a video stream in the file
            if (!item.DefaultVideoStreamIndex.HasValue)
            {
                return false;
            }

            return true;
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

            var video = (Video)item;

            // Double check this here in case force was used
            if (QualifiesForExtraction(video))
            {
                try
                {
                    await ExtractImage(video, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Swallow this so that we don't keep on trying over and over again

                    Logger.ErrorException("Error extracting image for {0}", ex, item.Name);
                }
            }

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return true;
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

            var path = GetVideoImagePath(item);

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

                var inputPath = MediaEncoderHelpers.GetInputArgument(video.Path, video.LocationType == LocationType.Remote, video.VideoType, video.IsoType, isoMount, video.PlayableStreamFileNames, out type);

                await _mediaEncoder.ExtractImage(inputPath, type, false, video.Video3DFormat, imageOffset, path, cancellationToken).ConfigureAwait(false);

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
        /// Gets the lock.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>SemaphoreSlim.</returns>
        private SemaphoreSlim GetLock(string filename)
        {
            return _locks.GetOrAdd(filename, key => new SemaphoreSlim(1, 1));
        }

        /// <summary>
        /// Gets the video images data path.
        /// </summary>
        /// <value>The video images data path.</value>
        public string VideoImagesPath
        {
            get
            {
                return Path.Combine(ConfigurationManager.ApplicationPaths.DataPath, "extracted-video-images");
            }
        }

        /// <summary>
        /// Gets the audio image path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        private string GetVideoImagePath(Video item)
        {
            var filename = item.Path + "_" + item.DateModified.Ticks + "_primary";

            filename = filename.GetMD5() + ".jpg";

            var prefix = filename.Substring(0, 1);

            return Path.Combine(VideoImagesPath, prefix, filename);
        }
    }
}
