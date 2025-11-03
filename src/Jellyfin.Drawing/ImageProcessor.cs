using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncKeyedLock;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Logging;
using Photo = MediaBrowser.Controller.Entities.Photo;

namespace Jellyfin.Drawing;

/// <summary>
/// Provides optimized image processing functionality for Jellyfin
/// with concurrency-limited encoding and cache-aware performance.
/// </summary>
public sealed class ImageProcessor : IImageProcessor, IDisposable
{
    private const char Version = '3';

    private static readonly HashSet<string> TransparentExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".webp", ".gif", ".svg" };

    private readonly ILogger<ImageProcessor> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IServerApplicationPaths _appPaths;
    private readonly IImageEncoder _imageEncoder;
    private readonly AsyncNonKeyedLocker _parallelEncodingLimit;

    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageProcessor"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="appPaths">The application paths.</param>
    /// <param name="fileSystem">The file system interface.</param>
    /// <param name="imageEncoder">The image encoder.</param>
    /// <param name="config">The configuration manager.</param>
    public ImageProcessor(
        ILogger<ImageProcessor> logger,
        IServerApplicationPaths appPaths,
        IFileSystem fileSystem,
        IImageEncoder imageEncoder,
        IServerConfigurationManager config)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _imageEncoder = imageEncoder;
        _appPaths = appPaths;

        var semaphoreCount = config.Configuration.ParallelImageEncodingLimit;
        if (semaphoreCount < 1)
        {
            semaphoreCount = Environment.ProcessorCount;
        }

        _parallelEncodingLimit = new(semaphoreCount);
    }

    private string ResizedImageCachePath => Path.Combine(_appPaths.ImageCachePath, "resized-images");

    /// <inheritdoc/>
    public IReadOnlyCollection<string> SupportedInputFormats => _imageEncoder.SupportedInputFormats;

    /// <inheritdoc/>
    public bool SupportsImageCollageCreation => _imageEncoder.SupportsImageCollageCreation;

    /// <inheritdoc/>
    public IReadOnlyCollection<ImageFormat> GetSupportedImageOutputFormats() => _imageEncoder.SupportedOutputFormats;

    /// <inheritdoc/>
    public async Task<(string Path, string? MimeType, DateTime DateModified)> ProcessImage(ImageProcessingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var originalImage = options.Image;
        var item = options.Item;

        string originalImagePath = originalImage.Path;
        DateTime dateModified = originalImage.DateModified;
        ImageDimensions? originalImageSize = null;

        if (originalImage.Width > 0 && originalImage.Height > 0)
        {
            originalImageSize = new ImageDimensions(originalImage.Width, originalImage.Height);
        }

        if (!_imageEncoder.SupportsImageEncoding)
        {
            return (originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
        }

        var supported = await GetSupportedImage(originalImagePath, dateModified).ConfigureAwait(false);
        originalImagePath = supported.Path;
        dateModified = supported.DateModified;

        if (!File.Exists(originalImagePath) || IsGif(originalImagePath))
        {
            return (originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
        }

        bool autoOrient = false;
        ImageOrientation? orientation = null;

        if (item is Photo photo)
        {
            if (photo.Orientation.HasValue)
            {
                if (photo.Orientation.Value != ImageOrientation.TopLeft)
                {
                    autoOrient = true;
                    orientation = photo.Orientation;
                }
            }
            else
            {
                autoOrient = true;
                orientation = photo.Orientation;
            }
        }

        if (options.HasDefaultOptions(originalImagePath, originalImageSize)
            && (!autoOrient || !options.RequiresAutoOrientation))
        {
            return (originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
        }

        bool needsTransparency = TransparentExtensions.Contains(Path.GetExtension(originalImagePath));
        ImageFormat outputFormat = SelectOutputFormat(options.SupportedOutputFormats, needsTransparency);

        string cacheFilePath = BuildCacheFilePath(originalImagePath, options, dateModified, outputFormat);

        try
        {
            if (!File.Exists(cacheFilePath))
            {
                using (await _parallelEncodingLimit.LockAsync().ConfigureAwait(false))
                {
                    var resultPath = _imageEncoder.EncodeImage(
                        originalImagePath,
                        dateModified,
                        cacheFilePath,
                        autoOrient && options.RequiresAutoOrientation,
                        orientation,
                        options.Quality,
                        options,
                        outputFormat);

                    if (string.Equals(resultPath, originalImagePath, StringComparison.OrdinalIgnoreCase))
                    {
                        return (originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
                    }
                }
            }

            var lastWrite = _fileSystem.GetLastWriteTimeUtc(cacheFilePath);
            return (cacheFilePath, outputFormat.GetMimeType(), lastWrite);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encoding image: {Path}", originalImagePath);
            return (originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
        }
    }

    private static bool IsGif(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".gif", StringComparison.OrdinalIgnoreCase);
    }

    private static ImageFormat SelectOutputFormat(IReadOnlyCollection<ImageFormat> clientSupportedFormats, bool requiresTransparency)
    {
        if (clientSupportedFormats.Contains(ImageFormat.Webp))
        {
            return ImageFormat.Webp;
        }

        if (requiresTransparency && clientSupportedFormats.Contains(ImageFormat.Png))
        {
            return ImageFormat.Png;
        }

        if (clientSupportedFormats.Contains(ImageFormat.Jpg))
        {
            return ImageFormat.Jpg;
        }

        return ImageFormat.Jpg;
    }

    private string BuildCacheFilePath(
        string originalPath,
        ImageProcessingOptions options,
        DateTime dateModified,
        ImageFormat format)
    {
        var sb = new StringBuilder(256);
        sb.Append(originalPath);
        sb.Append(",q=").Append(options.Quality);
        sb.Append(",dm=").Append(dateModified.Ticks);
        sb.Append(",f=").Append(format);

        if (options.Width.HasValue)
        {
            sb.Append(",w=").Append(options.Width.Value);
        }

        if (options.Height.HasValue)
        {
            sb.Append(",h=").Append(options.Height.Value);
        }

        if (options.MaxWidth.HasValue)
        {
            sb.Append(",mw=").Append(options.MaxWidth.Value);
        }

        if (options.MaxHeight.HasValue)
        {
            sb.Append(",mh=").Append(options.MaxHeight.Value);
        }

        if (options.FillWidth.HasValue)
        {
            sb.Append(",fw=").Append(options.FillWidth.Value);
        }

        if (options.FillHeight.HasValue)
        {
            sb.Append(",fh=").Append(options.FillHeight.Value);
        }

        if (options.PercentPlayed > 0)
        {
            sb.Append(",p=").Append(options.PercentPlayed.ToString(CultureInfo.InvariantCulture));
        }

        if (options.UnplayedCount.HasValue)
        {
            sb.Append(",up=").Append(options.UnplayedCount.Value);
        }

        if (options.Blur.HasValue)
        {
            sb.Append(",bl=").Append(options.Blur.Value);
        }

        if (!string.IsNullOrEmpty(options.BackgroundColor))
        {
            sb.Append(",bg=").Append(options.BackgroundColor);
        }

        if (!string.IsNullOrEmpty(options.ForegroundLayer))
        {
            sb.Append(",fl=").Append(options.ForegroundLayer);
        }

        sb.Append(",v=").Append(Version);
        var fileName = sb.ToString().GetMD5() + format.GetExtension();
        return GetCachePath(ResizedImageCachePath, fileName);
    }

    /// <inheritdoc/>
    public ImageDimensions GetImageDimensions(BaseItem item, ItemImageInfo info)
    {
        if (info.Width > 0 && info.Height > 0)
        {
            return new ImageDimensions(info.Width, info.Height);
        }

        var path = info.Path;
        _logger.LogDebug("Reading image size for {ItemType} {Path}", item.GetType().Name, path);
        var size = GetImageDimensions(path);
        info.Width = size.Width;
        info.Height = size.Height;
        return size;
    }

    /// <inheritdoc/>
    public ImageDimensions GetImageDimensions(string path) => _imageEncoder.GetImageSize(path);

    /// <inheritdoc/>
    public string GetImageBlurHash(string path)
    {
        var size = GetImageDimensions(path);
        return GetImageBlurHash(path, size);
    }

    /// <inheritdoc/>
    public string GetImageBlurHash(string path, ImageDimensions imageDimensions)
    {
        if (imageDimensions.Width <= 0 || imageDimensions.Height <= 0)
        {
            return string.Empty;
        }

        float xCompF = MathF.Sqrt(16.0f * imageDimensions.Width / imageDimensions.Height);
        float yCompF = xCompF * imageDimensions.Height / imageDimensions.Width;

        int xComp = Math.Min((int)xCompF + 1, 9);
        int yComp = Math.Min((int)yCompF + 1, 9);

        return _imageEncoder.GetImageBlurHash(xComp, yComp, path);
    }

    /// <inheritdoc/>
    public string GetImageCacheTag(string baseItemPath, DateTime imageDateModified)
    {
        return (baseItemPath + imageDateModified.Ticks.ToString(CultureInfo.InvariantCulture))
            .GetMD5()
            .ToString("N", CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public string GetImageCacheTag(BaseItem item, ItemImageInfo image)
        => GetImageCacheTag(item.Path, image.DateModified);

    /// <inheritdoc/>
    public string GetImageCacheTag(BaseItemDto item, ItemImageInfo image)
        => GetImageCacheTag(item.Path, image.DateModified);

    /// <inheritdoc/>
    public string? GetImageCacheTag(BaseItemDto item, ChapterInfo chapter)
    {
        if (chapter.ImagePath is null)
        {
            return null;
        }

        return GetImageCacheTag(item.Path, chapter.ImageDateModified);
    }

    /// <inheritdoc/>
    public string? GetImageCacheTag(BaseItem item, ChapterInfo chapter)
    {
        if (chapter.ImagePath is null)
        {
            return null;
        }

        return GetImageCacheTag(item, new ItemImageInfo
        {
            Path = chapter.ImagePath,
            Type = ImageType.Chapter,
            DateModified = chapter.ImageDateModified
        });
    }

    /// <inheritdoc/>
    public string? GetImageCacheTag(User user)
    {
        if (user.ProfileImage is null)
        {
            return null;
        }

        return GetImageCacheTag(user.ProfileImage.Path, user.ProfileImage.LastModified);
    }

    private static Task<(string Path, DateTime DateModified)> GetSupportedImage(string originalImagePath, DateTime dateModified)
    {
        return Task.FromResult((originalImagePath, dateModified));
    }

    /// <summary>
    /// Builds the cache path for the resized image.
    /// </summary>
    /// <param name="path">The base path.</param>
    /// <param name="uniqueName">The unique name.</param>
    /// <param name="fileExtension">The file extension.</param>
    /// <returns>The combined cache file path.</returns>
    public string GetCachePath(string path, string uniqueName, string fileExtension)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(uniqueName);
        ArgumentException.ThrowIfNullOrEmpty(fileExtension);

        var filename = uniqueName.GetMD5() + fileExtension;
        return GetCachePath(path, filename);
    }

    /// <summary>
    /// Builds a cache path with a shard prefix for directory balancing.
    /// </summary>
    /// <param name="path">The base directory.</param>
    /// <param name="filename">The filename.</param>
    /// <returns>The sharded full path.</returns>
    public string GetCachePath(ReadOnlySpan<char> path, ReadOnlySpan<char> filename)
    {
        if (path.IsEmpty)
        {
            throw new ArgumentException("Path can't be empty.", nameof(path));
        }

        if (filename.IsEmpty)
        {
            throw new ArgumentException("Filename can't be empty.", nameof(filename));
        }

        var prefix = filename.Slice(0, 1);
        return Path.Join(path, prefix, filename);
    }

    /// <inheritdoc/>
    public void CreateImageCollage(ImageCollageOptions options, string? libraryName)
    {
        _logger.LogDebug("Creating image collage → {Path}", options.OutputPath);
        _imageEncoder.CreateImageCollage(options, libraryName);
        _logger.LogDebug("Collage created → {Path}", options.OutputPath);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_imageEncoder is IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
            }
            catch
            {
                // ignore
            }
        }

        _parallelEncodingLimit.Dispose();
        _disposed = true;
    }
}
