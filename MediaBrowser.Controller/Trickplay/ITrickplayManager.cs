using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Trickplay
{
    /// <summary>
    /// Interface ITrickplayManager.
    /// </summary>
    public interface ITrickplayManager
    {
        /// <summary>
        /// Generate or replace trickplay data.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="replace">Whether or not existing data should be replaced.</param>
        /// <param name="cancellationToken">CancellationToken to use for operation.</param>
        /// <returns>Task.</returns>
        Task RefreshTrickplayData(Video video, bool replace, CancellationToken cancellationToken);

        /// <summary>
        /// Get available trickplay resolutions and corresponding info.
        /// </summary>
        /// <param name="itemId">The item.</param>
        /// <returns>Map of width resolutions to trickplay tiles info.</returns>
        Dictionary<int, TrickplayTilesInfo> GetTilesResolutions(Guid itemId);

        /// <summary>
        /// Saves trickplay tiles info.
        /// </summary>
        /// <param name="itemId">The item.</param>
        /// <param name="tilesInfo">The trickplay tiles info.</param>
        void SaveTilesInfo(Guid itemId, TrickplayTilesInfo tilesInfo);

        /// <summary>
        /// Gets the trickplay manifest.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>A map of media source id to a map of tile width to tile info.</returns>
        Dictionary<Guid, Dictionary<int, TrickplayTilesInfo>> GetTrickplayManifest(BaseItem item);

        /// <summary>
        /// Gets the path to a trickplay tiles image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="width">The width of a single tile.</param>
        /// <param name="index">The tile grid's index.</param>
        /// <returns>The absolute path.</returns>
        string GetTrickplayTilePath(BaseItem item, int width, int index);
    }
}
