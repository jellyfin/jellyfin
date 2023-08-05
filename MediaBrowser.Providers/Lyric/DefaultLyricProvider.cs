using System;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Resolvers;

namespace MediaBrowser.Providers.Lyric;

/// <inheritdoc />
public class DefaultLyricProvider : ILyricProvider
{
    private static readonly string[] _lyricExtensions = { ".lrc", ".elrc", ".txt" };

    /// <inheritdoc />
    public string Name => "DefaultLyricProvider";

    /// <inheritdoc />
    public ResolverPriority Priority => ResolverPriority.First;

    /// <inheritdoc />
    public bool HasLyrics(BaseItem item)
    {
        var path = GetLyricsPath(item);
        return path is not null;
    }

    /// <inheritdoc />
    public async Task<LyricFile?> GetLyrics(BaseItem item)
    {
        var path = GetLyricsPath(item);
        if (path is not null)
        {
            var content = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(content))
            {
                return new LyricFile(path, content);
            }
        }

        return null;
    }

    private string? GetLyricsPath(BaseItem item)
    {
        // Ensure the path to the item is not null
        string? itemDirectoryPath = Path.GetDirectoryName(item.Path);
        if (itemDirectoryPath is null)
        {
            return null;
        }

        // Ensure the directory path exists
        if (!Directory.Exists(itemDirectoryPath))
        {
            return null;
        }

        foreach (var lyricFilePath in Directory.GetFiles(itemDirectoryPath, $"{Path.GetFileNameWithoutExtension(item.Path)}.*"))
        {
            if (_lyricExtensions.Contains(Path.GetExtension(lyricFilePath.AsSpan()), StringComparison.OrdinalIgnoreCase))
            {
                return lyricFilePath;
            }
        }

        return null;
    }
}
