using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Models.PlaybackDtos;
using Jellyfin.Api.Models.StreamingDtos;
using Jellyfin.Data.Enums;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Transcoding job helpers.
/// </summary>
public class TranscodingJobHelper : IDisposable
{
    /// <summary>
    /// The active transcoding jobs.
    /// </summary>
    private static readonly List<TranscodingJobDto> _activeTranscodingJobs = new List<TranscodingJobDto>();

    /// <summary>
    /// The transcoding locks.
    /// </summary>
    private static readonly Dictionary<string, SemaphoreSlim> _transcodingLocks = new Dictionary<string, SemaphoreSlim>();

    private readonly IAttachmentExtractor _attachmentExtractor;
    private readonly IApplicationPaths _appPaths;
    private readonly EncodingHelper _encodingHelper;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<TranscodingJobHelper> _logger;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly ISessionManager _sessionManager;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscodingJobHelper"/> class.
    /// </summary>
    /// <param name="attachmentExtractor">Instance of the <see cref="IAttachmentExtractor"/> interface.</param>
    /// <param name="appPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{TranscodingJobHelpers}"/> interface.</param>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
    /// <param name="encodingHelper">Instance of <see cref="EncodingHelper"/>.</param>
    /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    public TranscodingJobHelper(
        IAttachmentExtractor attachmentExtractor,
        IApplicationPaths appPaths,
        ILogger<TranscodingJobHelper> logger,
        IMediaSourceManager mediaSourceManager,
        IFileSystem fileSystem,
        IMediaEncoder mediaEncoder,
        IServerConfigurationManager serverConfigurationManager,
        ISessionManager sessionManager,
        EncodingHelper encodingHelper,
        ILoggerFactory loggerFactory,
        IUserManager userManager)
    {
        _attachmentExtractor = attachmentExtractor;
        _appPaths = appPaths;
        _logger = logger;
        _mediaSourceManager = mediaSourceManager;
        _fileSystem = fileSystem;
        _mediaEncoder = mediaEncoder;
        _serverConfigurationManager = serverConfigurationManager;
        _sessionManager = sessionManager;
        _encodingHelper = encodingHelper;
        _loggerFactory = loggerFactory;
        _userManager = userManager;

        DeleteEncodedMediaCache();

        sessionManager.PlaybackProgress += OnPlaybackProgress;
        sessionManager.PlaybackStart += OnPlaybackProgress;
    }

    /// <summary>
    /// Get transcoding job.
    /// </summary>
    /// <param name="playSessionId">Playback session id.</param>
    /// <returns>The transcoding job.</returns>
    public TranscodingJobDto? GetTranscodingJob(string playSessionId)
    {
        lock (_activeTranscodingJobs)
        {
            return _activeTranscodingJobs.FirstOrDefault(j => string.Equals(j.PlaySessionId, playSessionId, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Get transcoding job.
    /// </summary>
    /// <param name="path">Path to the transcoding file.</param>
    /// <param name="type">The <see cref="TranscodingJobType"/>.</param>
    /// <returns>The transcoding job.</returns>
    public TranscodingJobDto? GetTranscodingJob(string path, TranscodingJobType type)
    {
        lock (_activeTranscodingJobs)
        {
            return _activeTranscodingJobs.FirstOrDefault(j => j.Type == type && string.Equals(j.Path, path, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Ping transcoding job.
    /// </summary>
    /// <param name="playSessionId">Play session id.</param>
    /// <param name="isUserPaused">Is user paused.</param>
    /// <exception cref="ArgumentNullException">Play session id is null.</exception>
    public void PingTranscodingJob(string playSessionId, bool? isUserPaused)
    {
        ArgumentException.ThrowIfNullOrEmpty(playSessionId);

        _logger.LogDebug("PingTranscodingJob PlaySessionId={0} isUsedPaused: {1}", playSessionId, isUserPaused);

        List<TranscodingJobDto> jobs;

        lock (_activeTranscodingJobs)
        {
            // This is really only needed for HLS.
            // Progressive streams can stop on their own reliably.
            jobs = _activeTranscodingJobs.Where(j => string.Equals(playSessionId, j.PlaySessionId, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        foreach (var job in jobs)
        {
            if (isUserPaused.HasValue)
            {
                _logger.LogDebug("Setting job.IsUserPaused to {0}. jobId: {1}", isUserPaused, job.Id);
                job.IsUserPaused = isUserPaused.Value;
            }

            PingTimer(job, true);
        }
    }

    private void PingTimer(TranscodingJobDto job, bool isProgressCheckIn)
    {
        if (job.HasExited)
        {
            job.StopKillTimer();
            return;
        }

        var timerDuration = 10000;

        if (job.Type != TranscodingJobType.Progressive)
        {
            timerDuration = 60000;
        }

        job.PingTimeout = timerDuration;
        job.LastPingDate = DateTime.UtcNow;

        // Don't start the timer for playback checkins with progressive streaming
        if (job.Type != TranscodingJobType.Progressive || !isProgressCheckIn)
        {
            job.StartKillTimer(OnTranscodeKillTimerStopped);
        }
        else
        {
            job.ChangeKillTimerIfStarted();
        }
    }

    /// <summary>
    /// Called when [transcode kill timer stopped].
    /// </summary>
    /// <param name="state">The state.</param>
    private async void OnTranscodeKillTimerStopped(object? state)
    {
        var job = state as TranscodingJobDto ?? throw new ArgumentException($"{nameof(state)} is not of type {nameof(TranscodingJobDto)}", nameof(state));
        if (!job.HasExited && job.Type != TranscodingJobType.Progressive)
        {
            var timeSinceLastPing = (DateTime.UtcNow - job.LastPingDate).TotalMilliseconds;

            if (timeSinceLastPing < job.PingTimeout)
            {
                job.StartKillTimer(OnTranscodeKillTimerStopped, job.PingTimeout);
                return;
            }
        }

        _logger.LogInformation("Transcoding kill timer stopped for JobId {0} PlaySessionId {1}. Killing transcoding", job.Id, job.PlaySessionId);

        await KillTranscodingJob(job, true, path => true).ConfigureAwait(false);
    }

    /// <summary>
    /// Kills the single transcoding job.
    /// </summary>
    /// <param name="deviceId">The device id.</param>
    /// <param name="playSessionId">The play session identifier.</param>
    /// <param name="deleteFiles">The delete files.</param>
    /// <returns>Task.</returns>
    public Task KillTranscodingJobs(string deviceId, string? playSessionId, Func<string, bool> deleteFiles)
    {
        return KillTranscodingJobs(
            j => string.IsNullOrWhiteSpace(playSessionId)
                ? string.Equals(deviceId, j.DeviceId, StringComparison.OrdinalIgnoreCase)
                : string.Equals(playSessionId, j.PlaySessionId, StringComparison.OrdinalIgnoreCase),
            deleteFiles);
    }

    /// <summary>
    /// Kills the transcoding jobs.
    /// </summary>
    /// <param name="killJob">The kill job.</param>
    /// <param name="deleteFiles">The delete files.</param>
    /// <returns>Task.</returns>
    private Task KillTranscodingJobs(Func<TranscodingJobDto, bool> killJob, Func<string, bool> deleteFiles)
    {
        var jobs = new List<TranscodingJobDto>();

        lock (_activeTranscodingJobs)
        {
            // This is really only needed for HLS.
            // Progressive streams can stop on their own reliably.
            jobs.AddRange(_activeTranscodingJobs.Where(killJob));
        }

        if (jobs.Count == 0)
        {
            return Task.CompletedTask;
        }

        IEnumerable<Task> GetKillJobs()
        {
            foreach (var job in jobs)
            {
                yield return KillTranscodingJob(job, false, deleteFiles);
            }
        }

        return Task.WhenAll(GetKillJobs());
    }

    /// <summary>
    /// Kills the transcoding job.
    /// </summary>
    /// <param name="job">The job.</param>
    /// <param name="closeLiveStream">if set to <c>true</c> [close live stream].</param>
    /// <param name="delete">The delete.</param>
    private async Task KillTranscodingJob(TranscodingJobDto job, bool closeLiveStream, Func<string, bool> delete)
    {
        job.DisposeKillTimer();

        _logger.LogDebug("KillTranscodingJob - JobId {0} PlaySessionId {1}. Killing transcoding", job.Id, job.PlaySessionId);

        lock (_activeTranscodingJobs)
        {
            _activeTranscodingJobs.Remove(job);

            if (job.CancellationTokenSource?.IsCancellationRequested == false)
            {
                job.CancellationTokenSource.Cancel();
            }
        }

        lock (_transcodingLocks)
        {
            _transcodingLocks.Remove(job.Path!);
        }

        lock (job.ProcessLock!)
        {
#pragma warning disable CA1849 // Can't await in lock block
            job.TranscodingThrottler?.Stop().GetAwaiter().GetResult();

            var process = job.Process;

            var hasExited = job.HasExited;

            if (!hasExited)
            {
                try
                {
                    _logger.LogInformation("Stopping ffmpeg process with q command for {Path}", job.Path);

                    process!.StandardInput.WriteLine("q");

                    // Need to wait because killing is asynchronous.
                    if (!process.WaitForExit(5000))
                    {
                        _logger.LogInformation("Killing FFmpeg process for {Path}", job.Path);
                        process.Kill();
                    }
                }
                catch (InvalidOperationException)
                {
                }
            }
#pragma warning restore CA1849
        }

        if (delete(job.Path!))
        {
            await DeletePartialStreamFiles(job.Path!, job.Type, 0, 1500).ConfigureAwait(false);
            if (job.MediaSource?.VideoType == VideoType.Dvd || job.MediaSource?.VideoType == VideoType.BluRay)
            {
                var concatFilePath = Path.Join(_serverConfigurationManager.GetTranscodePath(), job.MediaSource.Id + ".concat");
                if (File.Exists(concatFilePath))
                {
                    _logger.LogInformation("Deleting ffmpeg concat configuration at {Path}", concatFilePath);
                    File.Delete(concatFilePath);
                }
            }
        }

        if (closeLiveStream && !string.IsNullOrWhiteSpace(job.LiveStreamId))
        {
            try
            {
                await _mediaSourceManager.CloseLiveStream(job.LiveStreamId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing live stream for {Path}", job.Path);
            }
        }
    }

    private async Task DeletePartialStreamFiles(string path, TranscodingJobType jobType, int retryCount, int delayMs)
    {
        if (retryCount >= 10)
        {
            return;
        }

        _logger.LogInformation("Deleting partial stream file(s) {Path}", path);

        await Task.Delay(delayMs).ConfigureAwait(false);

        try
        {
            if (jobType == TranscodingJobType.Progressive)
            {
                DeleteProgressivePartialStreamFiles(path);
            }
            else
            {
                DeleteHlsPartialStreamFiles(path);
            }
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Error deleting partial stream file(s) {Path}", path);

            await DeletePartialStreamFiles(path, jobType, retryCount + 1, 500).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting partial stream file(s) {Path}", path);
        }
    }

    /// <summary>
    /// Deletes the progressive partial stream files.
    /// </summary>
    /// <param name="outputFilePath">The output file path.</param>
    private void DeleteProgressivePartialStreamFiles(string outputFilePath)
    {
        if (File.Exists(outputFilePath))
        {
            _fileSystem.DeleteFile(outputFilePath);
        }
    }

    /// <summary>
    /// Deletes the HLS partial stream files.
    /// </summary>
    /// <param name="outputFilePath">The output file path.</param>
    private void DeleteHlsPartialStreamFiles(string outputFilePath)
    {
        var directory = Path.GetDirectoryName(outputFilePath)
                        ?? throw new ArgumentException("Path can't be a root directory.", nameof(outputFilePath));

        var name = Path.GetFileNameWithoutExtension(outputFilePath);

        var filesToDelete = _fileSystem.GetFilePaths(directory)
            .Where(f => f.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1);

        List<Exception>? exs = null;
        foreach (var file in filesToDelete)
        {
            try
            {
                _logger.LogDebug("Deleting HLS file {0}", file);
                _fileSystem.DeleteFile(file);
            }
            catch (IOException ex)
            {
                (exs ??= new List<Exception>(4)).Add(ex);
                _logger.LogError(ex, "Error deleting HLS file {Path}", file);
            }
        }

        if (exs is not null)
        {
            throw new AggregateException("Error deleting HLS files", exs);
        }
    }

    /// <summary>
    /// Report the transcoding progress to the session manager.
    /// </summary>
    /// <param name="job">The <see cref="TranscodingJobDto"/> of which the progress will be reported.</param>
    /// <param name="state">The <see cref="StreamState"/> of the current transcoding job.</param>
    /// <param name="transcodingPosition">The current transcoding position.</param>
    /// <param name="framerate">The framerate of the transcoding job.</param>
    /// <param name="percentComplete">The completion percentage of the transcode.</param>
    /// <param name="bytesTranscoded">The number of bytes transcoded.</param>
    /// <param name="bitRate">The bitrate of the transcoding job.</param>
    public void ReportTranscodingProgress(
        TranscodingJobDto job,
        StreamState state,
        TimeSpan? transcodingPosition,
        float? framerate,
        double? percentComplete,
        long? bytesTranscoded,
        int? bitRate)
    {
        var ticks = transcodingPosition?.Ticks;

        if (job is not null)
        {
            job.Framerate = framerate;
            job.CompletionPercentage = percentComplete;
            job.TranscodingPositionTicks = ticks;
            job.BytesTranscoded = bytesTranscoded;
            job.BitRate = bitRate;
        }

        var deviceId = state.Request.DeviceId;

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            var audioCodec = state.ActualOutputAudioCodec;
            var videoCodec = state.ActualOutputVideoCodec;
            var hardwareAccelerationTypeString = _serverConfigurationManager.GetEncodingOptions().HardwareAccelerationType;
            HardwareEncodingType? hardwareAccelerationType = null;
            if (Enum.TryParse<HardwareEncodingType>(hardwareAccelerationTypeString, out var parsedHardwareAccelerationType))
            {
                hardwareAccelerationType = parsedHardwareAccelerationType;
            }

            _sessionManager.ReportTranscodingInfo(deviceId, new TranscodingInfo
            {
                Bitrate = bitRate ?? state.TotalOutputBitrate,
                AudioCodec = audioCodec,
                VideoCodec = videoCodec,
                Container = state.OutputContainer,
                Framerate = framerate,
                CompletionPercentage = percentComplete,
                Width = state.OutputWidth,
                Height = state.OutputHeight,
                AudioChannels = state.OutputAudioChannels,
                IsAudioDirect = EncodingHelper.IsCopyCodec(state.OutputAudioCodec),
                IsVideoDirect = EncodingHelper.IsCopyCodec(state.OutputVideoCodec),
                HardwareAccelerationType = hardwareAccelerationType,
                TranscodeReasons = state.TranscodeReasons
            });
        }
    }

    /// <summary>
    /// Starts FFmpeg.
    /// </summary>
    /// <param name="state">The state.</param>
    /// <param name="outputPath">The output path.</param>
    /// <param name="commandLineArguments">The command line arguments for FFmpeg.</param>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="transcodingJobType">The <see cref="TranscodingJobType"/>.</param>
    /// <param name="cancellationTokenSource">The cancellation token source.</param>
    /// <param name="workingDirectory">The working directory.</param>
    /// <returns>Task.</returns>
    public async Task<TranscodingJobDto> StartFfMpeg(
        StreamState state,
        string outputPath,
        string commandLineArguments,
        HttpRequest request,
        TranscodingJobType transcodingJobType,
        CancellationTokenSource cancellationTokenSource,
        string? workingDirectory = null)
    {
        var directory = Path.GetDirectoryName(outputPath) ?? throw new ArgumentException($"Provided path ({outputPath}) is not valid.", nameof(outputPath));
        Directory.CreateDirectory(directory);

        await AcquireResources(state, cancellationTokenSource).ConfigureAwait(false);

        if (state.VideoRequest is not null && !EncodingHelper.IsCopyCodec(state.OutputVideoCodec))
        {
            var userId = request.HttpContext.User.GetUserId();
            var user = userId.Equals(default) ? null : _userManager.GetUserById(userId);
            if (user is not null && !user.HasPermission(PermissionKind.EnableVideoPlaybackTranscoding))
            {
                this.OnTranscodeFailedToStart(outputPath, transcodingJobType, state);

                throw new ArgumentException("User does not have access to video transcoding.");
            }
        }

        ArgumentException.ThrowIfNullOrEmpty(_mediaEncoder.EncoderPath);

        // If subtitles get burned in fonts may need to be extracted from the media file
        if (state.SubtitleStream is not null && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode)
        {
            var attachmentPath = Path.Combine(_appPaths.CachePath, "attachments", state.MediaSource.Id);
            if (state.VideoType != VideoType.Dvd)
            {
                await _attachmentExtractor.ExtractAllAttachments(state.MediaPath, state.MediaSource, attachmentPath, cancellationTokenSource.Token).ConfigureAwait(false);
            }

            if (state.SubtitleStream.IsExternal && Path.GetExtension(state.SubtitleStream.Path.AsSpan()).Equals(".mks", StringComparison.OrdinalIgnoreCase))
            {
                string subtitlePath = state.SubtitleStream.Path;
                string subtitlePathArgument = string.Format(CultureInfo.InvariantCulture, "file:\"{0}\"", subtitlePath.Replace("\"", "\\\"", StringComparison.Ordinal));
                string subtitleId = subtitlePath.GetMD5().ToString("N", CultureInfo.InvariantCulture);

                await _attachmentExtractor.ExtractAllAttachmentsExternal(subtitlePathArgument, subtitleId, attachmentPath, cancellationTokenSource.Token).ConfigureAwait(false);
            }
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,

                // Must consume both stdout and stderr or deadlocks may occur
                // RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                FileName = _mediaEncoder.EncoderPath,
                Arguments = commandLineArguments,
                WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? string.Empty : workingDirectory,
                ErrorDialog = false
            },
            EnableRaisingEvents = true
        };

        var transcodingJob = this.OnTranscodeBeginning(
            outputPath,
            state.Request.PlaySessionId,
            state.MediaSource.LiveStreamId,
            Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
            transcodingJobType,
            process,
            state.Request.DeviceId,
            state,
            cancellationTokenSource);

        _logger.LogInformation("{Filename} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);

        var logFilePrefix = "FFmpeg.Transcode-";
        if (state.VideoRequest is not null
            && EncodingHelper.IsCopyCodec(state.OutputVideoCodec))
        {
            logFilePrefix = EncodingHelper.IsCopyCodec(state.OutputAudioCodec)
                ? "FFmpeg.Remux-"
                : "FFmpeg.DirectStream-";
        }

        var logFilePath = Path.Combine(
            _serverConfigurationManager.ApplicationPaths.LogDirectoryPath,
            $"{logFilePrefix}{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{state.Request.MediaSourceId}_{Guid.NewGuid().ToString()[..8]}.log");

        // FFmpeg writes debug/error info to stderr. This is useful when debugging so let's put it in the log directory.
        Stream logStream = new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, IODefaults.FileStreamBufferSize, FileOptions.Asynchronous);

        var commandLineLogMessage = process.StartInfo.FileName + " " + process.StartInfo.Arguments;
        var commandLineLogMessageBytes = Encoding.UTF8.GetBytes(request.Path + Environment.NewLine + Environment.NewLine + JsonSerializer.Serialize(state.MediaSource) + Environment.NewLine + Environment.NewLine + commandLineLogMessage + Environment.NewLine + Environment.NewLine);
        await logStream.WriteAsync(commandLineLogMessageBytes, cancellationTokenSource.Token).ConfigureAwait(false);

        process.Exited += (sender, args) => OnFfMpegProcessExited(process, transcodingJob, state);

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting FFmpeg");

            this.OnTranscodeFailedToStart(outputPath, transcodingJobType, state);

            throw;
        }

        _logger.LogDebug("Launched FFmpeg process");
        state.TranscodingJob = transcodingJob;

        // Important - don't await the log task or we won't be able to kill FFmpeg when the user stops playback
        _ = new JobLogger(_logger).StartStreamingLog(state, process.StandardError, logStream);

        // Wait for the file to exist before proceeding
        var ffmpegTargetFile = state.WaitForPath ?? outputPath;
        _logger.LogDebug("Waiting for the creation of {0}", ffmpegTargetFile);
        while (!File.Exists(ffmpegTargetFile) && !transcodingJob.HasExited)
        {
            await Task.Delay(100, cancellationTokenSource.Token).ConfigureAwait(false);
        }

        _logger.LogDebug("File {0} created or transcoding has finished", ffmpegTargetFile);

        if (state.IsInputVideo && transcodingJob.Type == TranscodingJobType.Progressive && !transcodingJob.HasExited)
        {
            await Task.Delay(1000, cancellationTokenSource.Token).ConfigureAwait(false);

            if (state.ReadInputAtNativeFramerate && !transcodingJob.HasExited)
            {
                await Task.Delay(1500, cancellationTokenSource.Token).ConfigureAwait(false);
            }
        }

        if (!transcodingJob.HasExited)
        {
            StartThrottler(state, transcodingJob);
        }
        else if (transcodingJob.ExitCode != 0)
        {
            throw new FfmpegException(string.Format(CultureInfo.InvariantCulture, "FFmpeg exited with code {0}", transcodingJob.ExitCode));
        }

        _logger.LogDebug("StartFfMpeg() finished successfully");

        return transcodingJob;
    }

    private void StartThrottler(StreamState state, TranscodingJobDto transcodingJob)
    {
        if (EnableThrottling(state))
        {
            transcodingJob.TranscodingThrottler = new TranscodingThrottler(transcodingJob, _loggerFactory.CreateLogger<TranscodingThrottler>(), _serverConfigurationManager, _fileSystem, _mediaEncoder);
            transcodingJob.TranscodingThrottler.Start();
        }
    }

    private bool EnableThrottling(StreamState state)
    {
        var encodingOptions = _serverConfigurationManager.GetEncodingOptions();

        return state.InputProtocol == MediaProtocol.File &&
               state.RunTimeTicks.HasValue &&
               state.RunTimeTicks.Value >= TimeSpan.FromMinutes(5).Ticks &&
               state.IsInputVideo &&
               state.VideoType == VideoType.VideoFile;
    }

    /// <summary>
    /// Called when [transcode beginning].
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="playSessionId">The play session identifier.</param>
    /// <param name="liveStreamId">The live stream identifier.</param>
    /// <param name="transcodingJobId">The transcoding job identifier.</param>
    /// <param name="type">The type.</param>
    /// <param name="process">The process.</param>
    /// <param name="deviceId">The device id.</param>
    /// <param name="state">The state.</param>
    /// <param name="cancellationTokenSource">The cancellation token source.</param>
    /// <returns>TranscodingJob.</returns>
    public TranscodingJobDto OnTranscodeBeginning(
        string path,
        string? playSessionId,
        string? liveStreamId,
        string transcodingJobId,
        TranscodingJobType type,
        Process process,
        string? deviceId,
        StreamState state,
        CancellationTokenSource cancellationTokenSource)
    {
        lock (_activeTranscodingJobs)
        {
            var job = new TranscodingJobDto(_loggerFactory.CreateLogger<TranscodingJobDto>())
            {
                Type = type,
                Path = path,
                Process = process,
                ActiveRequestCount = 1,
                DeviceId = deviceId,
                CancellationTokenSource = cancellationTokenSource,
                Id = transcodingJobId,
                PlaySessionId = playSessionId,
                LiveStreamId = liveStreamId,
                MediaSource = state.MediaSource
            };

            _activeTranscodingJobs.Add(job);

            ReportTranscodingProgress(job, state, null, null, null, null, null);

            return job;
        }
    }

    /// <summary>
    /// Called when [transcode end].
    /// </summary>
    /// <param name="job">The transcode job.</param>
    public void OnTranscodeEndRequest(TranscodingJobDto job)
    {
        job.ActiveRequestCount--;
        _logger.LogDebug("OnTranscodeEndRequest job.ActiveRequestCount={ActiveRequestCount}", job.ActiveRequestCount);
        if (job.ActiveRequestCount <= 0)
        {
            PingTimer(job, false);
        }
    }

    /// <summary>
    /// <summary>
    /// The progressive
    /// </summary>
    /// Called when [transcode failed to start].
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="type">The type.</param>
    /// <param name="state">The state.</param>
    public void OnTranscodeFailedToStart(string path, TranscodingJobType type, StreamState state)
    {
        lock (_activeTranscodingJobs)
        {
            var job = _activeTranscodingJobs.FirstOrDefault(j => j.Type == type && string.Equals(j.Path, path, StringComparison.OrdinalIgnoreCase));

            if (job is not null)
            {
                _activeTranscodingJobs.Remove(job);
            }
        }

        lock (_transcodingLocks)
        {
            _transcodingLocks.Remove(path);
        }

        if (!string.IsNullOrWhiteSpace(state.Request.DeviceId))
        {
            _sessionManager.ClearTranscodingInfo(state.Request.DeviceId);
        }
    }

    /// <summary>
    /// Processes the exited.
    /// </summary>
    /// <param name="process">The process.</param>
    /// <param name="job">The job.</param>
    /// <param name="state">The state.</param>
    private void OnFfMpegProcessExited(Process process, TranscodingJobDto job, StreamState state)
    {
        job.HasExited = true;
        job.ExitCode = process.ExitCode;

        ReportTranscodingProgress(job, state, null, null, null, null, null);

        _logger.LogDebug("Disposing stream resources");
        state.Dispose();

        if (process.ExitCode == 0)
        {
            _logger.LogInformation("FFmpeg exited with code 0");
        }
        else
        {
            _logger.LogError("FFmpeg exited with code {0}", process.ExitCode);
        }

        job.Dispose();
    }

    private async Task AcquireResources(StreamState state, CancellationTokenSource cancellationTokenSource)
    {
        if (state.MediaSource.RequiresOpening && string.IsNullOrWhiteSpace(state.Request.LiveStreamId))
        {
            var liveStreamResponse = await _mediaSourceManager.OpenLiveStream(
                    new LiveStreamRequest { OpenToken = state.MediaSource.OpenToken },
                    cancellationTokenSource.Token)
                .ConfigureAwait(false);
            var encodingOptions = _serverConfigurationManager.GetEncodingOptions();

            _encodingHelper.AttachMediaSourceInfo(state, encodingOptions, liveStreamResponse.MediaSource, state.RequestedUrl);

            if (state.VideoRequest is not null)
            {
                _encodingHelper.TryStreamCopy(state);
            }
        }

        if (state.MediaSource.BufferMs.HasValue)
        {
            await Task.Delay(state.MediaSource.BufferMs.Value, cancellationTokenSource.Token).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Called when [transcode begin request].
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="type">The type.</param>
    /// <returns>The <see cref="TranscodingJobDto"/>.</returns>
    public TranscodingJobDto? OnTranscodeBeginRequest(string path, TranscodingJobType type)
    {
        lock (_activeTranscodingJobs)
        {
            var job = _activeTranscodingJobs.FirstOrDefault(j => j.Type == type && string.Equals(j.Path, path, StringComparison.OrdinalIgnoreCase));

            if (job is null)
            {
                return null;
            }

            OnTranscodeBeginRequest(job);

            return job;
        }
    }

    private void OnTranscodeBeginRequest(TranscodingJobDto job)
    {
        job.ActiveRequestCount++;

        if (string.IsNullOrWhiteSpace(job.PlaySessionId) || job.Type == TranscodingJobType.Progressive)
        {
            job.StopKillTimer();
        }
    }

    /// <summary>
    /// Gets the transcoding lock.
    /// </summary>
    /// <param name="outputPath">The output path of the transcoded file.</param>
    /// <returns>A <see cref="SemaphoreSlim"/>.</returns>
    public SemaphoreSlim GetTranscodingLock(string outputPath)
    {
        lock (_transcodingLocks)
        {
            if (!_transcodingLocks.TryGetValue(outputPath, out SemaphoreSlim? result))
            {
                result = new SemaphoreSlim(1, 1);
                _transcodingLocks[outputPath] = result;
            }

            return result;
        }
    }

    private void OnPlaybackProgress(object? sender, PlaybackProgressEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.PlaySessionId))
        {
            PingTranscodingJob(e.PlaySessionId, e.IsPaused);
        }
    }

    /// <summary>
    /// Deletes the encoded media cache.
    /// </summary>
    private void DeleteEncodedMediaCache()
    {
        var path = _serverConfigurationManager.GetTranscodePath();
        if (!Directory.Exists(path))
        {
            return;
        }

        foreach (var file in _fileSystem.GetFilePaths(path, true))
        {
            _fileSystem.DeleteFile(file);
        }
    }

    /// <summary>
    /// Dispose transcoding job helper.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose throttler.
    /// </summary>
    /// <param name="disposing">Disposing.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _loggerFactory.Dispose();
            _sessionManager.PlaybackProgress -= OnPlaybackProgress;
            _sessionManager.PlaybackStart -= OnPlaybackProgress;
        }
    }
}
