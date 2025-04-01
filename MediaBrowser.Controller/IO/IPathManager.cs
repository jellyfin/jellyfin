using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.IO;

/// <summary>
/// Interface ITrickplayManager.
/// </summary>
public interface IPathManager
{
    /// <summary>
    /// Gets the path to the trickplay image base folder.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="saveWithMedia">Whether or not the tile should be saved next to the media file.</param>
    /// <returns>The absolute path.</returns>
    public string GetTrickplayDirectory(BaseItem item, bool saveWithMedia = false);
}
