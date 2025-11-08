using System;

namespace MediaBrowser.Controller.Utilities;

/// <summary>
/// Utility methods for determining image ordering priority based on filename conventions.
/// Used by both runtime image loading and database migrations to ensure consistent ordering.
/// </summary>
public static class ImageOrderingUtilities
{
    private const int UnknownImagePriority = 999;

    /// <summary>
    /// Gets the priority order for an image based on its path and naming convention.
    /// Lower numbers indicate higher priority.
    /// </summary>
    /// <remarks>
    /// Priority levels:
    /// <list type="bullet">
    /// <item><description>0: {mediaFileName}-fanart (e.g., "MOVIE-fanart.jpg")</description></item>
    /// <item><description>1: fanart (not in extrafanart folder)</description></item>
    /// <item><description>2: fanart-N (numbered, not in extrafanart)</description></item>
    /// <item><description>3: background or background-N</description></item>
    /// <item><description>4: art or art-N</description></item>
    /// <item><description>5: extrafanart folder images</description></item>
    /// <item><description>6: backdrop or backdropN</description></item>
    /// <item><description>999: unknown/default</description></item>
    /// </list>
    /// </remarks>
    /// <param name="path">The full path to the image file.</param>
    /// <param name="mediaFileName">The media file name without extension (e.g., "MOVIE").</param>
    /// <returns>Priority value where 0 is highest priority.</returns>
    public static int GetImageOrderPriority(string? path, string? mediaFileName)
    {
        if (string.IsNullOrEmpty(path))
        {
            return UnknownImagePriority;
        }

        var normalizedPath = path.Replace('\\', '/');
        var fileName = System.IO.Path.GetFileNameWithoutExtension(normalizedPath);

        // Priority 0: {mediaFileName}-fanart (any extension)
        if (!string.IsNullOrEmpty(mediaFileName))
        {
            var expectedName = $"{mediaFileName}-fanart";
            if (fileName.Equals(expectedName, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }
        }

        // Priority 1: fanart (not in extrafanart folder)
        if (fileName.Equals("fanart", StringComparison.OrdinalIgnoreCase) &&
            !normalizedPath.Contains("/extrafanart/", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        // Priority 2: fanart-N (numbered, not in extrafanart)
        if (fileName.StartsWith("fanart-", StringComparison.OrdinalIgnoreCase) &&
            !normalizedPath.Contains("/extrafanart/", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        // Priority 3: background or background-N
        if (fileName.Equals("background", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("background-", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        // Priority 4: art or art-N
        if (fileName.Equals("art", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("art-", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        // Priority 5: extrafanart folder
        if (normalizedPath.Contains("/extrafanart/", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith("extrafanart/", StringComparison.OrdinalIgnoreCase))
        {
            return 5;
        }

        // Priority 6: backdrop or backdropN
        if (fileName.Equals("backdrop", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("backdrop", StringComparison.OrdinalIgnoreCase))
        {
            return 6;
        }

        // Default: lowest priority
        return UnknownImagePriority;
    }

    /// <summary>
    /// Extracts numeric index from image filename for proper sorting within priority groups.
    /// Ensures natural number ordering (e.g., fanart1, fanart2, ..., fanart10 instead of fanart1, fanart10, fanart2).
    /// </summary>
    /// <param name="path">The full path to the image file.</param>
    /// <returns>Numeric index if found, otherwise int.MaxValue for non-numeric filenames.</returns>
    public static int GetNumericImageIndex(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return int.MaxValue;
        }

        var normalizedPath = path.Replace('\\', '/');
        var fileName = System.IO.Path.GetFileNameWithoutExtension(normalizedPath);

        if (fileName.Length > 0)
        {
            int digitStartIndex = -1;
            for (int i = fileName.Length - 1; i >= 0; i--)
            {
                if (char.IsDigit(fileName[i]))
                {
                    digitStartIndex = i;
                }
                else if (digitStartIndex >= 0)
                {
                    break;
                }
            }

            if (digitStartIndex >= 0)
            {
                var numericPart = fileName.Substring(digitStartIndex);
                if (int.TryParse(numericPart, out var index))
                {
                    return index;
                }
            }
        }

        return int.MaxValue;
    }
}
