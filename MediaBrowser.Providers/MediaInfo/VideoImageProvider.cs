using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.MediaInfo
{
    public class VideoImageProvider : IDynamicImageProvider, IHasOrder
    {
        private readonly IMediaEncoder _mediaEncoder;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public VideoImageProvider(IMediaEncoder mediaEncoder, ILogger<VideoImageProvider> logger, IFileSystem fileSystem)
        {
            _mediaEncoder = mediaEncoder;
            _logger = logger;
            _fileSystem = fileSystem;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType> { ImageType.Primary };
        }

        public Task<DynamicImageResponse> GetImage(BaseItem item, ImageType type, CancellationToken cancellationToken)
        {
            var video = (Video)item;

            // No support for this
            if (video.IsPlaceHolder)
            {
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }

            // No support for this
            if (video.VideoType == VideoType.Iso || video.VideoType == VideoType.Dvd || video.VideoType == VideoType.BluRay)
            {
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }

            // Can't extract if we didn't find a video stream in the file
            if (!video.DefaultVideoStreamIndex.HasValue)
            {
                _logger.LogInformation("Skipping image extraction due to missing DefaultVideoStreamIndex for {0}.", video.Path ?? string.Empty);
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }

            return GetVideoImage(video, cancellationToken);
        }

        public async Task<DynamicImageResponse> GetVideoImage(Video item, CancellationToken cancellationToken)
        {
            var protocol = item.PathProtocol ?? MediaProtocol.File;

            var inputPath = MediaEncoderHelpers.GetInputArgument(
                _fileSystem,
                item.Path,
                null,
                item.GetPlayableStreamFileNames());

            var mediaStreams =
                item.GetMediaStreams();

            var imageStreams =
                mediaStreams
                    .Where(i => i.Type == MediaStreamType.EmbeddedImage)
                    .ToList();

            var imageStream = imageStreams.FirstOrDefault(i => (i.Comment ?? string.Empty).IndexOf("front", StringComparison.OrdinalIgnoreCase) != -1) ??
                imageStreams.FirstOrDefault(i => (i.Comment ?? string.Empty).IndexOf("cover", StringComparison.OrdinalIgnoreCase) != -1) ??
                imageStreams.FirstOrDefault();

            string extractedImagePath;

            if (imageStream != null)
            {
                // Instead of using the raw stream index, we need to use nth video/embedded image stream
                var videoIndex = -1;
                foreach (var mediaStream in mediaStreams)
                {
                    if (mediaStream.Type == MediaStreamType.Video ||
                        mediaStream.Type == MediaStreamType.EmbeddedImage)
                    {
                        videoIndex++;
                    }
                    if (mediaStream == imageStream)
                    {
                        break;
                    }
                }

                extractedImagePath = await _mediaEncoder.ExtractVideoImage(inputPath, item.Container, protocol, imageStream, videoIndex, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // If we know the duration, grab it from 10% into the video. Otherwise just 10 seconds in.
                // Always use 10 seconds for dvd because our duration could be out of whack
                var imageOffset = item.VideoType != VideoType.Dvd && item.RunTimeTicks.HasValue &&
                                  item.RunTimeTicks.Value > 0
                                      ? TimeSpan.FromTicks(Convert.ToInt64(item.RunTimeTicks.Value * .1))
                                      : TimeSpan.FromSeconds(10);

                var videoStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Video);

                extractedImagePath = await _mediaEncoder.ExtractVideoImage(inputPath, item.Container, protocol, videoStream, item.Video3DFormat, imageOffset, cancellationToken).ConfigureAwait(false);
            }

            return new DynamicImageResponse
            {
                Format = ImageFormat.Jpg,
                HasImage = true,
                Path = extractedImagePath,
                Protocol = MediaProtocol.File
            };
        }

        public string Name => "Screen Grabber";

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

            var video = item as Video;

            if (video != null && !video.IsPlaceHolder && video.IsCompleteMedia)
            {
                return true;
            }

            return false;
        }
        // Make sure this comes after internet image providers
        public int Order => 100;
    }
}
