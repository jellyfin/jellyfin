using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MediaBrowser.Providers.Audiobooks;

/// <summary>
/// Audiobook utilities for audiobook processing.
/// </summary>
public static partial class AudiobookUtils
{
    /// <summary>
    /// List of supported audiobook file extensions.
    /// </summary>
    public static readonly string[] SupportedExtensions = [".m4b", ".mp3", ".m4a", ".aac", ".ogg", ".flac", ".wma"];

    private static readonly Regex _audiobookPatternRegex = AudiobookPatternRegex();
    private static readonly Regex _separatorRegex = SeparatorRegex();
    private static readonly Regex _extraSpacesRegex = ExtraSpacesRegex();
    private static readonly Regex _isbnRegex = IsbnRegex();
    private static readonly Regex[] _seriesPatternRegexes =
    [
        SeriesPatternWithKeyword1Regex(),   // "Series Name, Book 1"
        SeriesPatternWithKeyword2Regex(),   // "Series Name - Book #1"
        SeriesPatternParenthesesRegex(),    // "Series Name (Book 1)" or "Series Name (1)"
        SeriesPatternBracketsRegex(),       // "Series Name [Book 1]" or "Series Name [1]"
        SeriesPatternHashRegex(),           // "Series Name #1"
        SeriesPatternNumberSuffixRegex(),   // "Series Name 1" (last, as it's most general)
    ];

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
        title = _audiobookPatternRegex.Replace(title, string.Empty);

        // Remove common separators and extra spaces
        title = _separatorRegex.Replace(title, " ");
        title = _extraSpacesRegex.Replace(title, " ");

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

        foreach (var regex in _seriesPatternRegexes)
        {
            var match = regex.Match(cleanTitle);

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

        var isbnMatch = _isbnRegex.Match(text);

        if (isbnMatch.Success)
        {
            return isbnMatch.Groups[1].Value
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Replace(" ", string.Empty, StringComparison.Ordinal);
        }

        return null;
    }

    [GeneratedRegex(@"\s*[\[\(]\s*(unabridged|abridged|audiobook|mp3|m4b)\s*[\]\)]\s*", RegexOptions.IgnoreCase)]
    private static partial Regex AudiobookPatternRegex();

    [GeneratedRegex(@"\s*[-_]\s*")]
    private static partial Regex SeparatorRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex ExtraSpacesRegex();

    [GeneratedRegex(@"ISBN[:\-\s]*(\d{10}|\d{13}|\d{1,5}[\-\s]\d{1,7}[\-\s]\d{1,7}[\-\s][\dX])", RegexOptions.IgnoreCase)]
    private static partial Regex IsbnRegex();

    [GeneratedRegex(@"^(.+?)\s*[,\-:]\s*(?:book|vol|volume)\s*(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex SeriesPatternWithKeyword1Regex();

    [GeneratedRegex(@"^(.+?)\s*[,\-:]\s*(?:book|vol|volume)\s*#?(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex SeriesPatternWithKeyword2Regex();

    [GeneratedRegex(@"^(.+?)\s*\((?:book|vol|volume)?\s*#?(\d+)\)", RegexOptions.IgnoreCase)]
    private static partial Regex SeriesPatternParenthesesRegex();

    [GeneratedRegex(@"^(.+?)\s*\[(?:book|vol|volume)?\s*#?(\d+)\]", RegexOptions.IgnoreCase)]
    private static partial Regex SeriesPatternBracketsRegex();

    [GeneratedRegex(@"^(.+?)\s*#(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex SeriesPatternHashRegex();

    [GeneratedRegex(@"^(.+?)\s*(\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex SeriesPatternNumberSuffixRegex();
}
