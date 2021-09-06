#nullable enable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.MediaInfo
{
    public class VideoImageProvider : IDynamicImageProvider, IHasOrder
    {
        private readonly IMediaEncoder _mediaEncoder;
        private readonly ILogger<VideoImageProvider> _logger;

        public VideoImageProvider(IMediaEncoder mediaEncoder, ILogger<VideoImageProvider> logger)
        {
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

            var mediaStreams =
                item.GetMediaStreams();

            var imageStreams =
                mediaStreams
                    .Where(i => i.Type == MediaStreamType.EmbeddedImage)
                    .ToList();

            string extractedImagePath;

            if (imageStreams.Count == 0)
            {
                // If we know the duration, grab it from 10% into the video. Otherwise just 10 seconds in.
                // Always use 10 seconds for dvd because our duration could be out of whack
                var imageOffset = item.VideoType != VideoType.Dvd && item.RunTimeTicks.HasValue &&
                                  item.RunTimeTicks.Value > 0
                                      ? TimeSpan.FromTicks(item.RunTimeTicks.Value / 10)
                                      : TimeSpan.FromSeconds(10);

                var videoStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Video);
                extractedImagePath = await _mediaEncoder.ExtractVideoImage(item.Path, item.Container, mediaSource, videoStream, item.Video3DFormat, imageOffset, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var imageStream = imageStreams.Find(i => (i.Comment ?? string.Empty).Contains("front", StringComparison.OrdinalIgnoreCase))
                    ?? imageStreams.Find(i => (i.Comment ?? string.Empty).Contains("cover", StringComparison.OrdinalIgnoreCase))
                    ?? imageStreams[0];

                extractedImagePath = await _mediaEncoder.ExtractVideoImage(item.Path, item.Container, mediaSource, imageStream, imageStream.Index, cancellationToken).ConfigureAwait(false);
            }

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
