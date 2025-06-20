using System.Collections.Generic;
using System.IO;
using SkiaSharp;

namespace Jellyfin.Drawing.Skia;

/// <summary>
/// Class containing helper methods for working with SkiaSharp.
/// </summary>
public static class SkiaHelper
{
    /// <summary>
    /// Gets the next valid image as a bitmap.
    /// </summary>
    /// <param name="skiaEncoder">The current skia encoder.</param>
    /// <param name="paths">The list of image paths.</param>
    /// <param name="currentIndex">The current checked index.</param>
    /// <param name="newIndex">The new index.</param>
    /// <returns>A valid bitmap, or null if no bitmap exists after <c>currentIndex</c>.</returns>
    public static SKBitmap? GetNextValidImage(SkiaEncoder skiaEncoder, IReadOnlyList<string> paths, int currentIndex, out int newIndex)
    {
        var imagesTested = new Dictionary<int, int>();

        while (imagesTested.Count < paths.Count)
        {
            if (currentIndex >= paths.Count)
            {
                currentIndex = 0;
            }

            var imagePath = paths[currentIndex];
            imagesTested[currentIndex] = 0;
            currentIndex++;

            if (!Path.Exists(imagePath))
            {
                continue;
            }

            SKBitmap? bitmap = skiaEncoder.Decode(imagePath, false, null, out _);

            if (bitmap is not null)
            {
                newIndex = currentIndex;
                return bitmap;
            }
        }

        newIndex = currentIndex;
        return null;
    }
}
