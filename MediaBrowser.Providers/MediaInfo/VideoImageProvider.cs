using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.MediaInfo
{
    public class VideoImageProvider : IDynamicImageProvider, IHasChangeMonitor
    {
        private readonly IIsoManager _isoManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IServerConfigurationManager _config;

        public VideoImageProvider(IIsoManager isoManager, IMediaEncoder mediaEncoder, IServerConfigurationManager config)
        {
            _isoManager = isoManager;
            _mediaEncoder = mediaEncoder;
            _config = config;
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

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType> { ImageType.Primary };
        }

        public Task<DynamicImageResponse> GetImage(IHasImages item, ImageType type, CancellationToken cancellationToken)
        {
            var video = (Video)item;

            // No support for this
            if (video.VideoType == VideoType.HdDvd)
            {
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }

            // Can't extract from iso's if we weren't unable to determine iso type
            if (video.VideoType == VideoType.Iso && !video.IsoType.HasValue)
            {
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }

            // Can't extract if we didn't find a video stream in the file
            if (!video.DefaultVideoStreamIndex.HasValue)
            {
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }

            return GetVideoImage(video, cancellationToken);
        }

        public async Task<DynamicImageResponse> GetVideoImage(Video item, CancellationToken cancellationToken)
        {
            var isoMount = await MountIsoIfNeeded(item, cancellationToken).ConfigureAwait(false);

            try
            {
                // If we know the duration, grab it from 10% into the video. Otherwise just 10 seconds in.
                // Always use 10 seconds for dvd because our duration could be out of whack
                var imageOffset = item.VideoType != VideoType.Dvd && item.RunTimeTicks.HasValue &&
                                  item.RunTimeTicks.Value > 0
                                      ? TimeSpan.FromTicks(Convert.ToInt64(item.RunTimeTicks.Value * .1))
                                      : TimeSpan.FromSeconds(10);

                InputType type;

                var inputPath = MediaEncoderHelpers.GetInputArgument(item.Path, item.LocationType == LocationType.Remote, item.VideoType, item.IsoType, isoMount, item.PlayableStreamFileNames, out type);

                var stream = await _mediaEncoder.ExtractImage(inputPath, type, false, item.Video3DFormat, imageOffset, cancellationToken).ConfigureAwait(false);

                return new DynamicImageResponse
                {
                    Format = ImageFormat.Jpg,
                    HasImage = true,
                    Stream = stream
                };
            }
            finally
            {
                if (isoMount != null)
                {
                    isoMount.Dispose();
                }
            }
        }

        public string Name
        {
            get { return "Embedded Image"; }
        }

        public bool Supports(IHasImages item)
        {
            if (!_config.Configuration.EnableVideoImageExtraction)
            {
                return false;
            }

            return item.LocationType == LocationType.FileSystem && item is Video;
        }

        public bool HasChanged(IHasMetadata item, DateTime date)
        {
            return item.DateModified > date;
        }
    }
}
