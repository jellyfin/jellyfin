using System;
using System.IO;
using System.Linq;

namespace MediaBrowser.Providers.Audiobooks;

/// <summary>
/// Audiobook utilities for audiobook processing.
/// </summary>
public static class AudiobookUtils
{
    /// <summary>
    /// List of supported audiobook file extensions.
    /// </summary>
    public static readonly string[] SupportedExtensions = [".m4b", ".mp3", ".m4a", ".aac", ".ogg", ".flac", ".wma"];

    /// <summary>
    /// Check if the file is a valid audiobook file.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if the file is a valid audiobook file, false otherwise.</returns>
    public static bool IsValidAudiobookFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        if (!File.Exists(filePath))
        {
            return false;
        }

        var extension = Path.GetExtension(filePath);
        if (!SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            // Try to create a TagLib file to verify it's a valid audio file
            using var file = TagLib.File.Create(filePath);
            return file.Properties?.Duration > TimeSpan.Zero;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extract a clean book title from a filename.
    /// </summary>
    /// <param name="filename">The filename to clean.</param>
    /// <returns>A cleaned book title.</returns>
    public static string CleanBookTitle(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            return "Unknown Title";
        }

        // Remove file extension
        var title = Path.GetFileNameWithoutExtension(filename);

        // Remove common audiobook patterns
        title = System.Text.RegularExpressions.Regex.Replace(
            title,
            @"\s*[\[\(]\s*(unabridged|abridged|audiobook|mp3|m4b)\s*[\]\)]\s*",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove common separators and extra spaces
        title = System.Text.RegularExpressions.Regex.Replace(title, @"\s*[-_]\s*", " ");
        title = System.Text.RegularExpressions.Regex.Replace(title, @"\s+", " ");

        return title.Trim();
    }

    /// <summary>
    /// Try to parse series information from a title string.
    /// </summary>
    /// <param name="title">The title to parse.</param>
    /// <param name="seriesName">The extracted series name.</param>
    /// <param name="bookNumber">The extracted book number.</param>
    /// <returns>True if series information was found, false otherwise.</returns>
    public static bool TryParseSeriesInfo(string title, out string seriesName, out int? bookNumber)
    {
        seriesName = title;
        bookNumber = null;

        if (string.IsNullOrEmpty(title))
        {
            return false;
        }

        // Clean the title first - remove quotes and extra whitespace
        var cleanTitle = title.Trim('"', '\'', ' ');

        // Try various patterns for series detection
        var patterns = new[]
        {
            @"^(.+?)\s*[,\-:]\s*(?:book|vol|volume)\s*(\d+)",             // "Series Name, Book 1"
            @"^(.+?)\s*[,\-:]\s*(?:book|vol|volume)\s*#?(\d+)",           // "Series Name - Book #1"
            @"^(.+?)\s*\((?:book|vol|volume)?\s*#?(\d+)\)",               // "Series Name (Book 1)" or "Series Name (1)"
            @"^(.+?)\s*\[(?:book|vol|volume)?\s*#?(\d+)\]",               // "Series Name [Book 1]" or "Series Name [1]"
            @"^(.+?)\s*#(\d+)",                                           // "Series Name #1"
            @"^(.+?)\s*(\d+)$",                                           // "Series Name 1" (last, as it's most general)
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                cleanTitle,
                pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (match.Success && match.Groups.Count >= 3)
            {
                seriesName = match.Groups[1].Value.Trim();
                if (int.TryParse(match.Groups[2].Value, out var number))
                {
                    bookNumber = number;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Try to extract book title from file path structure.
    /// </summary>
    /// <param name="filePath">The full file path.</param>
    /// <param name="bookTitle">The extracted book title from folder structure.</param>
    /// <returns>True if book title was found, false otherwise.</returns>
    public static bool TryParseBookTitleFromPath(string filePath, out string bookTitle)
    {
        bookTitle = string.Empty;

        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        try
        {
            // Look at the folder structure: /Author/BookTitle/Book.ext
            var pathParts = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Where(part => !string.IsNullOrEmpty(part))
                .ToArray();

            if (pathParts.Length >= 3)
            {
                // Get the book folder name (second to last folder)
                var bookFolder = pathParts[^2];
                // Clean up the folder name to use as book title
                if (!string.IsNullOrEmpty(bookFolder) &&
                    !bookFolder.Contains("audiobook", StringComparison.OrdinalIgnoreCase) &&
                    !bookFolder.Contains("books", StringComparison.OrdinalIgnoreCase))
                {
                    bookTitle = bookFolder.Replace('+', ' ').Trim();
                    return true;
                }
            }
        }
        catch
        {
            // Ignore path parsing errors
        }

        return false;
    }

    /// <summary>
    /// Extract ISBN from a text string.
    /// </summary>
    /// <param name="text">The text to search for ISBN.</param>
    /// <returns>The extracted ISBN or null if not found.</returns>
    public static string? ExtractIsbn(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var isbnMatch = System.Text.RegularExpressions.Regex.Match(
            text,
            @"ISBN[:\-\s]*(\d{10}|\d{13}|\d{1,5}[\-\s]\d{1,7}[\-\s]\d{1,7}[\-\s][\dX])",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (isbnMatch.Success)
        {
            return isbnMatch.Groups[1].Value
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Replace(" ", string.Empty, StringComparison.Ordinal);
        }

        return null;
    }
}
