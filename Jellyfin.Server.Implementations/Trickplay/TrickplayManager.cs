using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncKeyedLock;
using Jellyfin.Data.Entities;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Trickplay;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Trickplay;

/// <summary>
/// ITrickplayManager implementation.
/// </summary>
public class TrickplayManager : ITrickplayManager
{
    private readonly ILogger<TrickplayManager> _logger;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly IFileSystem _fileSystem;
    private readonly EncodingHelper _encodingHelper;
    private readonly ILibraryManager _libraryManager;
    private readonly IServerConfigurationManager _config;
    private readonly IImageEncoder _imageEncoder;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly IApplicationPaths _appPaths;

    private static readonly AsyncNonKeyedLocker _resourcePool = new(1);
    private static readonly string[] _trickplayImgExtensions = { ".jpg" };

    /// <summary>
    /// Initializes a new instance of the <see cref="TrickplayManager"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="mediaEncoder">The media encoder.</param>
    /// <param name="fileSystem">The file systen.</param>
    /// <param name="encodingHelper">The encoding helper.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="config">The server configuration manager.</param>
    /// <param name="imageEncoder">The image encoder.</param>
    /// <param name="dbProvider">The database provider.</param>
    /// <param name="appPaths">The application paths.</param>
    public TrickplayManager(
        ILogger<TrickplayManager> logger,
        IMediaEncoder mediaEncoder,
        IFileSystem fileSystem,
        EncodingHelper encodingHelper,
        ILibraryManager libraryManager,
        IServerConfigurationManager config,
        IImageEncoder imageEncoder,
        IDbContextFactory<JellyfinDbContext> dbProvider,
        IApplicationPaths appPaths)
    {
        _logger = logger;
        _mediaEncoder = mediaEncoder;
        _fileSystem = fileSystem;
        _encodingHelper = encodingHelper;
        _libraryManager = libraryManager;
        _config = config;
        _imageEncoder = imageEncoder;
        _dbProvider = dbProvider;
        _appPaths = appPaths;
    }

    /// <inheritdoc />
    public async Task MoveGeneratedTrickplayDataAsync(Video video, LibraryOptions? libraryOptions, CancellationToken cancellationToken)
    {
        var options = _config.Configuration.TrickplayOptions;
        if (!CanGenerateTrickplay(video, options.Interval))
        {
            return;
        }

        var existingTrickplayResolutions = await GetTrickplayResolutions(video.Id).ConfigureAwait(false);
        foreach (var resolution in existingTrickplayResolutions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var existingResolution = resolution.Key;
            var tileWidth = resolution.Value.TileWidth;
            var tileHeight = resolution.Value.TileHeight;
            var shouldBeSavedWithMedia = libraryOptions is null ? false : libraryOptions.SaveTrickplayWithMedia;
            var localOutputDir = GetTrickplayDirectory(video, tileWidth, tileHeight, existingResolution, false);
            var mediaOutputDir = GetTrickplayDirectory(video, tileWidth, tileHeight, existingResolution, true);
            if (shouldBeSavedWithMedia && Directory.Exists(localOutputDir))
            {
                var localDirFiles = Directory.GetFiles(localOutputDir);
                var mediaDirExists = Directory.Exists(mediaOutputDir);
                if (localDirFiles.Length > 0 && ((mediaDirExists && Directory.GetFiles(mediaOutputDir).Length == 0) || !mediaDirExists))
                {
                    // Move images from local dir to media dir
                    MoveContent(localOutputDir, mediaOutputDir);
                    _logger.LogInformation("Moved trickplay images for {ItemName} to {Location}", video.Name, mediaOutputDir);
                }
            }
            else if (!shouldBeSavedWithMedia && Directory.Exists(mediaOutputDir))
            {
                var mediaDirFiles = Directory.GetFiles(mediaOutputDir);
                var localDirExists = Directory.Exists(localOutputDir);
                if (mediaDirFiles.Length > 0 && ((localDirExists && Directory.GetFiles(localOutputDir).Length == 0) || !localDirExists))
                {
                    // Move images from media dir to local dir
                    MoveContent(mediaOutputDir, localOutputDir);
                    _logger.LogInformation("Moved trickplay images for {ItemName} to {Location}", video.Name, localOutputDir);
                }
            }
        }
    }

    private void MoveContent(string sourceFolder, string destinationFolder)
    {
        _fileSystem.MoveDirectory(sourceFolder, destinationFolder);
        var parent = Directory.GetParent(sourceFolder);
        if (parent is not null)
        {
            var parentContent = Directory.GetDirectories(parent.FullName);
            if (parentContent.Length == 0)
            {
                Directory.Delete(parent.FullName);
            }
        }
    }

    /// <inheritdoc />
    public async Task RefreshTrickplayDataAsync(Video video, bool replace, LibraryOptions? libraryOptions, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Trickplay refresh for {ItemId} (replace existing: {Replace})", video.Id, replace);

        var options = _config.Configuration.TrickplayOptions;
        if (options.Interval < 1000)
        {
            _logger.LogWarning("Trickplay image interval {Interval} is too small, reset to the minimum valid value of 1000", options.Interval);
            options.Interval = 1000;
        }

        foreach (var width in options.WidthResolutions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RefreshTrickplayDataInternal(
                video,
                replace,
                width,
                options,
                libraryOptions,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RefreshTrickplayDataInternal(
        Video video,
        bool replace,
        int width,
        TrickplayOptions options,
        LibraryOptions? libraryOptions,
        CancellationToken cancellationToken)
    {
        if (!CanGenerateTrickplay(video, options.Interval))
        {
            return;
        }

        var imgTempDir = string.Empty;

        using (await _resourcePool.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                // Extract images
                // Note: Media sources under parent items exist as their own video/item as well. Only use this video stream for trickplay.
                var mediaSource = video.GetMediaSources(false).Find(source => Guid.Parse(source.Id).Equals(video.Id));

                if (mediaSource is null)
                {
                    _logger.LogDebug("Found no matching media source for item {ItemId}", video.Id);
                    return;
                }

                var mediaPath = mediaSource.Path;
                if (!File.Exists(mediaPath))
                {
                    _logger.LogWarning("Media not found at {Path} for item {ItemID}", mediaPath, video.Id);
                    return;
                }

                // We support video backdrops, but we should not generate trickplay images for them
                var parentDirectory = Directory.GetParent(mediaPath);
                if (parentDirectory is not null && string.Equals(parentDirectory.Name, "backdrops", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Ignoring backdrop media found at {Path} for item {ItemID}", mediaPath, video.Id);
                    return;
                }

                // The width has to be even, otherwise a lot of filters will not be able to sample it
                var actualWidth = 2 * (width / 2);

                // Force using the video width when the trickplay setting has a too large width
                if (mediaSource.VideoStream.Width is not null && mediaSource.VideoStream.Width < width)
                {
                    _logger.LogWarning("Video width {VideoWidth} is smaller than trickplay setting {TrickPlayWidth}, using video width for thumbnails", mediaSource.VideoStream.Width, width);
                    actualWidth = 2 * ((int)mediaSource.VideoStream.Width / 2);
                }

                var tileWidth = options.TileWidth;
                var tileHeight = options.TileHeight;
                var saveWithMedia = libraryOptions is null ? false : libraryOptions.SaveTrickplayWithMedia;
                var outputDir = GetTrickplayDirectory(video, tileWidth, tileHeight, actualWidth, saveWithMedia);

                // Import existing trickplay tiles
                if (!replace && Directory.Exists(outputDir))
                {
                    var existingFiles = Directory.GetFiles(outputDir);
                    if (existingFiles.Length > 0)
                    {
                        var hasTrickplayResolution = await HasTrickplayResolutionAsync(video.Id, actualWidth).ConfigureAwait(false);
                        if (hasTrickplayResolution)
                        {
                            _logger.LogDebug("Found existing trickplay files for {ItemId}.", video.Id);
                            return;
                        }

                        // Import tiles
                        var localTrickplayInfo = new TrickplayInfo
                        {
                            ItemId = video.Id,
                            Width = width,
                            Interval = options.Interval,
                            TileWidth = options.TileWidth,
                            TileHeight = options.TileHeight,
                            ThumbnailCount = existingFiles.Length,
                            Height = 0,
                            Bandwidth = 0
                        };

                        foreach (var tile in existingFiles)
                        {
                            var image = _imageEncoder.GetImageSize(tile);
                            localTrickplayInfo.Height = Math.Max(localTrickplayInfo.Height, (int)Math.Ceiling((double)image.Height / localTrickplayInfo.TileHeight));
                            var bitrate = (int)Math.Ceiling((decimal)new FileInfo(tile).Length * 8 / localTrickplayInfo.TileWidth / localTrickplayInfo.TileHeight / (localTrickplayInfo.Interval / 1000));
                            localTrickplayInfo.Bandwidth = Math.Max(localTrickplayInfo.Bandwidth, bitrate);
                        }

                        await SaveTrickplayInfo(localTrickplayInfo).ConfigureAwait(false);

                        _logger.LogDebug("Imported existing trickplay files for {ItemId}.", video.Id);
                        return;
                    }
                }

                // Generate trickplay tiles
                var mediaStream = mediaSource.VideoStream;
                var container = mediaSource.Container;

                _logger.LogInformation("Creating trickplay files at {Width} width, for {Path} [ID: {ItemId}]", actualWidth, mediaPath, video.Id);
                imgTempDir = await _mediaEncoder.ExtractVideoImagesOnIntervalAccelerated(
                    mediaPath,
                    container,
                    mediaSource,
                    mediaStream,
                    actualWidth,
                    TimeSpan.FromMilliseconds(options.Interval),
                    options.EnableHwAcceleration,
                    options.EnableHwEncoding,
                    options.ProcessThreads,
                    options.Qscale,
                    options.ProcessPriority,
                    options.EnableKeyFrameOnlyExtraction,
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
                var trickplayInfo = CreateTiles(images, actualWidth, options, outputDir);

                // Save tiles info
                try
                {
                    if (trickplayInfo is not null)
                    {
                        trickplayInfo.ItemId = video.Id;
                        await SaveTrickplayInfo(trickplayInfo).ConfigureAwait(false);

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
                if (!string.IsNullOrEmpty(imgTempDir))
                {
                    Directory.Delete(imgTempDir, true);
                }
            }
        }
    }

    /// <inheritdoc />
    public TrickplayInfo CreateTiles(IReadOnlyList<string> images, int width, TrickplayOptions options, string outputDir)
    {
        if (images.Count == 0)
        {
            throw new ArgumentException("Can't create trickplay from 0 images.");
        }

        var workDir = Path.Combine(_appPaths.TempDirectory, "trickplay_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);

        var trickplayInfo = new TrickplayInfo
        {
            Width = width,
            Interval = options.Interval,
            TileWidth = options.TileWidth,
            TileHeight = options.TileHeight,
            ThumbnailCount = images.Count,
            // Set during image generation
            Height = 0,
            Bandwidth = 0
        };

        /*
         * Generate trickplay tiles from sets of thumbnails
         */
        var imageOptions = new ImageCollageOptions
        {
            Width = trickplayInfo.TileWidth,
            Height = trickplayInfo.TileHeight
        };

        var thumbnailsPerTile = trickplayInfo.TileWidth * trickplayInfo.TileHeight;
        var requiredTiles = (int)Math.Ceiling((double)images.Count / thumbnailsPerTile);

        for (int i = 0; i < requiredTiles; i++)
        {
            // Set output/input paths
            var tilePath = Path.Combine(workDir, $"{i}.jpg");

            imageOptions.OutputPath = tilePath;
            imageOptions.InputPaths = images.Skip(i * thumbnailsPerTile).Take(Math.Min(thumbnailsPerTile, images.Count - (i * thumbnailsPerTile))).ToList();

            // Generate image and use returned height for tiles info
            var height = _imageEncoder.CreateTrickplayTile(imageOptions, options.JpegQuality, trickplayInfo.Width, trickplayInfo.Height != 0 ? trickplayInfo.Height : null);
            if (trickplayInfo.Height == 0)
            {
                trickplayInfo.Height = height;
            }

            // Update bitrate
            var bitrate = (int)Math.Ceiling(new FileInfo(tilePath).Length * 8m / trickplayInfo.TileWidth / trickplayInfo.TileHeight / (trickplayInfo.Interval / 1000m));
            trickplayInfo.Bandwidth = Math.Max(trickplayInfo.Bandwidth, bitrate);
        }

        /*
         * Move trickplay tiles to output directory
         */
        Directory.CreateDirectory(Directory.GetParent(outputDir)!.FullName);

        // Replace existing tiles if they already exist
        if (Directory.Exists(outputDir))
        {
            Directory.Delete(outputDir, true);
        }

        _fileSystem.MoveDirectory(workDir, outputDir);

        return trickplayInfo;
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
    public async Task<Dictionary<int, TrickplayInfo>> GetTrickplayResolutions(Guid itemId)
    {
        var trickplayResolutions = new Dictionary<int, TrickplayInfo>();

        var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var trickplayInfos = await dbContext.TrickplayInfos
                .AsNoTracking()
                .Where(i => i.ItemId.Equals(itemId))
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var info in trickplayInfos)
            {
                trickplayResolutions[info.Width] = info;
            }
        }

        return trickplayResolutions;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TrickplayInfo>> GetTrickplayItemsAsync(int limit, int offset)
    {
        IReadOnlyList<TrickplayInfo> trickplayItems;

        var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            trickplayItems = await dbContext.TrickplayInfos
                .AsNoTracking()
                .OrderBy(i => i.ItemId)
                .Skip(offset)
                .Take(limit)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        return trickplayItems;
    }

    /// <inheritdoc />
    public async Task SaveTrickplayInfo(TrickplayInfo info)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            var oldInfo = await dbContext.TrickplayInfos.FindAsync(info.ItemId, info.Width).ConfigureAwait(false);
            if (oldInfo is not null)
            {
                dbContext.TrickplayInfos.Remove(oldInfo);
            }

            dbContext.Add(info);

            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, Dictionary<int, TrickplayInfo>>> GetTrickplayManifest(BaseItem item)
    {
        var trickplayManifest = new Dictionary<string, Dictionary<int, TrickplayInfo>>();
        foreach (var mediaSource in item.GetMediaSources(false))
        {
            if (mediaSource.IsRemote || !Guid.TryParse(mediaSource.Id, out var mediaSourceId))
            {
                continue;
            }

            var trickplayResolutions = await GetTrickplayResolutions(mediaSourceId).ConfigureAwait(false);

            if (trickplayResolutions.Count > 0)
            {
                trickplayManifest[mediaSource.Id] = trickplayResolutions;
            }
        }

        return trickplayManifest;
    }

    /// <inheritdoc />
    public async Task<string> GetTrickplayTilePathAsync(BaseItem item, int width, int index, bool saveWithMedia)
    {
        var trickplayResolutions = await GetTrickplayResolutions(item.Id).ConfigureAwait(false);
        if (trickplayResolutions is not null && trickplayResolutions.TryGetValue(width, out var trickplayInfo))
        {
            return Path.Combine(GetTrickplayDirectory(item, trickplayInfo.TileWidth, trickplayInfo.TileHeight, width, saveWithMedia), index + ".jpg");
        }

        return string.Empty;
    }

    /// <inheritdoc />
    public async Task<string?> GetHlsPlaylist(Guid itemId, int width, string? apiKey)
    {
        var trickplayResolutions = await GetTrickplayResolutions(itemId).ConfigureAwait(false);
        if (trickplayResolutions is not null && trickplayResolutions.TryGetValue(width, out var trickplayInfo))
        {
            var builder = new StringBuilder(128);

            if (trickplayInfo.ThumbnailCount > 0)
            {
                const string urlFormat = "{0}.jpg?MediaSourceId={1}&api_key={2}";
                const string decimalFormat = "{0:0.###}";

                var resolution = $"{trickplayInfo.Width}x{trickplayInfo.Height}";
                var layout = $"{trickplayInfo.TileWidth}x{trickplayInfo.TileHeight}";
                var thumbnailsPerTile = trickplayInfo.TileWidth * trickplayInfo.TileHeight;
                var thumbnailDuration = trickplayInfo.Interval / 1000d;
                var infDuration = thumbnailDuration * thumbnailsPerTile;
                var tileCount = (int)Math.Ceiling((decimal)trickplayInfo.ThumbnailCount / thumbnailsPerTile);

                builder
                    .AppendLine("#EXTM3U")
                    .Append("#EXT-X-TARGETDURATION:")
                    .AppendLine(tileCount.ToString(CultureInfo.InvariantCulture))
                    .AppendLine("#EXT-X-VERSION:7")
                    .AppendLine("#EXT-X-MEDIA-SEQUENCE:1")
                    .AppendLine("#EXT-X-PLAYLIST-TYPE:VOD")
                    .AppendLine("#EXT-X-IMAGES-ONLY");

                for (int i = 0; i < tileCount; i++)
                {
                    // All tiles prior to the last must contain full amount of thumbnails (no black).
                    if (i == tileCount - 1)
                    {
                        thumbnailsPerTile = trickplayInfo.ThumbnailCount - (i * thumbnailsPerTile);
                        infDuration = thumbnailDuration * thumbnailsPerTile;
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
                        .AppendFormat(CultureInfo.InvariantCulture, decimalFormat, thumbnailDuration)
                        .AppendLine();

                    // URL
                    builder
                        .AppendFormat(
                            CultureInfo.InvariantCulture,
                            urlFormat,
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

    /// <inheritdoc />
    public string GetTrickplayDirectory(BaseItem item, int tileWidth, int tileHeight, int width, bool saveWithMedia = false)
    {
        var path = saveWithMedia
            ? Path.Combine(item.ContainingFolderPath, Path.ChangeExtension(item.Path, ".trickplay"))
            : Path.Combine(item.GetInternalMetadataPath(), "trickplay");

        var subdirectory = string.Format(
            CultureInfo.InvariantCulture,
            "{0} - {1}x{2}",
            width.ToString(CultureInfo.InvariantCulture),
            tileWidth.ToString(CultureInfo.InvariantCulture),
            tileHeight.ToString(CultureInfo.InvariantCulture));

        return Path.Combine(path, subdirectory);
    }

    private async Task<bool> HasTrickplayResolutionAsync(Guid itemId, int width)
    {
        var dbContext = await _dbProvider.CreateDbContextAsync().ConfigureAwait(false);
        await using (dbContext.ConfigureAwait(false))
        {
            return await dbContext.TrickplayInfos
                .AsNoTracking()
                .Where(i => i.ItemId.Equals(itemId))
                .AnyAsync(i => i.Width == width)
                .ConfigureAwait(false);
        }
    }
}
