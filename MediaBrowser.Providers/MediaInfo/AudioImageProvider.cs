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
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Providers.MediaInfo
{
    /// <summary>
    /// Uses ffmpeg to create video images
    /// </summary>
    public class AudioImageProvider : IDynamicImageProvider
    {
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;

        public AudioImageProvider(IMediaEncoder mediaEncoder, IServerConfigurationManager config, IFileSystem fileSystem)
        {
            _mediaEncoder = mediaEncoder;
            _config = config;
            _fileSystem = fileSystem;
        }

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new List<ImageType> { ImageType.Primary };
        }

        public Task<DynamicImageResponse> GetImage(BaseItem item, ImageType type, CancellationToken cancellationToken)
        {
            var audio = (Audio)item;

            var imageStreams =
                audio.GetMediaStreams(MediaStreamType.EmbeddedImage)
                    .Where(i => i.Type == MediaStreamType.EmbeddedImage)
                    .ToList();

            // Can't extract if we didn't find a video stream in the file
            if (imageStreams.Count == 0)
            {
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }

            return GetImage((Audio)item, imageStreams, cancellationToken);
        }

        public async Task<DynamicImageResponse> GetImage(Audio item, List<MediaStream> imageStreams, CancellationToken cancellationToken)
        {
            var path = GetAudioImagePath(item);

            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                var imageStream = imageStreams.FirstOrDefault(i => (i.Comment ?? string.Empty).IndexOf("front", StringComparison.OrdinalIgnoreCase) != -1) ??
                    imageStreams.FirstOrDefault(i => (i.Comment ?? string.Empty).IndexOf("cover", StringComparison.OrdinalIgnoreCase) != -1) ??
                    imageStreams.FirstOrDefault();

                var imageStreamIndex = imageStream == null ? (int?)null : imageStream.Index;

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
            string filename = null;

            if (item.GetType() == typeof(Audio))
            {
                var albumArtist = item.AlbumArtists.FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(item.Album) && !string.IsNullOrWhiteSpace(albumArtist))
                {
                    filename = (item.Album + "-" + albumArtist).GetMD5().ToString("N", CultureInfo.InvariantCulture);
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

            var prefix = filename.Substring(0, 1);

            return Path.Combine(AudioImagesPath, prefix, filename);
        }

        public string AudioImagesPath => Path.Combine(_config.ApplicationPaths.CachePath, "extracted-audio-images");

        public string Name => "Image Extractor";

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

            var audio = item as Audio;

            return audio != null;
        }
    }
}
