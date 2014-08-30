using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.MediaInfo
{
    /// <summary>
    /// Uses ffmpeg to create video images
    /// </summary>
    public class AudioImageProvider : IDynamicImageProvider, IHasChangeMonitor
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

            // Can't extract if we didn't find a video stream in the file
            if (!audio.HasEmbeddedImage)
            {
                return Task.FromResult(new DynamicImageResponse { HasImage = false });
            }

            return GetImage((Audio)item, cancellationToken);
        }

        public async Task<DynamicImageResponse> GetImage(Audio item, CancellationToken cancellationToken)
        {
            var path = GetAudioImagePath(item);

            if (!File.Exists(path))
            {
                var semaphore = GetLock(path);

                // Acquire a lock
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    // Check again in case it was saved while waiting for the lock
                    if (!File.Exists(path))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));

                        using (var stream = await _mediaEncoder.ExtractAudioImage(item.Path, cancellationToken).ConfigureAwait(false))
                        {
                            using (var fileStream = _fileSystem.GetFileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                            {
                                await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                            }
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
            var album = item.Parent as MusicAlbum;

            var filename = item.Album ?? string.Empty;
            filename += item.Artists.FirstOrDefault() ?? string.Empty;
            filename += album == null ? item.Id.ToString("N") + "_primary" + item.DateModified.Ticks : album.Id.ToString("N") + album.DateModified.Ticks + "_primary";

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
            return item is Audio;
        }

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService, DateTime date)
        {
            return item.DateModified > date;
        }
    }
}
