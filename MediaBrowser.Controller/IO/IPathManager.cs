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

    /// <summary>
    /// Gets the path to the subtitle file.
    /// </summary>
    /// <param name="mediaSourceId">The media source id.</param>
    /// <param name="streamIndex">The stream index.</param>
    /// <param name="extension">The subtitle file extension.</param>
    /// <returns>The absolute path.</returns>
    public string GetSubtitlePath(string mediaSourceId, int streamIndex, string extension);

    /// <summary>
    /// Gets the path to the attachment file.
    /// </summary>
    /// <param name="mediaSourceId">The media source id.</param>
    /// <param name="attachmentIndex">The stream index.</param>
    /// <returns>The absolute path.</returns>
    public string GetAttachmentPath(string mediaSourceId, int attachmentIndex);
}
