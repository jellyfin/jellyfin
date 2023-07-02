using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Resolvers;

namespace MediaBrowser.Providers.Lyric;

/// <summary>
/// Interface ILyricsProvider.
/// </summary>
public interface ILyricProvider
{
    /// <summary>
    /// Gets a value indicating the provider name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the priority.
    /// </summary>
    /// <value>The priority.</value>
    ResolverPriority Priority { get; }

    /// <summary>
    /// Gets the lyrics.
    /// </summary>
    /// <param name="path">The lyric path.</param>
    /// <returns>A task representing found lyrics.</returns>
    Task<LyricFile?> GetLyrics(string path);
}
