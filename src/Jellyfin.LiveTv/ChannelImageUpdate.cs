namespace Jellyfin.LiveTv;

/// <summary>
/// Outcome of a Live TV channel icon check.
/// </summary>
internal enum ChannelImageUpdate
{
    /// <summary>
    /// Nothing changed, the item does not need to be saved.
    /// </summary>
    None,

    /// <summary>
    /// Only the HTTP cache validators of the current image changed, so the item needs a plain save.
    /// The image itself is still current and must not be re-downloaded.
    /// </summary>
    ValidatorsOnly,

    /// <summary>
    /// The icon source changed, so the item needs to be saved and the image re-downloaded.
    /// </summary>
    ImageChanged
}
