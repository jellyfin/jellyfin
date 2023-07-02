using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Lyric;

/// <inheritdoc />
public class DefaultLyricProvider : ILyricProvider
{
    /// <inheritdoc />
    public string Name => "DefaultLyricProvider";

    /// <inheritdoc />
    public ResolverPriority Priority => ResolverPriority.First;

    /// <inheritdoc />
    public Task<bool> HasLyricsAsync(BaseItem item)
    {
        return Task.FromResult(item.GetMediaStreams()
            .Any(s => s.Type == MediaStreamType.Lyric));
    }

    /// <inheritdoc />
    public async Task<LyricFile?> GetLyricsAsync(BaseItem item)
    {
        var lyricPaths = item.GetMediaStreams()
            .Where(s => s.Type == MediaStreamType.Lyric)
            .Select(s => s.Path);

        foreach (var path in lyricPaths)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                var content = await File.ReadAllTextAsync(path).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(content))
                {
                    return new LyricFile(path, content);
                }
            }
        }

        return null;
    }
}
