using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        if (lyricProvider is null)
        {
            return null;
        }

        if (!Directory.Exists(Path.GetDirectoryName(itemPath)))
        {
            return null;
        }

        foreach (var lyricFilePath in Directory.GetFiles(Path.GetDirectoryName(itemPath), $"{Path.GetFileNameWithoutExtension(itemPath)}.*"))
        {
            if (lyricProvider.SupportedMediaTypes.Contains(Path.GetExtension(lyricFilePath)[1..]))
            {
                return lyricFilePath;
            }
        }

        return null;
    }
}
