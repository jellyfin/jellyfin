using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.MediaEncoding.Keyframes;

namespace MediaBrowser.Controller.IO;

/// <summary>
/// Interface IKeyframeManager.
/// </summary>
public interface IKeyframeManager
{
    /// <summary>
    /// Gets the keyframe data.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <returns>The keyframe data.</returns>
    IReadOnlyList<KeyframeData> GetKeyframeData(Guid itemId);

    /// <summary>
    /// Saves the keyframe data.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="data">The keyframe data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    Task SaveKeyframeDataAsync(Guid itemId, KeyframeData data, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the keyframe data.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    Task DeleteKeyframeDataAsync(Guid itemId, CancellationToken cancellationToken);
}
