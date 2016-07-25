using System;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Providers.MediaInfo
{
    /// <summary>
    /// Uses ffmpeg to create video images
    /// </summary>
    public class AudioImageProvider : IDynamicImageProvider, IHasItemChangeMonitor
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        private readonly IMediaEncoder _mediaEncoder;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;

        public AudioImageProvider(IMediaEncoder mediaEncoder, IServerConfigurationManager config, IFileSystem fileSystem)
        {
            _mediaEncoder = mediaEncoder;
            _config = config;
            _fileSystem = fileSystem;
        }

        public IEnumerable<ImageType> GetSupportedImages(IHasImages item)
        {
            return new List<ImageType> { ImageType.Primary };
        }

        public Task<DynamicImageResponse> GetImage(IHasImages item, ImageType type, CancellationToken cancellationToken)
        {
            var audio = (Audio)item;

            var imageStreams =
                audio.GetMediaSources(false)
                    .Take(1)
                    .SelectMany(i => i.MediaStreams)
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

            if (!_fileSystem.FileExists(path))
            {
                var semaphore = GetLock(path);

                // Acquire a lock
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    // Check again in case it was saved while waiting for the lock
                    if (!_fileSystem.FileExists(path))
                    {
                        _fileSystem.CreateDirectory(Path.GetDirectoryName(path));

                        var imageStream = imageStreams.FirstOrDefault(i => (i.Comment ?? string.Empty).IndexOf("front", StringComparison.OrdinalIgnoreCase) != -1) ??
                            imageStreams.FirstOrDefault(i => (i.Comment ?? string.Empty).IndexOf("cover", StringComparison.OrdinalIgnoreCase) != -1) ??
                            imageStreams.FirstOrDefault();

                        var imageStreamIndex = imageStream == null ? (int?)null : imageStream.Index;

                        var tempFile = await _mediaEncoder.ExtractAudioImage(item.Path, imageStreamIndex, cancellationToken).ConfigureAwait(false);

                        File.Copy(tempFile, path, true);

                        try
                        {
                            File.Delete(tempFile);
                        }
                        catch
                        {

                        }
                    }
                }
                finally
                {
                    semaphore.Release();
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
            var filename = item.Album ?? string.Empty;
            filename += string.Join(",", item.Artists.ToArray());

            if (!string.IsNullOrWhiteSpace(item.Album))
            {
                filename += "_" + item.Album;
            }
            else if (!string.IsNullOrWhiteSpace(item.Name))
            {
                filename += "_" + item.Name;
            }
            else
            {
                filename += "_" + item.Id.ToString("N");
            }

            filename = filename.GetMD5() + ".jpg";

            var prefix = filename.Substring(0, 1);

            return Path.Combine(AudioImagesPath, prefix, filename);
        }

        public string AudioImagesPath
        {
            get
            {
                return Path.Combine(_config.ApplicationPaths.CachePath, "extracted-audio-images");
            }
        }

        /// <summary>
        /// Gets the lock.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>SemaphoreSlim.</returns>
        private SemaphoreSlim GetLock(string filename)
        {
            return _locks.GetOrAdd(filename, key => new SemaphoreSlim(1, 1));
        }

        public string Name
        {
            get { return "Image Extractor"; }
        }

        public bool Supports(IHasImages item)
        {
            var audio = item as Audio;

            return item.LocationType == LocationType.FileSystem && audio != null && !audio.IsArchive;
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
