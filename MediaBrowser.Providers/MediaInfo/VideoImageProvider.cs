using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Providers.MediaInfo
{
    public class VideoImageProvider : IDynamicImageProvider, IHasItemChangeMonitor, IHasOrder
    {
        private readonly IIsoManager _isoManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IServerConfigurationManager _config;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public VideoImageProvider(IIsoManager isoManager, IMediaEncoder mediaEncoder, IServerConfigurationManager config, ILibraryManager libraryManager, ILogger logger, IFileSystem fileSystem)
        {
            _isoManager = isoManager;
            _mediaEncoder = mediaEncoder;
            _config = config;
            _libraryManager = libraryManager;
            _logger = logger;
            _fileSystem = fileSystem;
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
            if (video.VideoType == VideoType.HdDvd || video.IsPlaceHolder)
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
                _logger.Info("Skipping image extraction due to missing DefaultVideoStreamIndex for {0}.", video.Path ?? string.Empty);
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }

            return GetVideoImage(video, cancellationToken);
        }

        public async Task<DynamicImageResponse> GetVideoImage(Video item, CancellationToken cancellationToken)
        {
            var isoMount = await MountIsoIfNeeded(item, cancellationToken).ConfigureAwait(false);

            try
            {
                var protocol = item.LocationType == LocationType.Remote
                    ? MediaProtocol.Http
                    : MediaProtocol.File;

                var inputPath = MediaEncoderHelpers.GetInputArgument(_fileSystem, item.Path, protocol, isoMount, item.PlayableStreamFileNames);

                var mediaStreams =
                    item.GetMediaSources(false)
                        .Take(1)
                        .SelectMany(i => i.MediaStreams)
                        .ToList();

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

                    extractedImagePath = await _mediaEncoder.ExtractVideoImage(inputPath, protocol, videoIndex, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // If we know the duration, grab it from 10% into the video. Otherwise just 10 seconds in.
                    // Always use 10 seconds for dvd because our duration could be out of whack
                    var imageOffset = item.VideoType != VideoType.Dvd && item.RunTimeTicks.HasValue &&
                                      item.RunTimeTicks.Value > 0
                                          ? TimeSpan.FromTicks(Convert.ToInt64(item.RunTimeTicks.Value * .1))
                                          : TimeSpan.FromSeconds(10);

                    extractedImagePath = await _mediaEncoder.ExtractVideoImage(inputPath, protocol, item.Video3DFormat, imageOffset, cancellationToken).ConfigureAwait(false);
                }

                return new DynamicImageResponse
                {
                    Format = ImageFormat.Jpg,
                    HasImage = true,
                    Path = extractedImagePath,
                    Protocol = MediaProtocol.File
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
            get { return "Screen Grabber"; }
        }

        public bool Supports(IHasImages item)
        {
            var video = item as Video;

            if (item.LocationType == LocationType.FileSystem && video != null && !video.IsPlaceHolder &&
                !video.IsShortcut && !video.IsArchive)
            {
                return true;
            }

            return false;
        }

        public int Order
        {
            get
            {
                // Make sure this comes after internet image providers
                return 100;
            }
        }

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService)
        {
            var file = directoryService.GetFile(item.Path);
            if (file != null && file.LastWriteTimeUtc != item.DateModified)
            {
                return true;
            }

            return false;
        }
    }
}
