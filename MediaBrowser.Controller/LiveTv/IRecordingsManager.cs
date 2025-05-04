using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.LiveTv;

/// <summary>
/// Service responsible for managing LiveTV recordings.
/// </summary>
public interface IRecordingsManager
{
    /// <summary>
    /// Gets the path for the provided timer id.
    /// </summary>
    /// <param name="id">The timer id.</param>
    /// <returns>The recording path, or <c>null</c> if none exists.</returns>
    string? GetActiveRecordingPath(string id);

    /// <summary>
    /// Gets the information for an active recording.
    /// </summary>
    /// <param name="path">The recording path.</param>
    /// <returns>The <see cref="ActiveRecordingInfo"/>, or <c>null</c> if none exists.</returns>
    ActiveRecordingInfo? GetActiveRecordingInfo(string path);

    /// <summary>
    /// Gets the recording folders.
    /// </summary>
    /// <returns>The <see cref="VirtualFolderInfo"/> for each recording folder.</returns>
    IEnumerable<VirtualFolderInfo> GetRecordingFolders();

    /// <summary>
    /// Ensures that the recording folders all exist, and removes unused folders.
    /// </summary>
    /// <returns>Task.</returns>
    Task CreateRecordingFolders();

    /// <summary>
    /// Cancels the recording with the provided timer id, if one is active.
    /// </summary>
    /// <param name="timerId">The timer id.</param>
    /// <param name="timer">The timer.</param>
    void CancelRecording(string timerId, TimerInfo? timer);

    /// <summary>
    /// Records a stream.
    /// </summary>
    /// <param name="recordingInfo">The recording info.</param>
    /// <param name="channel">The channel associated with the recording timer.</param>
    /// <param name="recordingEndDate">The time to stop recording.</param>
    /// <returns>Task representing the recording process.</returns>
    Task RecordStream(ActiveRecordingInfo recordingInfo, BaseItem channel, DateTime recordingEndDate);
}
