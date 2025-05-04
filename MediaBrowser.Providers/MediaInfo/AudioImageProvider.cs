#pragma warning disable CA1826 // CA1826 Do not use Enumerable methods on Indexable collections.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Providers.MediaInfo
{
    /// <summary>
    /// Uses <see cref="IMediaEncoder"/> to extract embedded images.
    /// </summary>
    public class AudioImageProvider : IDynamicImageProvider
    {
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioImageProvider"/> class.
        /// </summary>
        /// <param name="mediaSourceManager">The media source manager for fetching item streams.</param>
        /// <param name="mediaEncoder">The media encoder for extracting embedded images.</param>
        /// <param name="config">The server configuration manager for getting image paths.</param>
        /// <param name="fileSystem">The filesystem.</param>
        public AudioImageProvider(IMediaSourceManager mediaSourceManager, IMediaEncoder mediaEncoder, IServerConfigurationManager config, IFileSystem fileSystem)
        {
            _mediaSourceManager = mediaSourceManager;
            _mediaEncoder = mediaEncoder;
            _config = config;
            _fileSystem = fileSystem;
        }

        private string AudioImagesPath => Path.Combine(_config.ApplicationPaths.CachePath, "extracted-audio-images");

        /// <inheritdoc />
        public string Name => "Image Extractor";

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new[] { ImageType.Primary };
        }

        /// <inheritdoc />
        public Task<DynamicImageResponse> GetImage(BaseItem item, ImageType type, CancellationToken cancellationToken)
        {
            var imageStreams = _mediaSourceManager.GetMediaStreams(new MediaStreamQuery
            {
                ItemId = item.Id,
                Type = MediaStreamType.EmbeddedImage
            });

            // Can't extract if we didn't find a video stream in the file
            if (imageStreams.Count == 0)
            {
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }

            return GetImage((Audio)item, imageStreams, cancellationToken);
        }

        private async Task<DynamicImageResponse> GetImage(Audio item, IReadOnlyList<MediaStream> imageStreams, CancellationToken cancellationToken)
        {
            var path = GetAudioImagePath(item);

            if (!File.Exists(path))
            {
                var directoryName = Path.GetDirectoryName(path) ?? throw new InvalidOperationException($"Invalid path '{path}'");
                Directory.CreateDirectory(directoryName);
                var imageStream = imageStreams.FirstOrDefault(i => (i.Comment ?? string.Empty).Contains("front", StringComparison.OrdinalIgnoreCase)) ??
                    imageStreams.FirstOrDefault(i => (i.Comment ?? string.Empty).Contains("cover", StringComparison.OrdinalIgnoreCase)) ??
                    imageStreams.FirstOrDefault();
                var imageStreamIndex = imageStream?.Index;

                var tempFile = await _mediaEncoder.ExtractAudioImage(item.Path, imageStreamIndex, cancellationToken).ConfigureAwait(false);

                File.Copy(tempFile, path, true);

                try
                {
                    _fileSystem.DeleteFile(tempFile);
                }
                catch
                {
                }
            }

            return new DynamicImageResponse
            {
                HasImage = true,
                Path = path
            };
        }

        private string GetAudioImagePath(Audio item)
        {
            string filename;

            if (item.GetType() == typeof(Audio))
            {
                if (item.AlbumArtists.Count > 0
                    && !string.IsNullOrWhiteSpace(item.Album)
                    && !string.IsNullOrWhiteSpace(item.AlbumArtists[0]))
                {
                    filename = (item.Album + "-" + item.AlbumArtists[0]).GetMD5().ToString("N", CultureInfo.InvariantCulture);
                }
                else
                {
                    filename = item.Id.ToString("N", CultureInfo.InvariantCulture);
                }

                filename += ".jpg";
            }
            else
            {
                // If it's an audio book or audio podcast, allow unique image per item
                filename = item.Id.ToString("N", CultureInfo.InvariantCulture) + ".jpg";
            }

            var prefix = filename.AsSpan().Slice(0, 1);

            return Path.Join(AudioImagesPath, prefix, filename);
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

            return item is Audio;
        }
    }
}
