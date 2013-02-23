using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers.MediaInfo
{
    /// <summary>
    /// Uses ffmpeg to create video images
    /// </summary>
    [Export(typeof(BaseMetadataProvider))]
    public class FFMpegVideoImageProvider : BaseFFMpegImageProvider<Video>
    {
        /// <summary>
        /// The _iso manager
        /// </summary>
        private readonly IIsoManager _isoManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="FFMpegVideoImageProvider" /> class.
        /// </summary>
        /// <param name="isoManager">The iso manager.</param>
        [ImportingConstructor]
        public FFMpegVideoImageProvider([Import("isoManager")] IIsoManager isoManager)
        {
            _isoManager = isoManager;
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
                if (video.VideoType == VideoType.Iso && video.IsoType.HasValue && _isoManager.CanMount(item.Path))
                {
                    return true;
                }

                // We can only extract images from folder rips if we know the largest stream path
                return video.VideoType == VideoType.VideoFile || video.VideoType == VideoType.BluRay || video.VideoType == VideoType.Dvd;
            }

            return false;
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        protected override Task<bool> FetchAsyncInternal(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(item.PrimaryImagePath))
            {
                var video = (Video)item;

                var filename = item.Id + "_" + item.DateModified.Ticks + "_primary";

                var path = Kernel.Instance.FFMpegManager.VideoImageCache.GetResourcePath(filename, ".jpg");

                if (!Kernel.Instance.FFMpegManager.VideoImageCache.ContainsFilePath(path))
                {
                    return ExtractImage(video, path, cancellationToken);
                }

                // Image is already in the cache
                item.PrimaryImagePath = path;
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
                var imageOffset = video.VideoType != VideoType.Dvd && video.RunTimeTicks.HasValue && video.RunTimeTicks.Value > 0
                                           ? TimeSpan.FromTicks(Convert.ToInt64(video.RunTimeTicks.Value * .1))
                                           : TimeSpan.FromSeconds(10);

                var inputPath = isoMount == null ?
                    Kernel.Instance.FFMpegManager.GetInputArgument(video) :
                    Kernel.Instance.FFMpegManager.GetInputArgument(video, isoMount);

                var success = await Kernel.Instance.FFMpegManager.ExtractImage(inputPath, imageOffset, path, cancellationToken).ConfigureAwait(false);

                if (success)
                {
                    video.PrimaryImagePath = path;
                    SetLastRefreshed(video, DateTime.UtcNow);
                }
                else
                {
                    SetLastRefreshed(video, DateTime.UtcNow, ProviderRefreshStatus.Failure);
                }
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
