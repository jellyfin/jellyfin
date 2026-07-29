using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace Jellyfin.LiveTv;

/// <summary>
/// Helpers for keeping Live TV channel icons in sync with guide data.
/// </summary>
internal static class LiveTvChannelImageHelper
{
    /// <summary>
    /// Provider id used to remember which source (URL or path) the current channel icon came from,
    /// so an unchanged icon is not needlessly re-applied (and thus re-downloaded) on every refresh.
    /// </summary>
    internal const string ImageSourceKey = "GuideImageSource";

    /// <summary>
    /// Applies the channel icon from guide or tuner metadata.
    /// The icon is only (re)applied when the source changed or the channel has no icon yet; an
    /// unchanged icon is left in place so it is not re-downloaded on every guide refresh. This
    /// matters a lot for large IPTV playlists where re-fetching tens of thousands of logos each
    /// cycle dominates the refresh time.
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

        // Skip when the same source is already applied and an icon is present. The stored source is
        // tracked separately from the image path because the path may be localized after download.
        if (item.HasImage(ImageType.Primary)
            && string.Equals(item.GetProviderId(ImageSourceKey), newImageSource, StringComparison.Ordinal))
        {
            return false;
        }

        item.SetImagePath(ImageType.Primary, newImageSource);
        item.SetProviderId(ImageSourceKey, newImageSource);
        return true;
    }
}
