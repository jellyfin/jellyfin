using System;

namespace MediaBrowser.Controller.Utilities;

/// <summary>
/// Utility methods for natural image ordering helpers (numeric suffix parsing, etc.).
/// </summary>
public static class ImageOrderingUtilities
{
    /// <summary>
    /// Priority value assigned to images that don't match any known naming pattern.
    /// </summary>
    public const int UnknownImagePriority = 999;

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

    /// <summary>
    /// Determines the ordering priority for an image based on its filename and media context.
    /// Lower values indicate higher priority (e.g., media-specific fanart = 0, backdrop = 6).
    /// </summary>
    /// <param name="path">The full path to the image file.</param>
    /// <param name="mediaFileName">The media filename prefix (without extension) for media-specific matching.</param>
    /// <returns>Priority value from 0 (highest) to <see cref="UnknownImagePriority"/> (lowest).</returns>
    public static int GetImageOrderPriority(string? path, string? mediaFileName)
    {
        if (string.IsNullOrEmpty(path))
        {
            return UnknownImagePriority;
        }

        var normalizedPath = path.Replace('\\', '/');
        var fileName = System.IO.Path.GetFileNameWithoutExtension(normalizedPath);

        if (IsMediaSpecificFanart(fileName, mediaFileName))
        {
            return 0;
        }

        if (fileName.Equals("fanart", StringComparison.OrdinalIgnoreCase) &&
            !normalizedPath.Contains("/extrafanart/", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (fileName.StartsWith("fanart-", StringComparison.OrdinalIgnoreCase) &&
            !normalizedPath.Contains("/extrafanart/", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (fileName.Equals("background", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("background-", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (fileName.Equals("art", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("art-", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (normalizedPath.Contains("/extrafanart/", StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith("extrafanart/", StringComparison.OrdinalIgnoreCase))
        {
            return 5;
        }

        if (fileName.Equals("backdrop", StringComparison.OrdinalIgnoreCase) ||
            fileName.StartsWith("backdrop", StringComparison.OrdinalIgnoreCase))
        {
            return 6;
        }

        return UnknownImagePriority;
    }

    private static bool IsMediaSpecificFanart(string fileName, string? mediaFileName)
    {
        if (string.IsNullOrEmpty(mediaFileName))
        {
            return false;
        }

        var expectedName = $"{mediaFileName}-fanart";
        return fileName.Equals(expectedName, StringComparison.OrdinalIgnoreCase);
    }
}
