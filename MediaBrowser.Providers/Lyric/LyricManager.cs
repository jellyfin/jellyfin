using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Model.Entities;

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
    public async Task<LyricResponse?> GetLyrics(BaseItem item)
    {
        var lyricPaths = item.GetMediaStreams().Where(s => s.Type == MediaStreamType.Lyric);
        foreach (var lyricPath in lyricPaths)
        {
            foreach (ILyricProvider provider in _lyricProviders)
            {
                var lyrics = await provider.GetLyrics(lyricPath.Path).ConfigureAwait(false);
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
        }

        return null;
    }
}
