using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Controller.Trickplay;

/// <summary>
/// Interface ITrickplayManager.
/// </summary>
public interface ITrickplayManager
{
    /// <summary>
    /// Generates new trickplay images and metadata.
    /// </summary>
    /// <param name="video">The video.</param>
    /// <param name="replace">Whether or not existing data should be replaced.</param>
    /// <param name="cancellationToken">CancellationToken to use for operation.</param>
    /// <returns>Task.</returns>
    Task RefreshTrickplayDataAsync(Video video, bool replace, CancellationToken cancellationToken);

    /// <summary>
    /// Creates trickplay tiles out of individual thumbnails.
    /// </summary>
    /// <param name="images">Ordered file paths of the thumbnails to be used.</param>
    /// <param name="width">The width of a single thumbnail.</param>
    /// <param name="options">The trickplay options.</param>
    /// <param name="outputDir">The output directory.</param>
    /// <returns>The associated trickplay information.</returns>
    /// <remarks>
    /// The output directory will be DELETED and replaced if it already exists.
    /// </remarks>
    TrickplayInfo CreateTiles(List<string> images, int width, TrickplayOptions options, string outputDir);

    /// <summary>
    /// Get available trickplay resolutions and corresponding info.
    /// </summary>
    /// <param name="itemId">The item.</param>
    /// <returns>Map of width resolutions to trickplay tiles info.</returns>
    Task<Dictionary<int, TrickplayInfo>> GetTrickplayResolutions(Guid itemId);

    /// <summary>
    /// Saves trickplay info.
    /// </summary>
    /// <param name="info">The trickplay info.</param>
    /// <returns>Task.</returns>
    Task SaveTrickplayInfo(TrickplayInfo info);

    /// <summary>
    /// Gets all trickplay infos for all media streams of an item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>A map of media source id to a map of tile width to trickplay info.</returns>
    Task<Dictionary<string, Dictionary<int, TrickplayInfo>>> GetTrickplayManifest(BaseItem item);

    /// <summary>
    /// Gets the path to a trickplay tile image.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="width">The width of a single thumbnail.</param>
    /// <param name="index">The tile's index.</param>
    /// <returns>The absolute path.</returns>
    string GetTrickplayTilePath(BaseItem item, int width, int index);

    /// <summary>
    /// Gets the trickplay HLS playlist.
    /// </summary>
    /// <param name="itemId">The item.</param>
    /// <param name="width">The width of a single thumbnail.</param>
    /// <param name="apiKey">Optional api key of the requesting user.</param>
    /// <returns>The text content of the .m3u8 playlist.</returns>
    Task<string?> GetHlsPlaylist(Guid itemId, int width, string? apiKey);
}
