using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Trickplay;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Trickplay;

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
    private readonly IServerConfigurationManager _config;
    private readonly IImageEncoder _imageEncoder;

    private static readonly SemaphoreSlim _resourcePool = new(1, 1);
    private static readonly string[] _trickplayImgExtensions = { ".jpg" };

    /// <summary>
    /// Initializes a new instance of the <see cref="TrickplayManager"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="itemRepo">The item repository.</param>
    /// <param name="mediaEncoder">The media encoder.</param>
    /// <param name="fileSystem">The file systen.</param>
    /// <param name="encodingHelper">The encoding helper.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="config">The server configuration manager.</param>
    /// <param name="imageEncoder">The image encoder.</param>
    public TrickplayManager(
        ILogger<TrickplayManager> logger,
        IItemRepository itemRepo,
        IMediaEncoder mediaEncoder,
        IFileSystem fileSystem,
        EncodingHelper encodingHelper,
        ILibraryManager libraryManager,
        IServerConfigurationManager config,
        IImageEncoder imageEncoder)
    {
        _logger = logger;
        _itemRepo = itemRepo;
        _mediaEncoder = mediaEncoder;
        _fileSystem = fileSystem;
        _encodingHelper = encodingHelper;
        _libraryManager = libraryManager;
        _config = config;
        _imageEncoder = imageEncoder;
    }

    /// <inheritdoc />
    public async Task RefreshTrickplayDataAsync(Video video, bool replace, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Trickplay refresh for {ItemId} (replace existing: {Replace})", video.Id, replace);

        var options = _config.Configuration.TrickplayOptions;
        foreach (var width in options.WidthResolutions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RefreshTrickplayDataInternal(
                video,
                replace,
                width,
                options,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RefreshTrickplayDataInternal(
        Video video,
        bool replace,
        int width,
        TrickplayOptions options,
        CancellationToken cancellationToken)
    {
        if (!CanGenerateTrickplay(video, options.Interval))
        {
            return;
        }

        var imgTempDir = string.Empty;
        var outputDir = GetTrickplayDirectory(video, width);

        await _resourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
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
                width,
                TimeSpan.FromMilliseconds(options.Interval),
                options.EnableHwAcceleration,
                options.ProcessThreads,
                options.Qscale,
                options.ProcessPriority,
                _encodingHelper,
                cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrEmpty(imgTempDir) || !Directory.Exists(imgTempDir))
            {
                throw new InvalidOperationException("Null or invalid directory from media encoder.");
            }

            var images = _fileSystem.GetFiles(imgTempDir, _trickplayImgExtensions, false, false)
                .Select(i => i.FullName)
                .OrderBy(i => i)
                .ToList();

            // Create tiles
            var tilesTempDir = Path.Combine(imgTempDir, Guid.NewGuid().ToString("N"));
            var tilesInfo = CreateTiles(images, width, options, tilesTempDir, outputDir);

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

    private TrickplayTilesInfo CreateTiles(List<string> images, int width, TrickplayOptions options, string workDir, string outputDir)
    {
        if (images.Count == 0)
        {
            throw new ArgumentException("Can't create trickplay from 0 images.");
        }

        Directory.CreateDirectory(workDir);

        var tilesInfo = new TrickplayTilesInfo
        {
            Width = width,
            Interval = options.Interval,
            TileWidth = options.TileWidth,
            TileHeight = options.TileHeight,
            TileCount = images.Count,
            // Set during image generation
            Height = 0,
            Bandwidth = 0
        };

        /*
         * Generate trickplay tile grids from sets of images
         */
        var imageOptions = new ImageCollageOptions
        {
            Width = tilesInfo.TileWidth,
            Height = tilesInfo.TileHeight
        };

        var tilesPerGrid = tilesInfo.TileWidth * tilesInfo.TileHeight;
        var requiredTileGrids = (int)Math.Ceiling((double)images.Count / tilesPerGrid);

        for (int i = 0; i < requiredTileGrids; i++)
        {
            // Set output/input paths
            var tileGridPath = Path.Combine(workDir, $"{i}.jpg");

            imageOptions.OutputPath = tileGridPath;
            imageOptions.InputPaths = images.Skip(i * tilesPerGrid).Take(tilesPerGrid).ToList();

            // Generate image and use returned height for tiles info
            var height = _imageEncoder.CreateTrickplayGrid(imageOptions, options.JpegQuality, tilesInfo.Width, tilesInfo.Height != 0 ? tilesInfo.Height : null);
            if (tilesInfo.Height == 0)
            {
                tilesInfo.Height = height;
            }

            // Update bitrate
            var bitrate = (int)Math.Ceiling((decimal)new FileInfo(tileGridPath).Length * 8 / tilesInfo.TileWidth / tilesInfo.TileHeight / (tilesInfo.Interval / 1000));
            tilesInfo.Bandwidth = Math.Max(tilesInfo.Bandwidth, bitrate);
        }

        /*
         * Move trickplay tiles to output directory
         */
        Directory.CreateDirectory(Directory.GetParent(outputDir)!.FullName);

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
        if (libraryOptions is null || !libraryOptions.EnableTrickplayImageExtraction)
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

    /// <inheritdoc />
    public string? GetHlsPlaylist(Guid itemId, int width, string? apiKey)
    {
        var tilesResolutions = GetTilesResolutions(itemId);
        if (tilesResolutions is not null && tilesResolutions.TryGetValue(width, out var tilesInfo))
        {
            var builder = new StringBuilder(128);

            if (tilesInfo.TileCount > 0)
            {
                const string urlFormat = "Trickplay/{0}/{1}.jpg?MediaSourceId={2}&api_key={3}";
                const string decimalFormat = "{0:0.###}";

                var resolution = $"{tilesInfo.Width}x{tilesInfo.Height}";
                var layout = $"{tilesInfo.TileWidth}x{tilesInfo.TileHeight}";
                var tilesPerGrid = tilesInfo.TileWidth * tilesInfo.TileHeight;
                var tileDuration = tilesInfo.Interval / 1000d;
                var infDuration = tileDuration * tilesPerGrid;
                var tileGridCount = (int)Math.Ceiling((decimal)tilesInfo.TileCount / tilesPerGrid);

                builder
                    .AppendLine("#EXTM3U")
                    .Append("#EXT-X-TARGETDURATION:")
                    .AppendLine(tileGridCount.ToString(CultureInfo.InvariantCulture))
                    .AppendLine("#EXT-X-VERSION:7")
                    .AppendLine("#EXT-X-MEDIA-SEQUENCE:1")
                    .AppendLine("#EXT-X-PLAYLIST-TYPE:VOD")
                    .AppendLine("#EXT-X-IMAGES-ONLY");

                for (int i = 0; i < tileGridCount; i++)
                {
                    // All tile grids before the last one must contain full amount of tiles.
                    // The final grid will be 0 < count <= maxTiles
                    if (i == tileGridCount - 1)
                    {
                        tilesPerGrid = tilesInfo.TileCount - (i * tilesPerGrid);
                        infDuration = tileDuration * tilesPerGrid;
                    }

                    // EXTINF
                    builder
                        .Append("#EXTINF:")
                        .AppendFormat(CultureInfo.InvariantCulture, decimalFormat, infDuration)
                        .AppendLine(",");

                    // EXT-X-TILES
                    builder
                        .Append("#EXT-X-TILES:RESOLUTION=")
                        .Append(resolution)
                        .Append(",LAYOUT=")
                        .Append(layout)
                        .Append(",DURATION=")
                        .AppendFormat(CultureInfo.InvariantCulture, decimalFormat, tileDuration)
                        .AppendLine();

                    // URL
                    builder
                        .AppendFormat(
                            CultureInfo.InvariantCulture,
                            urlFormat,
                            width.ToString(CultureInfo.InvariantCulture),
                            i.ToString(CultureInfo.InvariantCulture),
                            itemId.ToString("N"),
                            apiKey)
                        .AppendLine();
                }

                builder.AppendLine("#EXT-X-ENDLIST");
                return builder.ToString();
            }
        }

        return null;
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
        catch (IOException)
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
