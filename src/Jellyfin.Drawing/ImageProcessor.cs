using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncKeyedLock;
using Jellyfin.Data.Entities;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Logging;
using Photo = MediaBrowser.Controller.Entities.Photo;

namespace Jellyfin.Drawing;

/// <summary>
/// Class ImageProcessor.
/// </summary>
public sealed class ImageProcessor : IImageProcessor, IDisposable
{
    // Increment this when there's a change requiring caches to be invalidated
    private const char Version = '3';

    private static readonly HashSet<string> _transparentImageTypes
        = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".webp", ".gif" };

    private readonly ILogger<ImageProcessor> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IServerApplicationPaths _appPaths;
    private readonly IImageEncoder _imageEncoder;

    private readonly AsyncNonKeyedLocker _parallelEncodingLimit;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageProcessor"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="appPaths">The server application paths.</param>
    /// <param name="fileSystem">The filesystem.</param>
    /// <param name="imageEncoder">The image encoder.</param>
    /// <param name="config">The configuration.</param>
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

    /// <inheritdoc />
    public IReadOnlyCollection<string> SupportedInputFormats =>
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "tiff",
            "tif",
            "jpeg",
            "jpg",
            "png",
            "aiff",
            "cr2",
            "crw",
            "nef",
            "orf",
            "pef",
            "arw",
            "webp",
            "gif",
            "bmp",
            "erf",
            "raf",
            "rw2",
            "nrw",
            "dng",
            "ico",
            "astc",
            "ktx",
            "pkm",
            "wbmp",
            "avif"
        };

    /// <inheritdoc />
    public bool SupportsImageCollageCreation => _imageEncoder.SupportsImageCollageCreation;

    /// <inheritdoc />
    public IReadOnlyCollection<ImageFormat> GetSupportedImageOutputFormats()
        => _imageEncoder.SupportedOutputFormats;

    /// <inheritdoc />
    public async Task<(string Path, string? MimeType, DateTime DateModified)> ProcessImage(ImageProcessingOptions options)
    {
        ItemImageInfo originalImage = options.Image;
        BaseItem item = options.Item;

        string originalImagePath = originalImage.Path;
        DateTime dateModified = originalImage.DateModified;
        ImageDimensions? originalImageSize = null;
        if (originalImage.Width > 0 && originalImage.Height > 0)
        {
            originalImageSize = new ImageDimensions(originalImage.Width, originalImage.Height);
        }

        var mimeType = MimeTypes.GetMimeType(originalImagePath);
        if (!_imageEncoder.SupportsImageEncoding)
        {
            return (originalImagePath, mimeType, dateModified);
        }

        var supportedImageInfo = await GetSupportedImage(originalImagePath, dateModified).ConfigureAwait(false);
        originalImagePath = supportedImageInfo.Path;

        // Original file doesn't exist, or original file is gif.
        if (!File.Exists(originalImagePath) || string.Equals(mimeType, MediaTypeNames.Image.Gif, StringComparison.OrdinalIgnoreCase))
        {
            return (originalImagePath, mimeType, dateModified);
        }

        dateModified = supportedImageInfo.DateModified;
        bool requiresTransparency = _transparentImageTypes.Contains(Path.GetExtension(originalImagePath));

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
                // Orientation unknown, so do it
                autoOrient = true;
                orientation = photo.Orientation;
            }
        }

        if (options.HasDefaultOptions(originalImagePath, originalImageSize) && (!autoOrient || !options.RequiresAutoOrientation))
        {
            // Just spit out the original file if all the options are default
            return (originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
        }

        int quality = options.Quality;

        ImageFormat outputFormat = GetOutputFormat(options.SupportedOutputFormats, requiresTransparency);
        string cacheFilePath = GetCacheFilePath(
            originalImagePath,
            options.Width,
            options.Height,
            options.MaxWidth,
            options.MaxHeight,
            options.FillWidth,
            options.FillHeight,
            quality,
            dateModified,
            outputFormat,
            options.PercentPlayed,
            options.UnplayedCount,
            options.Blur,
            options.BackgroundColor,
            options.ForegroundLayer);

        try
        {
            if (!File.Exists(cacheFilePath))
            {
                string resultPath;

                // Limit number of parallel (more precisely: concurrent) image encodings to prevent a high memory usage
                using (await _parallelEncodingLimit.LockAsync().ConfigureAwait(false))
                {
                    resultPath = _imageEncoder.EncodeImage(originalImagePath, dateModified, cacheFilePath, autoOrient, orientation, quality, options, outputFormat);
                }

                if (string.Equals(resultPath, originalImagePath, StringComparison.OrdinalIgnoreCase))
                {
                    return (originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
                }
            }

            return (cacheFilePath, outputFormat.GetMimeType(), _fileSystem.GetLastWriteTimeUtc(cacheFilePath));
        }
        catch (Exception ex)
        {
            // If it fails for whatever reason, return the original image
            _logger.LogError(ex, "Error encoding image");
            return (originalImagePath, MimeTypes.GetMimeType(originalImagePath), dateModified);
        }
    }

    private ImageFormat GetOutputFormat(IReadOnlyCollection<ImageFormat> clientSupportedFormats, bool requiresTransparency)
    {
        var serverFormats = GetSupportedImageOutputFormats();

        // Client doesn't care about format, so start with webp if supported
        if (serverFormats.Contains(ImageFormat.Webp) && clientSupportedFormats.Contains(ImageFormat.Webp))
        {
            return ImageFormat.Webp;
        }

        // If transparency is needed and webp isn't supported, than png is the only option
        if (requiresTransparency && clientSupportedFormats.Contains(ImageFormat.Png))
        {
            return ImageFormat.Png;
        }

        foreach (var format in clientSupportedFormats)
        {
            if (serverFormats.Contains(format))
            {
                return format;
            }
        }

        // We should never actually get here
        return ImageFormat.Jpg;
    }

    /// <summary>
    /// Gets the cache file path based on a set of parameters.
    /// </summary>
    private string GetCacheFilePath(
        string originalPath,
        int? width,
        int? height,
        int? maxWidth,
        int? maxHeight,
        int? fillWidth,
        int? fillHeight,
        int quality,
        DateTime dateModified,
        ImageFormat format,
        double percentPlayed,
        int? unwatchedCount,
        int? blur,
        string backgroundColor,
        string foregroundLayer)
    {
        var filename = new StringBuilder(256);
        filename.Append(originalPath);

        filename.Append(",quality=");
        filename.Append(quality);

        filename.Append(",datemodified=");
        filename.Append(dateModified.Ticks);

        filename.Append(",f=");
        filename.Append(format);

        if (width.HasValue)
        {
            filename.Append(",width=");
            filename.Append(width.Value);
        }

        if (height.HasValue)
        {
            filename.Append(",height=");
            filename.Append(height.Value);
        }

        if (maxWidth.HasValue)
        {
            filename.Append(",maxwidth=");
            filename.Append(maxWidth.Value);
        }

        if (maxHeight.HasValue)
        {
            filename.Append(",maxheight=");
            filename.Append(maxHeight.Value);
        }

        if (fillWidth.HasValue)
        {
            filename.Append(",fillwidth=");
            filename.Append(fillWidth.Value);
        }

        if (fillHeight.HasValue)
        {
            filename.Append(",fillheight=");
            filename.Append(fillHeight.Value);
        }

        if (percentPlayed > 0)
        {
            filename.Append(",p=");
            filename.Append(percentPlayed);
        }

        if (unwatchedCount.HasValue)
        {
            filename.Append(",p=");
            filename.Append(unwatchedCount.Value);
        }

        if (blur.HasValue)
        {
            filename.Append(",blur=");
            filename.Append(blur.Value);
        }

        if (!string.IsNullOrEmpty(backgroundColor))
        {
            filename.Append(",b=");
            filename.Append(backgroundColor);
        }

        if (!string.IsNullOrEmpty(foregroundLayer))
        {
            filename.Append(",fl=");
            filename.Append(foregroundLayer);
        }

        filename.Append(",v=");
        filename.Append(Version);

        return GetCachePath(ResizedImageCachePath, filename.ToString(), format.GetExtension());
    }

    /// <inheritdoc />
    public ImageDimensions GetImageDimensions(BaseItem item, ItemImageInfo info)
    {
        int width = info.Width;
        int height = info.Height;

        if (height > 0 && width > 0)
        {
            return new ImageDimensions(width, height);
        }

        string path = info.Path;
        _logger.LogDebug("Getting image size for item {ItemType} {Path}", item.GetType().Name, path);

        ImageDimensions size = GetImageDimensions(path);
        info.Width = size.Width;
        info.Height = size.Height;

        return size;
    }

    /// <inheritdoc />
    public ImageDimensions GetImageDimensions(string path)
        => _imageEncoder.GetImageSize(path);

    /// <inheritdoc />
    public string GetImageBlurHash(string path)
    {
        var size = GetImageDimensions(path);
        return GetImageBlurHash(path, size);
    }

    /// <inheritdoc />
    public string GetImageBlurHash(string path, ImageDimensions imageDimensions)
    {
        if (imageDimensions.Width <= 0 || imageDimensions.Height <= 0)
        {
            return string.Empty;
        }

        // We want tiles to be as close to square as possible, and to *mostly* keep under 16 tiles for performance.
        // One tile is (width / xComp) x (height / yComp) pixels, which means that ideally yComp = xComp * height / width.
        // See more at https://github.com/woltapp/blurhash/#how-do-i-pick-the-number-of-x-and-y-components
        float xCompF = MathF.Sqrt(16.0f * imageDimensions.Width / imageDimensions.Height);
        float yCompF = xCompF * imageDimensions.Height / imageDimensions.Width;

        int xComp = Math.Min((int)xCompF + 1, 9);
        int yComp = Math.Min((int)yCompF + 1, 9);

        return _imageEncoder.GetImageBlurHash(xComp, yComp, path);
    }

    /// <inheritdoc />
    public string GetImageCacheTag(BaseItem item, ItemImageInfo image)
        => (item.Path + image.DateModified.Ticks).GetMD5().ToString("N", CultureInfo.InvariantCulture);

    /// <inheritdoc />
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

    /// <inheritdoc />
    public string? GetImageCacheTag(User user)
    {
        if (user.ProfileImage is null)
        {
            return null;
        }

        return (user.ProfileImage.Path + user.ProfileImage.LastModified.Ticks).GetMD5()
            .ToString("N", CultureInfo.InvariantCulture);
    }

    private Task<(string Path, DateTime DateModified)> GetSupportedImage(string originalImagePath, DateTime dateModified)
    {
        var inputFormat = Path.GetExtension(originalImagePath.AsSpan()).TrimStart('.').ToString();

        // These are just jpg files renamed as tbn
        if (string.Equals(inputFormat, "tbn", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult((originalImagePath, dateModified));
        }

        return Task.FromResult((originalImagePath, dateModified));
    }

    /// <summary>
    /// Gets the cache path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="uniqueName">Name of the unique.</param>
    /// <param name="fileExtension">The file extension.</param>
    /// <returns>System.String.</returns>
    /// <exception cref="ArgumentNullException">
    /// path
    /// or
    /// uniqueName
    /// or
    /// fileExtension.
    /// </exception>
    public string GetCachePath(string path, string uniqueName, string fileExtension)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentException.ThrowIfNullOrEmpty(uniqueName);
        ArgumentException.ThrowIfNullOrEmpty(fileExtension);

        var filename = uniqueName.GetMD5() + fileExtension;

        return GetCachePath(path, filename);
    }

    /// <summary>
    /// Gets the cache path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="filename">The filename.</param>
    /// <returns>System.String.</returns>
    /// <exception cref="ArgumentNullException">
    /// path
    /// or
    /// filename.
    /// </exception>
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

    /// <inheritdoc />
    public void CreateImageCollage(ImageCollageOptions options, string? libraryName)
    {
        _logger.LogInformation("Creating image collage and saving to {Path}", options.OutputPath);

        _imageEncoder.CreateImageCollage(options, libraryName);

        _logger.LogInformation("Completed creation of image collage and saved to {Path}", options.OutputPath);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_imageEncoder is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _parallelEncodingLimit?.Dispose();

        _disposed = true;
    }
}
