using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Model.Waveform;

namespace MediaBrowser.Controller.Waveform;

/// <summary>
/// Interface IWaveformManager.
/// </summary>
public interface IWaveformManager
{
    /// <summary>
    /// Gets or generates waveform data for an item.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The waveform data, or null if generation fails.</returns>
    Task<WaveformDto?> GetWaveformAsync(Guid itemId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves waveform info to the database.
    /// </summary>
    /// <param name="info">The waveform info.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task.</returns>
    Task SaveWaveformInfoAsync(WaveformInfo info, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes waveform data and database entry for an item.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task.</returns>
    Task DeleteWaveformDataAsync(Guid itemId, CancellationToken cancellationToken);
}
