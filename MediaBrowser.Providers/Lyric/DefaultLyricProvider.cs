using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Resolvers;

namespace MediaBrowser.Providers.Lyric;

/// <inheritdoc />
public class DefaultLyricProvider : ILyricProvider
{
    /// <inheritdoc />
    public string Name => "DefaultLyricProvider";

    /// <inheritdoc />
    public ResolverPriority Priority => ResolverPriority.First;

    /// <inheritdoc />
    public async Task<LyricFile?> GetLyrics(string path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            var content = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(content))
            {
                return new LyricFile(path, content);
            }
        }

        return null;
    }
}
