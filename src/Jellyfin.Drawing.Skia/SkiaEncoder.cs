using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BlurHashSharp.SkiaSharp;
using Jellyfin.Extensions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Drawing;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using Svg.Skia;

namespace Jellyfin.Drawing.Skia;

/// <summary>
/// Image encoder that uses <see cref="SkiaSharp"/> to manipulate images.
/// </summary>
public class SkiaEncoder : IImageEncoder
{
    private const string SvgFormat = "svg";
    private static readonly HashSet<string> _transparentImageTypes = new(StringComparer.OrdinalIgnoreCase) { ".png", ".gif", ".webp" };
    private readonly ILogger<SkiaEncoder> _logger;
    private readonly IApplicationPaths _appPaths;
    private static readonly SKImageFilter _imageFilter;

#pragma warning disable CA1810
    static SkiaEncoder()
#pragma warning restore CA1810
    {
        var kernel = new[]
        {
            0,    -.1f,    0,
            -.1f, 1.4f, -.1f,
            0,    -.1f,    0,
        };

        var kernelSize = new SKSizeI(3, 3);
        var kernelOffset = new SKPointI(1, 1);
        _imageFilter = SKImageFilter.CreateMatrixConvolution(
            kernelSize,
            kernel,
            1f,
            0f,
            kernelOffset,
            SKShaderTileMode.Clamp,
            true);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SkiaEncoder"/> class.
    /// </summary>
    /// <param name="logger">The application logger.</param>
    /// <param name="appPaths">The application paths.</param>
    public SkiaEncoder(ILogger<SkiaEncoder> logger, IApplicationPaths appPaths)
    {
        _logger = logger;
        _appPaths = appPaths;
    }

    /// <inheritdoc/>
    public string Name => "Skia";

    /// <inheritdoc/>
    public bool SupportsImageCollageCreation => true;

    /// <inheritdoc/>
    public bool SupportsImageEncoding => true;

    /// <inheritdoc/>
    public IReadOnlyCollection<string> SupportedInputFormats =>
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "jpeg",
            "jpg",
            "png",
            "dng",
            "webp",
            "gif",
            "bmp",
            "ico",
            "astc",
            "ktx",
            "pkm",
            "wbmp",
            // TODO: check if these are supported on multiple platforms
            // https://github.com/google/skia/blob/master/infra/bots/recipes/test.py#L454
            // working on windows at least
            "cr2",
            "nef",
            "arw",
            SvgFormat
        };

    /// <inheritdoc/>
    public IReadOnlyCollection<ImageFormat> SupportedOutputFormats
        => new HashSet<ImageFormat> { ImageFormat.Webp, ImageFormat.Jpg, ImageFormat.Png, ImageFormat.Svg };

    /// <summary>
    /// Check if the native lib is available.
    /// </summary>
    /// <returns>True if the native lib is available, otherwise false.</returns>
    public static bool IsNativeLibAvailable()
    {
        try
        {
            // test an operation that requires the native library
            SKPMColor.PreMultiply(SKColors.Black);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Convert a <see cref="ImageFormat"/> to a <see cref="SKEncodedImageFormat"/>.
    /// </summary>
    /// <param name="selectedFormat">The format to convert.</param>
    /// <returns>The converted format.</returns>
    public static SKEncodedImageFormat GetImageFormat(ImageFormat selectedFormat)
    {
        return selectedFormat switch
        {
            ImageFormat.Bmp => SKEncodedImageFormat.Bmp,
            ImageFormat.Jpg => SKEncodedImageFormat.Jpeg,
            ImageFormat.Gif => SKEncodedImageFormat.Gif,
            ImageFormat.Webp => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Png
        };
    }

    /// <inheritdoc />
    /// <exception cref="FileNotFoundException">The path is not valid.</exception>
    public ImageDimensions GetImageSize(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("File not found", path);
        }

        var extension = Path.GetExtension(path.AsSpan());
        if (extension.Equals(".svg", StringComparison.OrdinalIgnoreCase))
        {
            using var svg = new SKSvg();
            try
            {
                using var picture = svg.Load(path);
                if (picture is null)
                {
                    _logger.LogError("Unable to determine image dimensions for {FilePath}", path);
                    return default;
                }

                return new ImageDimensions(Convert.ToInt32(picture.CullRect.Width), Convert.ToInt32(picture.CullRect.Height));
            }
            catch (FormatException skiaColorException)
            {
                // This exception is known to be thrown on vector images that define custom styles
                // Skia SVG is not able to handle that and as the repository is quite stale and has not received updates we just catch them
                _logger.LogDebug(skiaColorException, "There was a issue loading the requested svg file");
                return default;
            }
        }

        using var codec = SKCodec.Create(path, out SKCodecResult result);
        switch (result)
        {
            case SKCodecResult.Success:
                var info = codec.Info;
                return new ImageDimensions(info.Width, info.Height);
            case SKCodecResult.Unimplemented:
                _logger.LogDebug("Image format not supported: {FilePath}", path);
                return default;
            default:
                _logger.LogError("Unable to determine image dimensions for {FilePath}: {SkCodecResult}", path, result);
                return default;
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    /// <exception cref="FileNotFoundException">The path is not valid.</exception>
    public string GetImageBlurHash(int xComp, int yComp, string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var extension = Path.GetExtension(path.AsSpan()).TrimStart('.');
        if (!SupportedInputFormats.Contains(extension, StringComparison.OrdinalIgnoreCase)
            || extension.Equals(SvgFormat, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Unable to compute blur hash due to unsupported format: {ImagePath}", path);
            return string.Empty;
        }

        // Use FileStream with FileShare.Read instead of having Skia open the file to allow concurrent read access
        using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        // Any larger than 128x128 is too slow and there's no visually discernible difference
        return BlurHashEncoder.Encode(xComp, yComp, fileStream, 128, 128);
    }

    private bool RequiresSpecialCharacterHack(string path)
    {
        for (int i = 0; i < path.Length; i++)
        {
            if (char.GetUnicodeCategory(path[i]) == UnicodeCategory.OtherLetter)
            {
                return true;
            }
        }

        return path.HasDiacritics();
    }

    private string NormalizePath(string path)
    {
        if (!RequiresSpecialCharacterHack(path))
        {
            return path;
        }

        var tempPath = Path.Join(_appPaths.TempDirectory, string.Concat("skia_", Guid.NewGuid().ToString(), Path.GetExtension(path.AsSpan())));
        var directory = Path.GetDirectoryName(tempPath) ?? throw new ResourceNotFoundException($"Provided path ({tempPath}) is not valid.");
        Directory.CreateDirectory(directory);
        File.Copy(path, tempPath, true);

        return tempPath;
    }

    private static SKEncodedOrigin GetSKEncodedOrigin(ImageOrientation? orientation)
    {
        if (!orientation.HasValue)
        {
            return SKEncodedOrigin.Default;
        }

        return (SKEncodedOrigin)orientation.Value;
    }

    /// <summary>
    /// Decode an image.
    /// </summary>
    /// <param name="path">The filepath of the image to decode.</param>
    /// <param name="forceCleanBitmap">Whether to force clean the bitmap.</param>
    /// <param name="orientation">The orientation of the image.</param>
    /// <param name="origin">The detected origin of the image.</param>
    /// <returns>The resulting bitmap of the image.</returns>
    internal SKBitmap? Decode(string path, bool forceCleanBitmap, ImageOrientation? orientation, out SKEncodedOrigin origin)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("File not found", path);
        }

        var requiresTransparencyHack = _transparentImageTypes.Contains(Path.GetExtension(path));

        if (requiresTransparencyHack || forceCleanBitmap)
        {
            using SKCodec codec = SKCodec.Create(NormalizePath(path), out SKCodecResult res);
            if (res != SKCodecResult.Success)
            {
                origin = GetSKEncodedOrigin(orientation);
                return null;
            }

            if (codec.FrameCount != 0)
            {
                throw new ArgumentException("Cannot decode images with multiple frames");
            }

            // create the bitmap
            SKBitmap? bitmap = null;
            try
            {
                bitmap = new SKBitmap(codec.Info.Width, codec.Info.Height, !requiresTransparencyHack);

                // decode
                _ = codec.GetPixels(bitmap.Info, bitmap.GetPixels());

                origin = codec.EncodedOrigin;

                return bitmap!;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Detected intermediary error decoding image {0}", path);
                bitmap?.Dispose();
                throw;
            }
        }

        var resultBitmap = SKBitmap.Decode(NormalizePath(path));

        if (resultBitmap is null)
        {
            return Decode(path, true, orientation, out origin);
        }

        try
        {
             // If we have to resize these they often end up distorted
            if (resultBitmap.ColorType == SKColorType.Gray8)
            {
                using (resultBitmap)
                {
                    return Decode(path, true, orientation, out origin);
                }
            }

            origin = SKEncodedOrigin.TopLeft;
            return resultBitmap;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Detected intermediary error decoding image {0}", path);
            resultBitmap?.Dispose();
            throw;
        }
    }

    private SKBitmap? GetBitmap(string path, bool autoOrient, ImageOrientation? orientation)
    {
        if (autoOrient)
        {
            var bitmap = Decode(path, true, orientation, out var origin);

            if (bitmap is not null && origin != SKEncodedOrigin.TopLeft)
            {
                using (bitmap)
                {
                    return OrientImage(bitmap, origin);
                }
            }

            return bitmap;
        }

        return Decode(path, false, orientation, out _);
    }

    private SKBitmap? GetBitmapFromSvg(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("File not found", path);
        }

        using var svg = SKSvg.CreateFromFile(path);
        if (svg.Drawable is null)
        {
            return null;
        }

        var width = (int)Math.Round(svg.Drawable.Bounds.Width);
        var height = (int)Math.Round(svg.Drawable.Bounds.Height);

        SKBitmap? bitmap = null;
        try
        {
            bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);
            canvas.DrawPicture(svg.Picture);
            canvas.Flush();
            canvas.Save();

            return bitmap!;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Detected intermediary error extracting image {0}", path);
            bitmap?.Dispose();
            throw;
        }
    }

    private SKBitmap OrientImage(SKBitmap bitmap, SKEncodedOrigin origin)
    {
        var needsFlip = origin is SKEncodedOrigin.LeftBottom or SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.RightTop;
        SKBitmap? rotated = null;
        try
        {
            rotated = needsFlip
                ? new SKBitmap(bitmap.Height, bitmap.Width)
                : new SKBitmap(bitmap.Width, bitmap.Height);
            using var surface = new SKCanvas(rotated);
            var midX = (float)rotated.Width / 2;
            var midY = (float)rotated.Height / 2;

            switch (origin)
            {
                case SKEncodedOrigin.TopRight:
                    surface.Scale(-1, 1, midX, midY);
                    break;
                case SKEncodedOrigin.BottomRight:
                    surface.RotateDegrees(180, midX, midY);
                    break;
                case SKEncodedOrigin.BottomLeft:
                    surface.Scale(1, -1, midX, midY);
                    break;
                case SKEncodedOrigin.LeftTop:
                    surface.Translate(0, -rotated.Height);
                    surface.Scale(1, -1, midX, midY);
                    surface.RotateDegrees(-90);
                    break;
                case SKEncodedOrigin.RightTop:
                    surface.Translate(rotated.Width, 0);
                    surface.RotateDegrees(90);
                    break;
                case SKEncodedOrigin.RightBottom:
                    surface.Translate(rotated.Width, 0);
                    surface.Scale(1, -1, midX, midY);
                    surface.RotateDegrees(90);
                    break;
                case SKEncodedOrigin.LeftBottom:
                    surface.Translate(0, rotated.Height);
                    surface.RotateDegrees(-90);
                    break;
            }

            surface.DrawBitmap(bitmap, 0, 0);
            return rotated;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Detected intermediary error rotating image");
            rotated?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Resizes an image on the CPU, by utilizing a surface and canvas.
    ///
    /// The convolutional matrix kernel used in this resize function gives a (light) sharpening effect.
    /// This technique is similar to effect that can be created using for example the [Convolution matrix filter in GIMP](https://docs.gimp.org/2.10/en/gimp-filter-convolution-matrix.html).
    /// </summary>
    /// <param name="source">The source bitmap.</param>
    /// <param name="targetInfo">This specifies the target size and other information required to create the surface.</param>
    /// <param name="isAntialias">This enables anti-aliasing on the SKPaint instance.</param>
    /// <param name="isDither">This enables dithering on the SKPaint instance.</param>
    /// <returns>The resized image.</returns>
    internal static SKImage ResizeImage(SKBitmap source, SKImageInfo targetInfo, bool isAntialias = false, bool isDither = false)
    {
        using var surface = SKSurface.Create(targetInfo);
        using var canvas = surface.Canvas;
        using var paint = new SKPaint
        {
            FilterQuality = SKFilterQuality.High,
            IsAntialias = isAntialias,
            IsDither = isDither
        };

        paint.ImageFilter = _imageFilter;
        canvas.DrawBitmap(
            source,
            SKRect.Create(0, 0, source.Width, source.Height),
            SKRect.Create(0, 0, targetInfo.Width, targetInfo.Height),
            paint);

        return surface.Snapshot();
    }

    /// <inheritdoc/>
    public string EncodeImage(string inputPath, DateTime dateModified, string outputPath, bool autoOrient, ImageOrientation? orientation, int quality, ImageProcessingOptions options, ImageFormat outputFormat)
    {
        ArgumentException.ThrowIfNullOrEmpty(inputPath);
        ArgumentException.ThrowIfNullOrEmpty(outputPath);

        var inputFormat = Path.GetExtension(inputPath.AsSpan()).TrimStart('.');
        if (!SupportedInputFormats.Contains(inputFormat, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Unable to encode image due to unsupported format: {ImagePath}", inputPath);
            return inputPath;
        }

        if (outputFormat == ImageFormat.Svg
            && !inputFormat.Equals(SvgFormat, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Requested svg output from {inputFormat} input");
        }

        var skiaOutputFormat = GetImageFormat(outputFormat);

        var hasBackgroundColor = !string.IsNullOrWhiteSpace(options.BackgroundColor);
        var hasForegroundColor = !string.IsNullOrWhiteSpace(options.ForegroundLayer);
        var blur = options.Blur ?? 0;
        var hasIndicator = options.UnplayedCount.HasValue || !options.PercentPlayed.Equals(0);

        using var bitmap = inputFormat.Equals(SvgFormat, StringComparison.OrdinalIgnoreCase)
            ? GetBitmapFromSvg(inputPath)
            : GetBitmap(inputPath, autoOrient, orientation);

        if (bitmap is null)
        {
            throw new InvalidDataException($"Skia unable to read image {inputPath}");
        }

        var originalImageSize = new ImageDimensions(bitmap.Width, bitmap.Height);

        if (options.HasDefaultOptions(inputPath, originalImageSize) && !autoOrient)
        {
            // Just spit out the original file if all the options are default
            return inputPath;
        }

        var newImageSize = ImageHelper.GetNewImageSize(options, originalImageSize);

        var width = newImageSize.Width;
        var height = newImageSize.Height;

        // scale image (the FromImage creates a copy)
        var imageInfo = new SKImageInfo(width, height, bitmap.ColorType, bitmap.AlphaType, bitmap.ColorSpace);
        using var resizedImage = ResizeImage(bitmap, imageInfo);
        using var resizedBitmap = SKBitmap.FromImage(resizedImage);

        // If all we're doing is resizing then we can stop now
        if (!hasBackgroundColor && !hasForegroundColor && blur == 0 && !hasIndicator)
        {
            var outputDirectory = Path.GetDirectoryName(outputPath) ?? throw new ArgumentException($"Provided path ({outputPath}) is not valid.", nameof(outputPath));
            Directory.CreateDirectory(outputDirectory);
            using var outputStream = new SKFileWStream(outputPath);
            using var pixmap = new SKPixmap(new SKImageInfo(width, height), resizedBitmap.GetPixels());
            resizedBitmap.Encode(outputStream, skiaOutputFormat, quality);
            return outputPath;
        }

        // create bitmap to use for canvas drawing used to draw into bitmap
        using var saveBitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(saveBitmap);
        // set background color if present
        if (hasBackgroundColor)
        {
            canvas.Clear(SKColor.Parse(options.BackgroundColor));
        }

        // Add blur if option is present
        if (blur > 0)
        {
            // create image from resized bitmap to apply blur
            using var paint = new SKPaint();
            using var filter = SKImageFilter.CreateBlur(blur, blur);
            paint.ImageFilter = filter;
            canvas.DrawBitmap(resizedBitmap, SKRect.Create(width, height), paint);
        }
        else
        {
            // draw resized bitmap onto canvas
            canvas.DrawBitmap(resizedBitmap, SKRect.Create(width, height));
        }

        // If foreground layer present then draw
        if (hasForegroundColor)
        {
            if (!double.TryParse(options.ForegroundLayer, out double opacity))
            {
                opacity = .4;
            }

            canvas.DrawColor(new SKColor(0, 0, 0, (byte)((1 - opacity) * 0xFF)), SKBlendMode.SrcOver);
        }

        if (hasIndicator)
        {
            DrawIndicator(canvas, width, height, options);
        }

        var directory = Path.GetDirectoryName(outputPath) ?? throw new ArgumentException($"Provided path ({outputPath}) is not valid.", nameof(outputPath));
        Directory.CreateDirectory(directory);
        using (var outputStream = new SKFileWStream(outputPath))
        {
            using var pixmap = new SKPixmap(new SKImageInfo(width, height), saveBitmap.GetPixels());
            pixmap.Encode(outputStream, skiaOutputFormat, quality);
        }

        return outputPath;
    }

    /// <inheritdoc/>
    public void CreateImageCollage(ImageCollageOptions options, string? libraryName)
    {
        double ratio = (double)options.Width / options.Height;

        if (ratio >= 1.4)
        {
            new StripCollageBuilder(this).BuildThumbCollage(options.InputPaths, options.OutputPath, options.Width, options.Height, libraryName);
        }
        else if (ratio >= .9)
        {
            new StripCollageBuilder(this).BuildSquareCollage(options.InputPaths, options.OutputPath, options.Width, options.Height);
        }
        else
        {
            // TODO: Create Poster collage capability
            new StripCollageBuilder(this).BuildSquareCollage(options.InputPaths, options.OutputPath, options.Width, options.Height);
        }
    }

    /// <inheritdoc />
    public void CreateSplashscreen(IReadOnlyList<string> posters, IReadOnlyList<string> backdrops)
    {
        // Only generate the splash screen if we have at least one poster and at least one backdrop/thumbnail.
        if (posters.Count > 0 && backdrops.Count > 0)
        {
            var splashBuilder = new SplashscreenBuilder(this, _logger);
            var outputPath = Path.Combine(_appPaths.DataPath, "splashscreen.png");
            splashBuilder.GenerateSplash(posters, backdrops, outputPath);
        }
    }

    /// <inheritdoc />
    public int CreateTrickplayTile(ImageCollageOptions options, int quality, int imgWidth, int? imgHeight)
    {
        var paths = options.InputPaths;
        var tileWidth = options.Width;
        var tileHeight = options.Height;

        if (paths.Count < 1)
        {
            throw new ArgumentException("InputPaths cannot be empty.");
        }
        else if (paths.Count > tileWidth * tileHeight)
        {
            throw new ArgumentException($"InputPaths contains more images than would fit on {tileWidth}x{tileHeight} grid.");
        }

        // If no height provided, use height of first image.
        if (!imgHeight.HasValue)
        {
            using var firstImg = Decode(paths[0], false, null, out _);

            if (firstImg is null)
            {
                throw new InvalidDataException("Could not decode image data.");
            }

            if (firstImg.Width != imgWidth)
            {
                throw new InvalidOperationException("Image width does not match provided width.");
            }

            imgHeight = firstImg.Height;
        }

        // Make horizontal strips using every provided image.
        using var tileGrid = new SKBitmap(imgWidth * tileWidth, imgHeight.Value * tileHeight);
        using var canvas = new SKCanvas(tileGrid);

        var imgIndex = 0;
        for (var y = 0; y < tileHeight; y++)
        {
            for (var x = 0; x < tileWidth; x++)
            {
                if (imgIndex >= paths.Count)
                {
                    break;
                }

                using var img = Decode(paths[imgIndex++], false, null, out _);

                if (img is null)
                {
                    throw new InvalidDataException("Could not decode image data.");
                }

                if (img.Width != imgWidth)
                {
                    throw new InvalidOperationException("Image width does not match provided width.");
                }

                if (img.Height != imgHeight)
                {
                    throw new InvalidOperationException("Image height does not match first image height.");
                }

                canvas.DrawBitmap(img, x * imgWidth, y * imgHeight.Value);
            }
        }

        using var outputStream = new SKFileWStream(options.OutputPath);
        tileGrid.Encode(outputStream, SKEncodedImageFormat.Jpeg, quality);

        return imgHeight.Value;
    }

    private void DrawIndicator(SKCanvas canvas, int imageWidth, int imageHeight, ImageProcessingOptions options)
    {
        try
        {
            var currentImageSize = new ImageDimensions(imageWidth, imageHeight);

            if (options.UnplayedCount.HasValue)
            {
                UnplayedCountIndicator.DrawUnplayedCountIndicator(canvas, currentImageSize, options.UnplayedCount.Value);
            }

            if (options.PercentPlayed > 0)
            {
                PercentPlayedDrawer.Process(canvas, currentImageSize, options.PercentPlayed);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error drawing indicator overlay");
        }
    }
}
