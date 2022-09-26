using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Resolvers;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// TXT Lyric Provider.
/// </summary>
public class TxtLyricProvider : ILyricProvider
{
    /// <inheritdoc />
    public string Name => "TxtLyricProvider";

    /// <summary>
    /// Gets the priority.
    /// </summary>
    /// <value>The priority.</value>
    public ResolverPriority Priority => ResolverPriority.Second;

    /// <inheritdoc />
    public IReadOnlyCollection<string> SupportedMediaTypes { get; } = new[] { "lrc", "elrc", "txt" };

    /// <summary>
    /// Opens lyric file for the requested item, and processes it for API return.
    /// </summary>
    /// <param name="item">The item to to process.</param>
    /// <returns>If provider can determine lyrics, returns a <see cref="LyricResponse"/>; otherwise, null.</returns>
    public async Task<LyricResponse?> GetLyrics(BaseItem item)
    {
        string? lyricFilePath = this.GetLyricFilePath(item.Path);

        if (string.IsNullOrEmpty(lyricFilePath))
        {
            return null;
        }

        string[] lyricTextLines = await File.ReadAllLinesAsync(lyricFilePath).ConfigureAwait(false);

        if (lyricTextLines.Length == 0)
        {
            return null;
        }

        LyricLine[] lyricList = new LyricLine[lyricTextLines.Length];

        for (int lyricLineIndex = 0; lyricLineIndex < lyricTextLines.Length; lyricLineIndex++)
        {
            lyricList[lyricLineIndex] = new LyricLine(lyricTextLines[lyricLineIndex]);
        }

        return new LyricResponse
        {
            Lyrics = lyricList
        };
    }
}
