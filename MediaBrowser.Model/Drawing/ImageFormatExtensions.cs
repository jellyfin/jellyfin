using System.ComponentModel;
using System.Net.Mime;

namespace MediaBrowser.Model.Drawing;

/// <summary>
/// Extension class for the <see cref="ImageFormat" /> enum.
/// </summary>
public static class ImageFormatExtensions
{
    /// <summary>
    /// Returns the correct mime type for this <see cref="ImageFormat" />.
    /// </summary>
    /// <param name="format">This <see cref="ImageFormat" />.</param>
    /// <exception cref="InvalidEnumArgumentException">The <paramref name="format"/> is an invalid enumeration value.</exception>
    /// <returns>The correct mime type for this <see cref="ImageFormat" />.</returns>
    public static string GetMimeType(this ImageFormat format)
        => format switch
        {
            ImageFormat.Bmp => MediaTypeNames.Image.Bmp,
            ImageFormat.Gif => MediaTypeNames.Image.Gif,
            ImageFormat.Jpg => MediaTypeNames.Image.Jpeg,
            ImageFormat.Png => MediaTypeNames.Image.Png,
            ImageFormat.Webp => MediaTypeNames.Image.Webp,
            ImageFormat.Svg => MediaTypeNames.Image.Svg,
            _ => throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(ImageFormat))
        };

    /// <summary>
    /// Returns the correct extension for this <see cref="ImageFormat" />.
    /// </summary>
    /// <param name="format">This <see cref="ImageFormat" />.</param>
    /// <exception cref="InvalidEnumArgumentException">The <paramref name="format"/> is an invalid enumeration value.</exception>
    /// <returns>The correct extension for this <see cref="ImageFormat" />.</returns>
    public static string GetExtension(this ImageFormat format)
        => format switch
        {
            ImageFormat.Bmp => ".bmp",
            ImageFormat.Gif => ".gif",
            ImageFormat.Jpg => ".jpg",
            ImageFormat.Png => ".png",
            ImageFormat.Webp => ".webp",
            ImageFormat.Svg => ".svg",
            _ => throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(ImageFormat))
        };
}
