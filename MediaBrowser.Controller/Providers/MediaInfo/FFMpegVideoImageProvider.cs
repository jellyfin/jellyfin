using MediaBrowser.Common.IO;
using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers.MediaInfo
{
    /// <summary>
    /// Uses ffmpeg to create video images
    /// </summary>
    public class FfMpegVideoImageProvider : BaseFFMpegProvider<Video>
    {
        /// <summary>
        /// The _iso manager
        /// </summary>
        private readonly IIsoManager _isoManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="FfMpegVideoImageProvider" /> class.
        /// </summary>
        /// <param name="isoManager">The iso manager.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        public FfMpegVideoImageProvider(IIsoManager isoManager, ILogManager logManager, IServerConfigurationManager configurationManager, IMediaEncoder mediaEncoder)
            : base(logManager, configurationManager, mediaEncoder)
        {
            _isoManager = isoManager;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Last; }
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            if (item.LocationType != LocationType.FileSystem)
            {
                return false;
            }

            var video = item as Video;

            if (video != null)
            {
                if (video.VideoType == VideoType.Iso && _isoManager.CanMount(item.Path))
                {
                    return true;
                }

                // We can only extract images from folder rips if we know the largest stream path
                return video.VideoType == VideoType.VideoFile || video.VideoType == VideoType.BluRay || video.VideoType == VideoType.Dvd;
            }

            return false;
        }

        /// <summary>
        /// The true task result
        /// </summary>
        protected static readonly Task<bool> TrueTaskResult = Task.FromResult(true);

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            if (force || string.IsNullOrEmpty(item.PrimaryImagePath))
            {
                var video = (Video)item;

                // We can only extract images from videos if we know there's an embedded video stream
                if (video.MediaStreams != null && video.MediaStreams.Any(m => m.Type == MediaStreamType.Video))
                {
                    var filename = item.Id + "_" + item.DateModified.Ticks + "_primary";

                    var path = Kernel.Instance.FFMpegManager.VideoImageCache.GetResourcePath(filename, ".jpg");

                    if (!Kernel.Instance.FFMpegManager.VideoImageCache.ContainsFilePath(path))
                    {
                        return ExtractImage(video, path, cancellationToken);
                    }

                    // Image is already in the cache
                    item.PrimaryImagePath = path;
                }
            }

            SetLastRefreshed(item, DateTime.UtcNow);
            return TrueTaskResult;
        }

        /// <summary>
        /// Mounts the iso if needed.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>IsoMount.</returns>
        protected Task<IIsoMount> MountIsoIfNeeded(Video item, CancellationToken cancellationToken)
        {
            if (item.VideoType == VideoType.Iso)
            {
                return _isoManager.Mount(item.Path, cancellationToken);
            }

            return NullMountTaskResult;
        }

        /// <summary>
        /// Extracts the image.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="path">The path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        private async Task<bool> ExtractImage(Video video, string path, CancellationToken cancellationToken)
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

                await MediaEncoder.ExtractImage(inputPath, type, imageOffset, path, cancellationToken).ConfigureAwait(false);

                video.PrimaryImagePath = path;
                SetLastRefreshed(video, DateTime.UtcNow);
            }
            catch
            {
                SetLastRefreshed(video, DateTime.UtcNow, ProviderRefreshStatus.Failure);
            }
            finally
            {
                if (isoMount != null)
                {
                    isoMount.Dispose();
                }
            }

            return true;
        }
    }
}
