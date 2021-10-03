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
    /// <summary>
    /// Uses ffmpeg to extract embedded images.
    /// </summary>
    public class EmbeddedImageProvider : IDynamicImageProvider, IHasOrder
    {
        private readonly IMediaEncoder _mediaEncoder;
        private readonly ILogger<EmbeddedImageProvider> _logger;

        public EmbeddedImageProvider(IMediaEncoder mediaEncoder, ILogger<EmbeddedImageProvider> logger)
        {
            _mediaEncoder = mediaEncoder;
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => "Embedded Image Extractor";

        /// <inheritdoc />
        // Default to after internet image providers but before Screen Grabber
        public int Order => 99;

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

            // Can't extract if we didn't find any video streams in the file
            if (!video.DefaultVideoStreamIndex.HasValue)
            {
                _logger.LogInformation("Skipping image extraction due to missing DefaultVideoStreamIndex for {Path}.", video.Path ?? string.Empty);
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }

            return GetEmbeddedImage(video, cancellationToken);
        }

        private async Task<DynamicImageResponse> GetEmbeddedImage(Video item, CancellationToken cancellationToken)
        {
            MediaSourceInfo mediaSource = new MediaSourceInfo
            {
                VideoType = item.VideoType,
                IsoType = item.IsoType,
                Protocol = item.PathProtocol ?? MediaProtocol.File,
            };

            var imageStreams =
                item.GetMediaStreams()
                    .Where(i => i.Type == MediaStreamType.EmbeddedImage)
                    .ToList();

            string extractedImagePath;

            if (imageStreams.Count == 0)
            {
                // Can't extract if we don't have any EmbeddedImage streams
                return new DynamicImageResponse { HasImage = false };
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
