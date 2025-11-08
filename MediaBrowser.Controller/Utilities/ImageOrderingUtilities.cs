using System;

namespace MediaBrowser.Controller.Utilities;

/// <summary>
/// Utility methods for natural image ordering helpers (numeric suffix parsing, etc.).
/// </summary>
public static class ImageOrderingUtilities
{
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
