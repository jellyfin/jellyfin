using System.Linq;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Helper methods for playlist share token validation.
/// </summary>
public static class PlaylistShareHelper
{
    /// <summary>
    /// Validates share token access for a media item.
    /// </summary>
    /// <param name="playlistManager">The playlist manager.</param>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="shareToken">The share token.</param>
    /// <param name="itemId">The item ID to validate.</param>
    /// <returns>ActionResult if validation fails, null if validation succeeds.</returns>
    public static ActionResult? ValidateShareTokenAccess(
        IPlaylistManager playlistManager,
        ILibraryManager libraryManager,
        string? shareToken,
        System.Guid itemId)
    {
        if (string.IsNullOrWhiteSpace(shareToken))
        {
            return null;
        }

        var playlist = playlistManager.GetPlaylistByShareToken(shareToken);
        if (playlist is null)
        {
            return new NotFoundObjectResult("Invalid share token");
        }

        var item = libraryManager.GetItemById(itemId);
        if (item is null)
        {
            return new NotFoundObjectResult("Item not found");
        }

        var playlistItems = playlist.GetManageableItems();
        if (!playlistItems.Any(i => i.Item2.Id.Equals(itemId)))
        {
            return new ForbidResult();
        }

        return null;
    }
}
