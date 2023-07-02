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
}
