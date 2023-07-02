using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Lyrics;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// Lyric Manager.
/// </summary>
public class LyricManager : ILyricManager
{
    private readonly ILyricProvider[] _lyricProviders;
    private readonly ILyricParser[] _lyricParsers;

    /// <summary>
    /// Initializes a new instance of the <see cref="LyricManager"/> class.
    /// </summary>
    /// <param name="lyricProviders">All found lyricProviders.</param>
    /// <param name="lyricParsers">All found lyricParsers.</param>
    public LyricManager(IEnumerable<ILyricProvider> lyricProviders, IEnumerable<ILyricParser> lyricParsers)
    {
        _lyricProviders = lyricProviders.OrderBy(i => i.Priority).ToArray();
        _lyricParsers = lyricParsers.OrderBy(i => i.Priority).ToArray();
    }

    /// <inheritdoc />
    public async Task<LyricResponse?> GetLyricsAsync(BaseItem item)
    {
        foreach (ILyricProvider provider in _lyricProviders)
        {
            var lyrics = await provider.GetLyricsAsync(item).ConfigureAwait(false);
            if (lyrics is null)
            {
                continue;
            }

            foreach (ILyricParser parser in _lyricParsers)
            {
                var result = parser.ParseLyrics(lyrics);
                if (result is not null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> HasLyricsAsync(BaseItem item)
    {
        foreach (var provider in _lyricProviders)
        {
            var hasLyrics = await provider.HasLyricsAsync(item).ConfigureAwait(false);
            if (hasLyrics)
            {
                return true;
            }
        }

        return false;
    }
}
