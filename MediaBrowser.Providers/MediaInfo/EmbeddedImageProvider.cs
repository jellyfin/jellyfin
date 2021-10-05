#nullable enable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
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
        private static readonly string[] _primaryImageFileNames =
        {
            "poster",
            "folder",
            "cover",
            "default"
        };

        private static readonly string[] _backdropImageFileNames =
        {
            "backdrop",
            "fanart",
            "background",
            "art"
        };

        private static readonly string[] _logoImageFileNames =
        {
            "logo",
        };

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
            if (item is Video)
            {
                if (item is Episode)
                {
                    return new List<ImageType>
                    {
                        ImageType.Primary,
                    };
                }

                return new List<ImageType>
                {
                    ImageType.Primary,
                    ImageType.Backdrop,
                    ImageType.Logo,
                };
            }

            return ImmutableList<ImageType>.Empty;
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

            return GetEmbeddedImage(video, type, cancellationToken);
        }

        private async Task<DynamicImageResponse> GetEmbeddedImage(Video item, ImageType type, CancellationToken cancellationToken)
        {
            MediaSourceInfo mediaSource = new MediaSourceInfo
            {
                VideoType = item.VideoType,
                IsoType = item.IsoType,
                Protocol = item.PathProtocol ?? MediaProtocol.File,
            };

            string[] imageFileNames;
            switch (type)
            {
                case ImageType.Backdrop:
                    imageFileNames = _backdropImageFileNames;
                    break;
                case ImageType.Logo:
                    imageFileNames = _logoImageFileNames;
                    break;
                case ImageType.Primary:
                default:
                    imageFileNames = _primaryImageFileNames;
                    break;
            }

            var imageStreams =
                item.GetMediaStreams()
                    .Where(i => i.Type == MediaStreamType.EmbeddedImage)
                    .ToList();

            if (!imageStreams.Any())
            {
                // Can't extract if we don't have any EmbeddedImage streams
                return new DynamicImageResponse { HasImage = false };
            }

            // Extract first stream containing an element of imageFileNames
            var imageStream = imageStreams
                .Where(stream => !string.IsNullOrEmpty(stream.Comment))
                .First(stream => imageFileNames.Any(name => stream.Comment.Contains(name, StringComparison.OrdinalIgnoreCase)));

            // Primary type only: default to first image if none found by label
            if (imageStream == null)
            {
                if (type == ImageType.Primary)
                {
                    imageStream = imageStreams[0];
                }
                else
                {
                    // No streams matched, abort
                    return new DynamicImageResponse { HasImage = false };
                }
            }

            string extractedImagePath = await _mediaEncoder.ExtractVideoImage(item.Path, item.Container, mediaSource, imageStream, imageStream.Index, cancellationToken).ConfigureAwait(false);

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
