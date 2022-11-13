using System;
using System.IO;
using Jellyfin.Extensions;

namespace MediaBrowser.Controller.Lyrics;

/// <summary>
/// Lyric helper methods.
/// </summary>
public static class LyricInfo
{
    /// <summary>
    /// Gets matching lyric file for a requested item.
    /// </summary>
    /// <param name="lyricProvider">The lyricProvider interface to use.</param>
    /// <param name="itemPath">Path of requested item.</param>
    /// <returns>Lyric file path if passed lyric provider's supported media type is found; otherwise, null.</returns>
    public static string? GetLyricFilePath(this ILyricProvider lyricProvider, string itemPath)
    {
        // Ensure we have a provider
        if (lyricProvider is null)
        {
            return null;
        }

        // Ensure the path to the item is not null
        string? itemDirectoryPath = Path.GetDirectoryName(itemPath);
        if (itemDirectoryPath is null)
        {
            return null;
        }

        // Ensure the directory path exists
        if (!Directory.Exists(itemDirectoryPath))
        {
            return null;
        }

        foreach (var lyricFilePath in Directory.GetFiles(itemDirectoryPath, $"{Path.GetFileNameWithoutExtension(itemPath)}.*"))
        {
            if (lyricProvider.SupportedMediaTypes.Contains(Path.GetExtension(lyricFilePath.AsSpan())[1..], StringComparison.OrdinalIgnoreCase))
            {
                return lyricFilePath;
            }
        }

        return null;
    }
}
