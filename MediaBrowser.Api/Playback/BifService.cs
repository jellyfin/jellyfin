using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using ServiceStack;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback
{
    [Route("/Videos/{Id}/index.bif", "GET")]
    public class GetBifFile
    {
        [ApiMember(Name = "MediaSourceId", Description = "The media version id, if playing an alternate version", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string MediaSourceId { get; set; }

        [ApiMember(Name = "MaxWidth", Description = "Optional. The maximum horizontal resolution of the encoded video.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? MaxWidth { get; set; }

        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    public class BifService : BaseApiService
    {
        private readonly IServerApplicationPaths _appPaths;
        private readonly ILibraryManager _libraryManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IFileSystem _fileSystem;

        public BifService(IServerApplicationPaths appPaths, ILibraryManager libraryManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem)
        {
            _appPaths = appPaths;
            _libraryManager = libraryManager;
            _mediaEncoder = mediaEncoder;
            _fileSystem = fileSystem;
        }

        public object Get(GetBifFile request)
        {
            return ToStaticFileResult(GetBifFile(request).Result);
        }

        private async Task<string> GetBifFile(GetBifFile request)
        {
            var widthVal = request.MaxWidth.HasValue ? request.MaxWidth.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;

            var item = _libraryManager.GetItemById(request.Id);
            var mediaSources = ((IHasMediaSources)item).GetMediaSources(false).ToList();
            var mediaSource = mediaSources.FirstOrDefault(i => string.Equals(i.Id, request.MediaSourceId)) ?? mediaSources.First();

            var path = Path.Combine(_appPaths.ImageCachePath, "bif", request.Id, request.MediaSourceId, widthVal, "index.bif");

            if (File.Exists(path))
            {
                return path;
            }

            var protocol = mediaSource.Protocol;

            var inputPath = MediaEncoderHelpers.GetInputArgument(mediaSource.Path, protocol, null, mediaSource.PlayableStreamFileNames);

            var semaphore = GetLock(path);

            await semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (File.Exists(path))
                {
                    return path;
                }
                
                await _mediaEncoder.ExtractVideoImagesOnInterval(inputPath, protocol, mediaSource.Video3DFormat,
                        TimeSpan.FromSeconds(10), Path.GetDirectoryName(path), "img_", request.MaxWidth, CancellationToken.None)
                        .ConfigureAwait(false);

                var images = new DirectoryInfo(Path.GetDirectoryName(path))
                    .EnumerateFiles()
                    .Where(img => string.Equals(img.Extension, ".jpg", StringComparison.Ordinal))
                    .OrderBy(i => i.FullName)
                    .ToList();

                using (var fs = _fileSystem.GetFileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                {
                    var magicNumber = new byte[] { 0x89, 0x42, 0x49, 0x46, 0x0d, 0x0a, 0x1a, 0x0a };
                    await fs.WriteAsync(magicNumber, 0, magicNumber.Length);

                    // version
                    var bytes = GetBytes(0);
                    await fs.WriteAsync(bytes, 0, bytes.Length);

                    // image count
                    bytes = GetBytes(images.Count);
                    await fs.WriteAsync(bytes, 0, bytes.Length);

                    // interval in ms
                    bytes = GetBytes(10000);
                    await fs.WriteAsync(bytes, 0, bytes.Length);

                    // reserved
                    for (var i = 20; i <= 63; i++)
                    {
                        bytes = new byte[] { 0x00 };
                        await fs.WriteAsync(bytes, 0, bytes.Length);
                    }

                    // write the bif index
                    var index = 0;
                    long imageOffset = 64 + (8 * images.Count) + 8;

                    foreach (var img in images)
                    {
                        bytes = GetBytes(index);
                        await fs.WriteAsync(bytes, 0, bytes.Length);

                        bytes = GetBytes(imageOffset);
                        await fs.WriteAsync(bytes, 0, bytes.Length);

                        imageOffset += img.Length;

                        index++;
                    }

                    bytes = new byte[] { 0xff, 0xff, 0xff, 0xff };
                    await fs.WriteAsync(bytes, 0, bytes.Length);

                    bytes = GetBytes(imageOffset);
                    await fs.WriteAsync(bytes, 0, bytes.Length);

                    // write the images
                    foreach (var img in images)
                    {
                        using (var imgStream = _fileSystem.GetFileStream(img.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, true))
                        {
                            await imgStream.CopyToAsync(fs).ConfigureAwait(false);
                        }
                    }
                }

                return path;
            }
            finally
            {
                semaphore.Release();
            }
        }

        private byte[] GetBytes(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }

        private byte[] GetBytes(long value)
        {
            var intVal = Convert.ToInt32(value);
            return GetBytes(intVal);

            //byte[] bytes = BitConverter.GetBytes(value);
            //if (BitConverter.IsLittleEndian)
            //    Array.Reverse(bytes);
            //return bytes;
        }

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> SemaphoreLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// Gets the lock.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.Object.</returns>
        private static SemaphoreSlim GetLock(string filename)
        {
            return SemaphoreLocks.GetOrAdd(filename, key => new SemaphoreSlim(1, 1));
        }
    }
}
