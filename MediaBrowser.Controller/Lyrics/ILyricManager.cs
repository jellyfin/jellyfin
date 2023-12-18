using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Lyrics;

/// <summary>
/// Interface ILyricManager.
/// </summary>
public interface ILyricManager
{
    /// <summary>
    /// Gets the lyrics.
    /// </summary>
    /// <param name="item">The media item.</param>
    /// <returns>A task representing found lyrics the passed item.</returns>
    Task<LyricResponse?> GetLyrics(BaseItem item);

    /// <summary>
    /// Checks if requested item has a matching local lyric file.
    /// </summary>
    /// <param name="item">The media item.</param>
    /// <returns>True if item has a matching lyric file; otherwise false.</returns>
    bool HasLyricFile(BaseItem item);
}
