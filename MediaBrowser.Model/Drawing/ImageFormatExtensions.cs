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
            ImageFormat.Bmp => "image/bmp",
            ImageFormat.Gif => MediaTypeNames.Image.Gif,
            ImageFormat.Jpg => MediaTypeNames.Image.Jpeg,
            ImageFormat.Png => "image/png",
            ImageFormat.Webp => "image/webp",
            _ => throw new InvalidEnumArgumentException(nameof(format), (int)format, typeof(ImageFormat))
        };
}
