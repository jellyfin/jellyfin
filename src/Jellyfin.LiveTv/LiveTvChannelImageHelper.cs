using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace Jellyfin.LiveTv;

/// <summary>
/// Helpers for keeping Live TV channel icons in sync with guide data.
/// </summary>
internal static class LiveTvChannelImageHelper
{
    /// <summary>
    /// Applies the channel icon from guide or tuner metadata.
    /// Called on each guide refresh so remote icons are re-downloaded even when the URL is unchanged.
    /// </summary>
    /// <param name="item">The channel item.</param>
    /// <param name="imagePath">The local image path from the tuner, if any.</param>
    /// <param name="imageUrl">The remote image URL from the guide provider, if any.</param>
    /// <returns><c>true</c> when the item image metadata was updated.</returns>
    internal static bool UpdateChannelImageIfNeeded(BaseItem item, string? imagePath, string? imageUrl)
    {
        var newImageSource = !string.IsNullOrWhiteSpace(imagePath)
            ? imagePath
            : imageUrl;

        if (string.IsNullOrWhiteSpace(newImageSource))
        {
            return false;
        }

        item.SetImagePath(ImageType.Primary, newImageSource);
        return true;
    }
}
