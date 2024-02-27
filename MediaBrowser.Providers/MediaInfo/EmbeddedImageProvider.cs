#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.MediaInfo
{
    /// <summary>
    /// Uses <see cref="IMediaEncoder"/> to extract embedded images.
    /// </summary>
    public class EmbeddedImageProvider : IDynamicImageProvider, IHasOrder
    {
        private static readonly string[] _primaryImageFileNames =
        {
            "poster",
            "folder",
            "cover",
            "default",
            "movie",
            "show"
        };

        private static readonly string[] _backdropImageFileNames =
        {
            "backdrop",
            "background",
            "art"
        };

        private static readonly string[] _logoImageFileNames =
        {
            "logo",
        };

        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly ILogger<EmbeddedImageProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddedImageProvider"/> class.
        /// </summary>
        /// <param name="mediaSourceManager">The media source manager for fetching item streams and attachments.</param>
        /// <param name="mediaEncoder">The media encoder for extracting attached/embedded images.</param>
        /// <param name="logger">The logger.</param>
        public EmbeddedImageProvider(IMediaSourceManager mediaSourceManager, IMediaEncoder mediaEncoder, ILogger<EmbeddedImageProvider> logger)
        {
            _mediaSourceManager = mediaSourceManager;
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
                    return new[]
                    {
                        ImageType.Primary,
                    };
                }

                return new[]
                {
                    ImageType.Primary,
                    ImageType.Backdrop,
                    ImageType.Logo,
                };
            }

            return Array.Empty<ImageType>();
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

            string[] imageFileNames = type switch
            {
                ImageType.Primary => _primaryImageFileNames,
                ImageType.Backdrop => _backdropImageFileNames,
                ImageType.Logo => _logoImageFileNames,
                _ => Array.Empty<string>()
            };

            if (imageFileNames.Length == 0)
            {
                _logger.LogWarning("Attempted to load unexpected image type: {Type}", type);
                return new DynamicImageResponse { HasImage = false };
            }

            // Try attachments first
            var attachmentStream = _mediaSourceManager.GetMediaAttachments(item.Id)
                .FirstOrDefault(attachment => !string.IsNullOrEmpty(attachment.FileName)
                    && imageFileNames.Any(name => attachment.FileName.Contains(name, StringComparison.OrdinalIgnoreCase)));

            if (attachmentStream is not null)
            {
                return await ExtractAttachment(item, attachmentStream, mediaSource, cancellationToken).ConfigureAwait(false);
            }

            // Fall back to EmbeddedImage streams
            var imageStreams = _mediaSourceManager.GetMediaStreams(new MediaStreamQuery
            {
                ItemId = item.Id,
                Type = MediaStreamType.EmbeddedImage
            });

            if (imageStreams.Count == 0)
            {
                // Can't extract if we don't have any EmbeddedImage streams
                return new DynamicImageResponse { HasImage = false };
            }

            // Extract first stream containing an element of imageFileNames
            var imageStream = imageStreams
                .FirstOrDefault(stream => !string.IsNullOrEmpty(stream.Comment)
                    && imageFileNames.Any(name => stream.Comment.Contains(name, StringComparison.OrdinalIgnoreCase)));

            // Primary type only: default to first image if none found by label
            if (imageStream is null)
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

            var format = imageStream.Codec switch
            {
                "bmp" => ImageFormat.Bmp,
                "gif" => ImageFormat.Gif,
                "mjpeg" => ImageFormat.Jpg,
                "png" => ImageFormat.Png,
                "webp" => ImageFormat.Webp,
                _ => ImageFormat.Jpg
            };

            string extractedImagePath =
                await _mediaEncoder.ExtractVideoImage(item.Path, item.Container, mediaSource, imageStream, imageStream.Index, format, cancellationToken)
                    .ConfigureAwait(false);

            return new DynamicImageResponse
            {
                Format = format,
                HasImage = true,
                Path = extractedImagePath,
                Protocol = MediaProtocol.File
            };
        }

        private async Task<DynamicImageResponse> ExtractAttachment(Video item, MediaAttachment attachmentStream, MediaSourceInfo mediaSource, CancellationToken cancellationToken)
        {
            var extension = string.IsNullOrEmpty(attachmentStream.MimeType)
                ? Path.GetExtension(attachmentStream.FileName)
                : MimeTypes.ToExtension(attachmentStream.MimeType);

            ImageFormat format = extension switch
            {
                ".bmp" => ImageFormat.Bmp,
                ".gif" => ImageFormat.Gif,
                ".png" => ImageFormat.Png,
                ".webp" => ImageFormat.Webp,
                _ => ImageFormat.Jpg
            };

            string extractedAttachmentPath =
                await _mediaEncoder.ExtractVideoImage(item.Path, item.Container, mediaSource, null, attachmentStream.Index, format, cancellationToken)
                    .ConfigureAwait(false);

            return new DynamicImageResponse
            {
                Format = format,
                HasImage = true,
                Path = extractedAttachmentPath,
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
