#pragma warning disable CA1826 // CA1826 Do not use Enumerable methods on Indexable collections.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.MediaInfo
{
    /// <summary>
    /// Uses <see cref="IMediaEncoder"/> to create still images from the main video.
    /// </summary>
    public class VideoImageProvider : IDynamicImageProvider, IHasOrder
    {
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly ILogger<VideoImageProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoImageProvider"/> class.
        /// </summary>
        /// <param name="mediaSourceManager">The media source manager for fetching item streams.</param>
        /// <param name="mediaEncoder">The media encoder for capturing images.</param>
        /// <param name="logger">The logger.</param>
        public VideoImageProvider(IMediaSourceManager mediaSourceManager, IMediaEncoder mediaEncoder, ILogger<VideoImageProvider> logger)
        {
            _mediaSourceManager = mediaSourceManager;
            _mediaEncoder = mediaEncoder;
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => "Screen Grabber";

        /// <inheritdoc />
        // Make sure this comes after internet image providers
        public int Order => 100;

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new[] { ImageType.Primary };
        }

        /// <inheritdoc />
        public Task<DynamicImageResponse> GetImage(BaseItem item, ImageType type, CancellationToken cancellationToken)
        {
            var video = (Video)item;

            // No support for these
            if (video.IsPlaceHolder || video.VideoType == VideoType.Dvd)
            {
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }

            // Can't extract if we didn't find a video stream in the file
            if (!video.DefaultVideoStreamIndex.HasValue)
            {
                _logger.LogInformation("Skipping image extraction due to missing DefaultVideoStreamIndex for {Path}.", video.Path ?? string.Empty);
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }

            return GetVideoImage(video, cancellationToken);
        }

        private async Task<DynamicImageResponse> GetVideoImage(Video item, CancellationToken cancellationToken)
        {
            MediaSourceInfo mediaSource = new MediaSourceInfo
            {
                VideoType = item.VideoType,
                IsoType = item.IsoType,
                Protocol = item.PathProtocol ?? MediaProtocol.File,
            };

            // If we know the duration, grab it from 10% into the video. Otherwise just 10 seconds in.
            // Always use 10 seconds for dvd because our duration could be out of whack
            var imageOffset = item.VideoType != VideoType.Dvd && item.RunTimeTicks > 0
                                  ? TimeSpan.FromTicks(item.RunTimeTicks.Value / 10)
                                  : TimeSpan.FromSeconds(10);

            var query = new MediaStreamQuery { ItemId = item.Id, Index = item.DefaultVideoStreamIndex };
            var videoStream = _mediaSourceManager.GetMediaStreams(query).FirstOrDefault();
            if (videoStream is null)
            {
                query.Type = MediaStreamType.Video;
                query.Index = null;
                videoStream = _mediaSourceManager.GetMediaStreams(query).FirstOrDefault();
            }

            if (videoStream is null)
            {
                _logger.LogInformation("Skipping image extraction: no video stream found for {Path}.", item.Path ?? string.Empty);
                return new DynamicImageResponse { HasImage = false };
            }

            string extractedImagePath = await _mediaEncoder.ExtractVideoImage(item.Path, item.Container, mediaSource, videoStream, item.Video3DFormat, imageOffset, cancellationToken).ConfigureAwait(false);

            return new DynamicImageResponse
            {
                Format = ImageFormat.Jpg,
                HasImage = true,
                Path = extractedImagePath,
                Protocol = MediaProtocol.File
            };
        }

        /// <inheritdoc />
        public bool Supports(BaseItem item)
        {
            if (item.IsShortcut)
            {
                return false;
            }

            if (!item.IsFileProtocol)
            {
                return false;
            }

            return item is Video video && !video.IsPlaceHolder && video.IsCompleteMedia;
        }
    }
}
