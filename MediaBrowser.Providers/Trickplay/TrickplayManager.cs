using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Trickplay;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace MediaBrowser.Providers.Trickplay
{
    /// <summary>
    /// ITrickplayManager implementation.
    /// </summary>
    public class TrickplayManager : ITrickplayManager
    {
        private readonly ILogger<TrickplayManager> _logger;
        private readonly IItemRepository _itemRepo;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IFileSystem _fileSystem;
        private readonly EncodingHelper _encodingHelper;
        private readonly ILibraryManager _libraryManager;

        private static readonly SemaphoreSlim _resourcePool = new(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="TrickplayManager"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="itemRepo">The item repository.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        /// <param name="fileSystem">The file systen.</param>
        /// <param name="encodingHelper">The encoding helper.</param>
        /// <param name="libraryManager">The library manager.</param>
        public TrickplayManager(
            ILogger<TrickplayManager> logger,
            IItemRepository itemRepo,
            IMediaEncoder mediaEncoder,
            IFileSystem fileSystem,
            EncodingHelper encodingHelper,
            ILibraryManager libraryManager)
        {
            _logger = logger;
            _itemRepo = itemRepo;
            _mediaEncoder = mediaEncoder;
            _fileSystem = fileSystem;
            _encodingHelper = encodingHelper;
            _libraryManager = libraryManager;
        }

        /// <inheritdoc />
        public async Task RefreshTrickplayData(Video video, bool replace, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Trickplay refresh for {ItemId} (replace existing: {Replace})", video.Id, replace);

            foreach (var width in new int[] { 320 } /*todo conf*/)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await RefreshTrickplayData(video, replace, width, 10000/*todo conf*/, 10/*todo conf*/, 10/*todo conf*/, true/*todo conf*/, true/*todo conf*/, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task RefreshTrickplayData(Video video, bool replace, int width, int interval, int tileWidth, int tileHeight, bool doHwAccel, bool doHwEncode, CancellationToken cancellationToken)
        {
            if (!CanGenerateTrickplay(video, interval))
            {
                return;
            }

            var imgTempDir = string.Empty;
            var outputDir = GetTrickplayDirectory(video, width);

            try
            {
                await _resourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (!replace && Directory.Exists(outputDir) && GetTilesResolutions(video.Id).ContainsKey(width))
                {
                    _logger.LogDebug("Found existing trickplay files for {ItemId}. Exiting.", video.Id);
                    return;
                }

                // Extract images
                // Note: Media sources under parent items exist as their own video/item as well. Only use this video stream for trickplay.
                var mediaSource = video.GetMediaSources(false).Find(source => Guid.Parse(source.Id).Equals(video.Id));

                if (mediaSource is null)
                {
                    _logger.LogDebug("Found no matching media source for item {ItemId}", video.Id);
                    return;
                }

                var mediaPath = mediaSource.Path;
                var mediaStream = mediaSource.VideoStream;
                var container = mediaSource.Container;

                _logger.LogInformation("Creating trickplay files at {Width} width, for {Path} [ID: {ItemId}]", width, mediaPath, video.Id);
                imgTempDir = await _mediaEncoder.ExtractVideoImagesOnIntervalAccelerated(
                    mediaPath,
                    container,
                    mediaSource,
                    mediaStream,
                    TimeSpan.FromMilliseconds(interval),
                    width,
                    doHwAccel,
                    doHwEncode,
                    _encodingHelper,
                    cancellationToken).ConfigureAwait(false);

                if (string.IsNullOrEmpty(imgTempDir) || !Directory.Exists(imgTempDir))
                {
                    throw new InvalidOperationException("Null or invalid directory from media encoder.");
                }

                var images = _fileSystem.GetFiles(imgTempDir, new string[] { ".jpg" }, false, false)
                    .Where(img => string.Equals(img.Extension, ".jpg", StringComparison.Ordinal))
                    .OrderBy(i => i.FullName)
                    .ToList();

                // Create tiles
                var tilesTempDir = Path.Combine(imgTempDir, Guid.NewGuid().ToString("N"));
                var tilesInfo = CreateTiles(images, width, interval, tileWidth, tileHeight, 100/* todo _config.JpegQuality*/, tilesTempDir, outputDir);

                // Save tiles info
                try
                {
                    if (tilesInfo is not null)
                    {
                        SaveTilesInfo(video.Id, tilesInfo);
                        _logger.LogInformation("Finished creation of trickplay files for {0}", mediaPath);
                    }
                    else
                    {
                        throw new InvalidOperationException("Null trickplay tiles info from CreateTiles.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while saving trickplay tiles info.");

                    // Make sure no files stay in metadata folders on failure
                    // if tiles info wasn't saved.
                    Directory.Delete(outputDir, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trickplay images.");
            }
            finally
            {
                _resourcePool.Release();

                if (!string.IsNullOrEmpty(imgTempDir))
                {
                    Directory.Delete(imgTempDir, true);
                }
            }
        }

        private TrickplayTilesInfo CreateTiles(List<FileSystemMetadata> images, int width, int interval, int tileWidth, int tileHeight, int quality, string workDir, string outputDir)
        {
            if (images.Count == 0)
            {
                throw new InvalidOperationException("Can't create trickplay from 0 images.");
            }

            Directory.CreateDirectory(workDir);

            var tilesInfo = new TrickplayTilesInfo
            {
                Width = width,
                Interval = interval,
                TileWidth = tileWidth,
                TileHeight = tileHeight,
                TileCount = 0,
                Bandwidth = 0
            };

            var firstImg = SKBitmap.Decode(images[0].FullName);
            if (firstImg == null)
            {
                throw new InvalidDataException("Could not decode image data.");
            }

            tilesInfo.Height = firstImg.Height;
            if (tilesInfo.Width != firstImg.Width)
            {
                throw new InvalidOperationException("Image width does not match config width.");
            }

            /*
             * Generate grids of trickplay image tiles
             */
            var imgNo = 0;
            var i = 0;
            while (i < images.Count)
            {
                var tileGrid = new SKBitmap(tilesInfo.Width * tilesInfo.TileWidth, tilesInfo.Height * tilesInfo.TileHeight);

                using (var canvas = new SKCanvas(tileGrid))
                {
                    for (var y = 0; y < tilesInfo.TileHeight; y++)
                    {
                        for (var x = 0; x < tilesInfo.TileWidth; x++)
                        {
                            if (i >= images.Count)
                            {
                                break;
                            }

                            var img = SKBitmap.Decode(images[i].FullName);
                            if (img == null)
                            {
                                throw new InvalidDataException("Could not decode image data.");
                            }

                            if (tilesInfo.Width != img.Width)
                            {
                                throw new InvalidOperationException("Image width does not match config width.");
                            }

                            if (tilesInfo.Height != img.Height)
                            {
                                throw new InvalidOperationException("Image height does not match first image height.");
                            }

                            canvas.DrawBitmap(img, x * tilesInfo.Width, y * tilesInfo.Height);
                            tilesInfo.TileCount++;
                            i++;
                        }
                    }
                }

                // Output each tile grid to singular file
                var tileGridPath = Path.Combine(workDir, $"{imgNo}.jpg");
                using (var stream = File.OpenWrite(tileGridPath))
                {
                    tileGrid.Encode(stream, SKEncodedImageFormat.Jpeg, quality);
                }

                var bitrate = (int)Math.Ceiling((decimal)new FileInfo(tileGridPath).Length * 8 / tilesInfo.TileWidth / tilesInfo.TileHeight / (tilesInfo.Interval / 1000));
                tilesInfo.Bandwidth = Math.Max(tilesInfo.Bandwidth, bitrate);

                imgNo++;
            }

            /*
             * Move trickplay tiles to output directory
             */
            Directory.CreateDirectory(outputDir);

            // Replace existing tile grids if they already exist
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }

            MoveDirectory(workDir, outputDir);

            return tilesInfo;
        }

        private bool CanGenerateTrickplay(Video video, int interval)
        {
            var videoType = video.VideoType;
            if (videoType == VideoType.Iso || videoType == VideoType.Dvd || videoType == VideoType.BluRay)
            {
                return false;
            }

            if (video.IsPlaceHolder)
            {
                return false;
            }

            if (video.IsShortcut)
            {
                return false;
            }

            if (!video.IsCompleteMedia)
            {
                return false;
            }

            if (!video.RunTimeTicks.HasValue || video.RunTimeTicks.Value < TimeSpan.FromMilliseconds(interval).Ticks)
            {
                return false;
            }

            var libraryOptions = _libraryManager.GetLibraryOptions(video);
            if (libraryOptions is not null)
            {
                if (!libraryOptions.EnableTrickplayImageExtraction)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            // Can't extract images if there are no video streams
            return video.GetMediaStreams().Count > 0;
        }

        /// <inheritdoc />
        public Dictionary<int, TrickplayTilesInfo> GetTilesResolutions(Guid itemId)
        {
            return _itemRepo.GetTilesResolutions(itemId);
        }

        /// <inheritdoc />
        public void SaveTilesInfo(Guid itemId, TrickplayTilesInfo tilesInfo)
        {
            _itemRepo.SaveTilesInfo(itemId, tilesInfo);
        }

        /// <inheritdoc />
        public Dictionary<Guid, Dictionary<int, TrickplayTilesInfo>> GetTrickplayManifest(BaseItem item)
        {
            return _itemRepo.GetTrickplayManifest(item);
        }

        /// <inheritdoc />
        public string GetTrickplayTilePath(BaseItem item, int width, int index)
        {
            return Path.Combine(GetTrickplayDirectory(item, width), index + ".jpg");
        }

        private string GetTrickplayDirectory(BaseItem item, int? width = null)
        {
            var path = Path.Combine(item.GetInternalMetadataPath(), "trickplay");

            return width.HasValue ? Path.Combine(path, width.Value.ToString(CultureInfo.InvariantCulture)) : path;
        }

        private void MoveDirectory(string source, string destination)
        {
            try
            {
                Directory.Move(source, destination);
            }
            catch (System.IO.IOException)
            {
                // Cross device move requires a copy
                Directory.CreateDirectory(destination);
                foreach (string file in Directory.GetFiles(source))
                {
                    File.Copy(file, Path.Join(destination, Path.GetFileName(file)), true);
                }

                Directory.Delete(source, true);
            }
        }
    }
}
