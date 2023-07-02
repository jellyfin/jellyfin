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
    /// Checks if an item has lyrics available.
    /// </summary>
    /// <param name="item">The media item.</param>
    /// <returns>Whether lyrics were found.</returns>
    Task<bool> HasLyricsAsync(BaseItem item);

    /// <summary>
    /// Gets the lyrics.
    /// </summary>
    /// <param name="item">The media item.</param>
    /// <returns>A task representing found lyrics.</returns>
    Task<LyricFile?> GetLyricsAsync(BaseItem item);
}
