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
    /// <param name="libraryOptions">The library options.</param>
    /// <param name="cancellationToken">CancellationToken to use for operation.</param>
    /// <returns>Task.</returns>
    Task RefreshTrickplayDataAsync(Video video, bool replace, LibraryOptions? libraryOptions, CancellationToken cancellationToken);

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
    TrickplayInfo CreateTiles(IReadOnlyList<string> images, int width, TrickplayOptions options, string outputDir);

    /// <summary>
    /// Get available trickplay resolutions and corresponding info.
    /// </summary>
    /// <param name="itemId">The item.</param>
    /// <returns>Map of width resolutions to trickplay tiles info.</returns>
    Task<Dictionary<int, TrickplayInfo>> GetTrickplayResolutions(Guid itemId);

    /// <summary>
    /// Gets the item ids of all items with trickplay info.
    /// </summary>
    /// <param name="limit">The limit of items to return.</param>
    /// <param name="offset">The offset to start the query at.</param>
    /// <returns>The list of item ids that have trickplay info.</returns>
    Task<IReadOnlyList<TrickplayInfo>> GetTrickplayItemsAsync(int limit, int offset);

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
    /// <param name="saveWithMedia">Whether or not the tile should be saved next to the media file.</param>
    /// <returns>The absolute path.</returns>
    Task<string> GetTrickplayTilePathAsync(BaseItem item, int width, int index, bool saveWithMedia);

    /// <summary>
    /// Gets the path to a trickplay tile image.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="tileWidth">The amount of images for the tile width.</param>
    /// <param name="tileHeight">The amount of images for the tile height.</param>
    /// <param name="width">The width of a single thumbnail.</param>
    /// <param name="saveWithMedia">Whether or not the tile should be saved next to the media file.</param>
    /// <returns>The absolute path.</returns>
    string GetTrickplayDirectory(BaseItem item, int tileWidth, int tileHeight, int width, bool saveWithMedia = false);

    /// <summary>
    /// Migrates trickplay images between local and media directories.
    /// </summary>
    /// <param name="video">The video.</param>
    /// <param name="libraryOptions">The library options.</param>
    /// <param name="cancellationToken">CancellationToken to use for operation.</param>
    /// <returns>Task.</returns>
    Task MoveGeneratedTrickplayDataAsync(Video video, LibraryOptions? libraryOptions, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the trickplay HLS playlist.
    /// </summary>
    /// <param name="itemId">The item.</param>
    /// <param name="width">The width of a single thumbnail.</param>
    /// <param name="apiKey">Optional api key of the requesting user.</param>
    /// <returns>The text content of the .m3u8 playlist.</returns>
    Task<string?> GetHlsPlaylist(Guid itemId, int width, string? apiKey);
}
