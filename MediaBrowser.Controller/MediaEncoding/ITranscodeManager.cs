using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Streaming;

namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// A service for managing media transcoding.
/// </summary>
public interface ITranscodeManager
{
    /// <summary>
    /// Get transcoding job.
    /// </summary>
    /// <param name="playSessionId">Playback session id.</param>
    /// <returns>The transcoding job.</returns>
    public TranscodingJob? GetTranscodingJob(string playSessionId);

    /// <summary>
    /// Get transcoding job.
    /// </summary>
    /// <param name="path">Path to the transcoding file.</param>
    /// <param name="type">The <see cref="TranscodingJobType"/>.</param>
    /// <returns>The transcoding job.</returns>
    public TranscodingJob? GetTranscodingJob(string path, TranscodingJobType type);

    /// <summary>
    /// Ping transcoding job.
    /// </summary>
    /// <param name="playSessionId">Play session id.</param>
    /// <param name="isUserPaused">Is user paused.</param>
    /// <exception cref="ArgumentNullException">Play session id is null.</exception>
    public void PingTranscodingJob(string playSessionId, bool? isUserPaused);

    /// <summary>
    /// Kills the single transcoding job.
    /// </summary>
    /// <param name="deviceId">The device id.</param>
    /// <param name="playSessionId">The play session identifier.</param>
    /// <param name="deleteFiles">The delete files.</param>
    /// <returns>Task.</returns>
    public Task KillTranscodingJobs(string deviceId, string? playSessionId, Func<string, bool> deleteFiles);

    /// <summary>
    /// Report the transcoding progress to the session manager.
    /// </summary>
    /// <param name="job">The <see cref="TranscodingJob"/> of which the progress will be reported.</param>
    /// <param name="state">The <see cref="StreamState"/> of the current transcoding job.</param>
    /// <param name="transcodingPosition">The current transcoding position.</param>
    /// <param name="framerate">The framerate of the transcoding job.</param>
    /// <param name="percentComplete">The completion percentage of the transcode.</param>
    /// <param name="bytesTranscoded">The number of bytes transcoded.</param>
    /// <param name="bitRate">The bitrate of the transcoding job.</param>
    public void ReportTranscodingProgress(
        TranscodingJob job,
        StreamState state,
        TimeSpan? transcodingPosition,
        float? framerate,
        double? percentComplete,
        long? bytesTranscoded,
        int? bitRate);

    /// <summary>
    /// Starts FFMpeg.
    /// </summary>
    /// <param name="state">The state.</param>
    /// <param name="outputPath">The output path.</param>
    /// <param name="commandLineArguments">The command line arguments for FFmpeg.</param>
    /// <param name="userId">The user id.</param>
    /// <param name="transcodingJobType">The <see cref="TranscodingJobType"/>.</param>
    /// <param name="cancellationTokenSource">The cancellation token source.</param>
    /// <param name="workingDirectory">The working directory.</param>
    /// <returns>Task.</returns>
    public Task<TranscodingJob> StartFfMpeg(
        StreamState state,
        string outputPath,
        string commandLineArguments,
        Guid userId,
        TranscodingJobType transcodingJobType,
        CancellationTokenSource cancellationTokenSource,
        string? workingDirectory = null);

    /// <summary>
    /// Called when [transcode begin request].
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="type">The type.</param>
    /// <returns>The <see cref="TranscodingJob"/>.</returns>
    public TranscodingJob? OnTranscodeBeginRequest(string path, TranscodingJobType type);

    /// <summary>
    /// Called when [transcode end].
    /// </summary>
    /// <param name="job">The transcode job.</param>
    public void OnTranscodeEndRequest(TranscodingJob job);

    /// <summary>
    /// Transcoding lock.
    /// </summary>
    /// <param name="outputPath">The output path of the transcoded file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An <see cref="IDisposable"/>.</returns>
    ValueTask<IDisposable> LockAsync(string outputPath, CancellationToken cancellationToken);
}
