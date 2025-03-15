using System;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;

namespace MediaBrowser.Controller.AudioWaveform;

/// <summary>
/// Interface IAudioWaveformManager.
/// </summary>
public interface IAudioWaveformManager
{
    /// <summary>
    /// Generates new audioWaveform metadata.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <returns>Task.</returns>
    Task<FileStream> GetAudioWaveformAnsyc(Guid itemId);

    /// <summary>
    /// Saves audioWaveform info.
    /// </summary>
    /// <param name="info">The audioWaveform info.</param>
    /// <returns>Task.</returns>
    Task SaveAudioWaveformInfo(AudioWaveformInfo info);
}
