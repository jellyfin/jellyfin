using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AsyncKeyedLock;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.Transcoding;

/// <inheritdoc cref="ITranscodeManager"/>
public sealed class TranscodeManager : ITranscodeManager, IDisposable
{
    /// <summary>
    /// How long a measured output size stays valid before the segment directory is walked again.
    /// </summary>
    private static readonly TimeSpan TranscodedSizeInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long the graph probe is given before it is killed.
    /// </summary>
    private static readonly TimeSpan GraphProbeTimeout = TimeSpan.FromSeconds(30);

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<TranscodeManager> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IApplicationPaths _appPaths;
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly IUserManager _userManager;
    private readonly ISessionManager _sessionManager;
    private readonly EncodingHelper _encodingHelper;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IAttachmentExtractor _attachmentExtractor;

    private readonly List<TranscodingJob> _activeTranscodingJobs = [];

    private readonly ConcurrentDictionary<string, TranscodingPipelineInfo> _pipelineGraphCache = new(StringComparer.OrdinalIgnoreCase);

    // Cached output size per job, so the segment directory isn't walked on every progress report.
    private readonly ConcurrentDictionary<string, TranscodedSize> _transcodedSizeCache = new(StringComparer.OrdinalIgnoreCase);

    // The graph probe runs a second, short-lived ffmpeg that opens the same decoder, filters and
    // encoder as the real transcode.
    private readonly SemaphoreSlim _graphProbeLock = new(1, 1);

    private readonly AsyncKeyedLocker<string> _transcodingLocks = new(o =>
    {
        o.PoolSize = 20;
        o.PoolInitialFill = 1;
    });

    private readonly Version _maxFFmpegCkeyPauseSupported = new(6, 1);
    private readonly Version _minFFmpegPrintGraphs = new(8, 0);

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscodeManager"/> class.
    /// </summary>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="fileSystem">The <see cref="IFileSystem"/>.</param>
    /// <param name="appPaths">The <see cref="IApplicationPaths"/>.</param>
    /// <param name="serverConfigurationManager">The <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="userManager">The <see cref="IUserManager"/>.</param>
    /// <param name="sessionManager">The <see cref="ISessionManager"/>.</param>
    /// <param name="encodingHelper">The <see cref="EncodingHelper"/>.</param>
    /// <param name="mediaEncoder">The <see cref="IMediaEncoder"/>.</param>
    /// <param name="mediaSourceManager">The <see cref="IMediaSourceManager"/>.</param>
    /// <param name="attachmentExtractor">The <see cref="IAttachmentExtractor"/>.</param>
    public TranscodeManager(
        ILoggerFactory loggerFactory,
        IFileSystem fileSystem,
        IApplicationPaths appPaths,
        IServerConfigurationManager serverConfigurationManager,
        IUserManager userManager,
        ISessionManager sessionManager,
        EncodingHelper encodingHelper,
        IMediaEncoder mediaEncoder,
        IMediaSourceManager mediaSourceManager,
        IAttachmentExtractor attachmentExtractor)
    {
        _loggerFactory = loggerFactory;
        _fileSystem = fileSystem;
        _appPaths = appPaths;
        _serverConfigurationManager = serverConfigurationManager;
        _userManager = userManager;
        _sessionManager = sessionManager;
        _encodingHelper = encodingHelper;
        _mediaEncoder = mediaEncoder;
        _mediaSourceManager = mediaSourceManager;
        _attachmentExtractor = attachmentExtractor;

        _logger = loggerFactory.CreateLogger<TranscodeManager>();
        DeleteEncodedMediaCache();
        _sessionManager.PlaybackProgress += OnPlaybackProgress;
        _sessionManager.PlaybackStart += OnPlaybackProgress;
    }

    /// <inheritdoc />
    public TranscodingJob? GetTranscodingJob(string playSessionId)
    {
        lock (_activeTranscodingJobs)
        {
            return _activeTranscodingJobs.FirstOrDefault(j => string.Equals(j.PlaySessionId, playSessionId, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <inheritdoc />
    public TranscodingJob? GetTranscodingJob(string path, TranscodingJobType type)
    {
        lock (_activeTranscodingJobs)
        {
            return _activeTranscodingJobs.FirstOrDefault(j => j.Type == type && string.Equals(j.Path, path, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <inheritdoc />
    public void PingTranscodingJob(string playSessionId, bool? isUserPaused)
    {
        ArgumentException.ThrowIfNullOrEmpty(playSessionId);

        _logger.LogDebug("PingTranscodingJob PlaySessionId={0} isUsedPaused: {1}", playSessionId, isUserPaused);

        List<TranscodingJob> jobs;

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

    private void PingTimer(TranscodingJob job, bool isProgressCheckIn)
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

    private async void OnTranscodeKillTimerStopped(object? state)
    {
        var job = state as TranscodingJob ?? throw new ArgumentException($"{nameof(state)} is not of type {nameof(TranscodingJob)}", nameof(state));
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

    /// <inheritdoc />
    public Task KillTranscodingJobs(string deviceId, string? playSessionId, Func<string, bool> deleteFiles)
    {
        var jobs = new List<TranscodingJob>();

        lock (_activeTranscodingJobs)
        {
            // This is really only needed for HLS.
            // Progressive streams can stop on their own reliably.
            jobs.AddRange(_activeTranscodingJobs.Where(j => string.IsNullOrWhiteSpace(playSessionId)
                ? string.Equals(deviceId, j.DeviceId, StringComparison.OrdinalIgnoreCase)
                : string.Equals(playSessionId, j.PlaySessionId, StringComparison.OrdinalIgnoreCase)));
        }

        return Task.WhenAll(GetKillJobs());

        IEnumerable<Task> GetKillJobs()
        {
            foreach (var job in jobs)
            {
                yield return KillTranscodingJob(job, false, deleteFiles);
            }
        }
    }

    private async Task KillTranscodingJob(TranscodingJob job, bool closeLiveStream, Func<string, bool> delete)
    {
        job.DisposeKillTimer();

        _logger.LogDebug("KillTranscodingJob - JobId {0} PlaySessionId {1}. Killing transcoding", job.Id, job.PlaySessionId);

        lock (_activeTranscodingJobs)
        {
            _activeTranscodingJobs.Remove(job);

            // Drop the cached pipeline graph once the last job for this play session is gone. The
            // graph probe re-checks this list under the same lock before writing, so a probe that
            // finishes after the job ended cannot resurrect the entry.
            if (!string.IsNullOrEmpty(job.PlaySessionId)
                && !_activeTranscodingJobs.Any(j => string.Equals(j.PlaySessionId, job.PlaySessionId, StringComparison.OrdinalIgnoreCase)))
            {
                _pipelineGraphCache.TryRemove(job.PlaySessionId, out _);
            }

            if (!string.IsNullOrEmpty(job.Id))
            {
                _transcodedSizeCache.TryRemove(job.Id, out _);
            }

            if (job.CancellationTokenSource?.IsCancellationRequested == false)
            {
#pragma warning disable CA1849 // Can't await in lock block
                job.CancellationTokenSource.Cancel();
#pragma warning restore CA1849
            }
        }

        job.Stop();

        if (delete(job.Path!))
        {
            await DeletePartialStreamFiles(job.Path!, job.Type, 0, 1500).ConfigureAwait(false);
        }

        if (closeLiveStream && !string.IsNullOrWhiteSpace(job.LiveStreamId))
        {
            await _sessionManager.CloseLiveStreamIfNeededAsync(job.LiveStreamId, job.PlaySessionId).ConfigureAwait(false);
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

    private void DeleteProgressivePartialStreamFiles(string outputFilePath)
    {
        if (File.Exists(outputFilePath))
        {
            _fileSystem.DeleteFile(outputFilePath);
        }
    }

    private void DeleteHlsPartialStreamFiles(string outputFilePath)
    {
        var directory = Path.GetDirectoryName(outputFilePath)
                        ?? throw new ArgumentException("Path can't be a root directory.", nameof(outputFilePath));

        var name = Path.GetFileNameWithoutExtension(outputFilePath);

        var filesToDelete = _fileSystem.GetFilePaths(directory)
            .Where(f => f.Contains(name, StringComparison.OrdinalIgnoreCase));

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
                (exs ??= []).Add(ex);
                _logger.LogError(ex, "Error deleting HLS file {Path}", file);
            }
        }

        if (exs is not null)
        {
            throw new AggregateException("Error deleting HLS files", exs);
        }
    }

    /// <inheritdoc />
    public void ReportTranscodingProgress(
        TranscodingJob job,
        StreamState state,
        TimeSpan? transcodingPosition,
        float? framerate,
        double? percentComplete,
        long? bytesTranscoded,
        int? bitRate,
        float? encodingSpeed)
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
            // Report acceleration only when this job actually uses it. The configured type is a
            // server-wide preference, not a statement about what is running: a remux, a software
            // transcode and an audio-only transcode all touch no video hardware, and reporting the
            // configured type for them claims an acceleration that isn't happening.
            var usesVideoHardware = job?.Pipeline?.Stages?
                .Any(s => s.IsHardware && string.Equals(s.MediaType, "Video", StringComparison.Ordinal)) == true;

            var hardwareAccelerationType = usesVideoHardware
                ? _serverConfigurationManager.GetEncodingOptions().HardwareAccelerationType
                : HardwareAccelerationType.none;

            // The transcoder buffer is how far the transcoder has run ahead of the playback head.
            long? transcodeBufferTicks = null;
            if (ticks.HasValue && job?.DownloadPositionTicks is { } playbackTicks)
            {
                transcodeBufferTicks = Math.Max(0, ticks.Value - playbackTicks);
            }

            _sessionManager.ReportTranscodingInfo(deviceId, new TranscodingInfo
            {
                Bitrate = bitRate ?? state.TotalOutputBitrate,
                AudioBitrate = state.OutputAudioBitrate,
                VideoBitrate = state.OutputVideoBitrate,
                BytesTranscoded = (job is null ? null : GetTranscodedBytes(job)) ?? bytesTranscoded,
                AudioCodec = audioCodec,
                VideoCodec = videoCodec,
                Container = state.OutputContainer,
                SubtitleCodec = state.SubtitleStream?.Codec,
                SubtitleDeliveryMethod = state.SubtitleStream is null ? null : state.SubtitleDeliveryMethod.ToString(),
                TranscodeProtocol = job?.Type switch
                {
                    TranscodingJobType.Hls => "hls",
                    TranscodingJobType.Dash => "dash",
                    _ => "http"
                },
                Framerate = framerate,
                Speed = encodingSpeed,
                CompletionPercentage = percentComplete,
                Width = state.OutputWidth,
                Height = state.OutputHeight,
                AudioChannels = state.OutputAudioChannels,
                IsAudioDirect = EncodingHelper.IsCopyCodec(state.OutputAudioCodec),
                IsVideoDirect = EncodingHelper.IsCopyCodec(state.OutputVideoCodec),
                HardwareAccelerationType = hardwareAccelerationType,
                TranscodeReasons = state.TranscodeReasons,
                TranscodePositionTicks = ticks,
                TranscodeBufferTicks = transcodeBufferTicks,
                IsThrottled = job?.TranscodingThrottler?.IsPaused ?? false,
                Pipeline = job?.Pipeline
            });
        }
    }

    /// <summary>
    /// Determines how many bytes the transcoder has written to disk so far.
    /// ffmpeg reports <c>size=N/A</c> for segmented (HLS/DASH) output, so the size is
    /// summed from the segment files that share the playlist's base name. Progressive
    /// output is a single file whose length is read directly.
    /// </summary>
    /// <remarks>
    /// The segmented path walks the transcode directory, which is shared by every concurrent job,
    /// so the result is cached for <see cref="TranscodedSizeInterval"/> instead of being recomputed
    /// on every progress report. The value is also kept monotonic: the segment cleaner deletes
    /// already-played segments, and a counter of produced bytes must not go backwards.
    /// </remarks>
    private long? GetTranscodedBytes(TranscodingJob job)
    {
        var path = job.Path;
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        var cacheKey = job.Id;
        var now = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(cacheKey)
            && _transcodedSizeCache.TryGetValue(cacheKey, out var cached)
            && now - cached.MeasuredAt < TranscodedSizeInterval)
        {
            return cached.Bytes;
        }

        long? measured;
        try
        {
            if (job.Type == TranscodingJobType.Progressive)
            {
                var info = _fileSystem.GetFileInfo(path);
                measured = info.Exists ? info.Length : null;
            }
            else
            {
                measured = SumSegmentBytes(path);
            }
        }
        catch (IOException)
        {
            return string.IsNullOrEmpty(cacheKey) ? null : _transcodedSizeCache.GetValueOrDefault(cacheKey).Bytes;
        }

        if (string.IsNullOrEmpty(cacheKey))
        {
            return measured;
        }

        var previous = _transcodedSizeCache.GetValueOrDefault(cacheKey).Bytes;
        var total = measured is null ? previous : Math.Max(measured.Value, previous ?? 0);
        _transcodedSizeCache[cacheKey] = new TranscodedSize(total, now);

        return total;
    }

    private long? SumSegmentBytes(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory))
        {
            return null;
        }

        // Segments are the playlist's base name with an index appended ("<name>1.ts").
        var name = Path.GetFileNameWithoutExtension(path);

        long total = 0;
        foreach (var file in _fileSystem.GetFiles(directory, false))
        {
            if (file.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
            {
                total += file.Length;
            }
        }

        return total > 0 ? total : null;
    }

    /// <inheritdoc />
    public async Task<TranscodingJob> StartFfMpeg(
        StreamState state,
        string outputPath,
        string commandLineArguments,
        Guid userId,
        TranscodingJobType transcodingJobType,
        CancellationTokenSource cancellationTokenSource,
        string? workingDirectory = null)
    {
        var directory = Path.GetDirectoryName(outputPath) ?? throw new ArgumentException($"Provided path ({outputPath}) is not valid.", nameof(outputPath));
        Directory.CreateDirectory(directory);

        await AcquireResources(state, cancellationTokenSource).ConfigureAwait(false);

        if (state.VideoRequest is not null && !EncodingHelper.IsCopyCodec(state.OutputVideoCodec))
        {
            var user = userId.IsEmpty() ? null : _userManager.GetUserById(userId);
            if (user is not null && !user.HasPermission(PermissionKind.EnableVideoPlaybackTranscoding))
            {
                OnTranscodeFailedToStart(outputPath, transcodingJobType, state);

                throw new ArgumentException("User does not have access to video transcoding.");
            }
        }

        ArgumentException.ThrowIfNullOrEmpty(_mediaEncoder.EncoderPath);

        // If subtitles get burned in fonts may need to be extracted from the media file
        if (state.SubtitleStream is not null && (state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode || state.BaseRequest.AlwaysBurnInSubtitleWhenTranscoding))
        {
            if (state.MediaSource.VideoType == VideoType.Dvd || state.MediaSource.VideoType == VideoType.BluRay)
            {
                var concatPath = Path.Join(_appPaths.CachePath, "concat", state.MediaSource.Id + ".concat");
                await _attachmentExtractor.ExtractAllAttachments(concatPath, state.MediaSource, cancellationTokenSource.Token).ConfigureAwait(false);
            }
            else
            {
                await _attachmentExtractor.ExtractAllAttachments(state.MediaPath, state.MediaSource, cancellationTokenSource.Token).ConfigureAwait(false);
            }

            if (state.SubtitleStream.IsExternal && Path.GetExtension(state.SubtitleStream.Path.AsSpan()).Equals(".mks", StringComparison.OrdinalIgnoreCase))
            {
                await _attachmentExtractor.ExtractAllAttachments(state.SubtitleStream.Path, state.MediaSource, cancellationTokenSource.Token).ConfigureAwait(false);
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
                StandardErrorEncoding = Encoding.UTF8,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                FileName = _mediaEncoder.EncoderPath,
                Arguments = commandLineArguments,
                WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? string.Empty : workingDirectory,
                ErrorDialog = false
            },
            EnableRaisingEvents = true
        };

        TranscodingPipelineInfo? pipeline = null;
        try
        {
            pipeline = TranscodingPipelineBuilder.Build(state, commandLineArguments);
        }
        catch (Exception ex)
        {
            // The pipeline description is purely informational, never fail the transcode for it.
            _logger.LogDebug(ex, "Failed to build transcoding pipeline description");
        }

        // Built before the job is registered: OnTranscodeBeginning reports the first progress
        // update, and that report derives the reported hardware acceleration from the pipeline.
        var transcodingJob = OnTranscodeBeginning(
            outputPath,
            state.Request.PlaySessionId,
            state.MediaSource.LiveStreamId,
            Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
            transcodingJobType,
            process,
            state.Request.DeviceId,
            state,
            cancellationTokenSource,
            pipeline);

        _logger.LogInformation("{Filename} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);

        var logFilePrefix = "FFmpeg.Transcode-";
        if (state.VideoRequest is not null
            && EncodingHelper.IsCopyCodec(state.OutputVideoCodec))
        {
            logFilePrefix = EncodingHelper.IsCopyCodec(state.OutputAudioCodec)
                ? "FFmpeg.Remux-"
                : "FFmpeg.DirectStream-";
        }

        if (state.VideoRequest is null && EncodingHelper.IsCopyCodec(state.OutputAudioCodec))
        {
            logFilePrefix = "FFmpeg.Remux-";
        }

        var logFilePath = Path.Combine(
            _serverConfigurationManager.ApplicationPaths.LogDirectoryPath,
            $"{logFilePrefix}{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{state.Request.MediaSourceId}_{Guid.NewGuid().ToString()[..8]}.log");

        // FFmpeg writes debug/error info to stderr. This is useful when debugging so let's put it in the log directory.
        Stream logStream = new FileStream(
            logFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            IODefaults.FileStreamBufferSize,
            FileOptions.Asynchronous);

        await JsonSerializer.SerializeAsync(logStream, state.MediaSource, cancellationToken: cancellationTokenSource.Token).ConfigureAwait(false);
        var commandLineLogMessageBytes = Encoding.UTF8.GetBytes(
            Environment.NewLine
            + Environment.NewLine
            + process.StartInfo.FileName + " " + process.StartInfo.Arguments
            + Environment.NewLine
            + Environment.NewLine);

        await logStream.WriteAsync(commandLineLogMessageBytes, cancellationTokenSource.Token).ConfigureAwait(false);

        process.Exited += (_, _) => OnFfMpegProcessExited(process, transcodingJob, state);

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting FFmpeg");
            OnTranscodeFailedToStart(outputPath, transcodingJobType, state);

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
            StartSegmentCleaner(state, transcodingJob);

            // ffmpeg 8.0+ can dump the negotiated filter graph, but only on exit - so run a short
            // throwaway "-t 0" probe alongside the transcode to capture the real pipeline. Only a
            // video encode has a filter graph worth dumping; a remux or an audio-only transcode
            // would pay for a second ffmpeg and learn nothing.
            var hasVideoEncode = transcodingJob.Pipeline?.Stages?
                .Any(s => s.Type == TranscodeStageType.Encode && string.Equals(s.MediaType, "Video", StringComparison.Ordinal)) == true;

            if (_mediaEncoder.EncoderVersion >= _minFFmpegPrintGraphs && hasVideoEncode)
            {
                var playSessionId = transcodingJob.PlaySessionId;
                if (!string.IsNullOrEmpty(playSessionId) && _pipelineGraphCache.TryGetValue(playSessionId, out var cachedGraph))
                {
                    transcodingJob.Pipeline = cachedGraph;
                }
                else
                {
                    _ = RunGraphProbeAsync(state, transcodingJob, commandLineArguments, outputPath, cancellationTokenSource.Token);
                }
            }
        }
        else if (transcodingJob.ExitCode != 0)
        {
            throw new FfmpegException(string.Format(CultureInfo.InvariantCulture, "FFmpeg exited with code {0}", transcodingJob.ExitCode));
        }

        _logger.LogDebug("StartFfMpeg() finished successfully");

        return transcodingJob;
    }

    private async Task RunGraphProbeAsync(StreamState state, TranscodingJob job, string commandLineArguments, string outputPath, CancellationToken cancellationToken)
    {
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrEmpty(outputDirectory))
        {
            return;
        }

        var probeDirectory = Path.Combine(outputDirectory, "graphprobe-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        var graphFilePath = Path.Combine(probeDirectory, "graph.json");
        var probeStarted = false;

        try
        {
            var probeArguments = TranscodingPipelineBuilder.BuildGraphProbeArguments(commandLineArguments, outputPath, probeDirectory, graphFilePath);
            if (probeArguments is null)
            {
                return;
            }

            // Queue behind any probe already running. If the transcode is torn down while waiting
            // (the user seeked or stopped), the token drops this probe instead of starting it.
            await _graphProbeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            probeStarted = true;

            Directory.CreateDirectory(probeDirectory);

            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    FileName = _mediaEncoder.EncoderPath,
                    Arguments = probeArguments,
                    ErrorDialog = false
                }
            })
            {
                process.Start();

                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(GraphProbeTimeout);

                // Drain stderr so the process can't block on a full pipe.
                var drainStdErr = process.StandardError.ReadToEndAsync(CancellationToken.None);

                try
                {
                    await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Disposing a Process does not terminate it - without this the probe would
                    // outlive the server's interest in it and keep a hardware session open.
                    _logger.LogDebug("FFmpeg filter graph probe timed out, killing it");
                    process.Kill(true);
                    throw;
                }
                finally
                {
                    await drainStdErr.ConfigureAwait(false);
                }
            }

            if (!File.Exists(graphFilePath))
            {
                return;
            }

            var graphJson = await File.ReadAllTextAsync(graphFilePath, cancellationToken).ConfigureAwait(false);
            var enriched = TranscodingPipelineBuilder.Build(state, commandLineArguments, graphJson);
            if (enriched is null)
            {
                return;
            }

            job.Pipeline = enriched;

            // Cache for the play session so subsequent transcodes (seeks/segments) reuse it. The
            // active job list is checked under its own lock because the cleanup that drops this key
            // runs there: without the check a probe finishing after the session ended would put
            // back an entry that nothing removes again.
            var playSessionId = job.PlaySessionId;
            if (!string.IsNullOrEmpty(playSessionId))
            {
                lock (_activeTranscodingJobs)
                {
                    if (_activeTranscodingJobs.Any(j => string.Equals(j.PlaySessionId, playSessionId, StringComparison.OrdinalIgnoreCase)))
                    {
                        _pipelineGraphCache[playSessionId] = enriched;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("FFmpeg filter graph probe cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "FFmpeg filter graph probe failed");
        }
        finally
        {
            if (probeStarted)
            {
                _graphProbeLock.Release();
            }

            try
            {
                if (Directory.Exists(probeDirectory))
                {
                    Directory.Delete(probeDirectory, true);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogDebug(ex, "Failed to remove filter graph probe directory {Path}", probeDirectory);
            }
        }
    }

    private void StartThrottler(StreamState state, TranscodingJob transcodingJob)
    {
        if (EnableThrottling(state)
            && (_mediaEncoder.IsPkeyPauseSupported
                || _mediaEncoder.EncoderVersion <= _maxFFmpegCkeyPauseSupported))
        {
            transcodingJob.TranscodingThrottler = new TranscodingThrottler(transcodingJob, _loggerFactory.CreateLogger<TranscodingThrottler>(), _serverConfigurationManager, _fileSystem, _mediaEncoder);
            transcodingJob.TranscodingThrottler.Start();
        }
    }

    private static bool EnableThrottling(StreamState state)
        => state.InputProtocol == MediaProtocol.File
           && state.RunTimeTicks.HasValue
           && state.RunTimeTicks.Value >= TimeSpan.FromMinutes(5).Ticks
           && state.IsInputVideo
           && state.VideoType == VideoType.VideoFile;

    private void StartSegmentCleaner(StreamState state, TranscodingJob transcodingJob)
    {
        if (EnableSegmentCleaning(state))
        {
            transcodingJob.TranscodingSegmentCleaner = new TranscodingSegmentCleaner(transcodingJob, _loggerFactory.CreateLogger<TranscodingSegmentCleaner>(), _serverConfigurationManager, _fileSystem, _mediaEncoder, state.SegmentLength);
            transcodingJob.TranscodingSegmentCleaner.Start();
        }
    }

    private static bool EnableSegmentCleaning(StreamState state)
        => state.InputProtocol is MediaProtocol.File or MediaProtocol.Http
           && state.IsInputVideo
           && state.TranscodingType == TranscodingJobType.Hls
           && state.RunTimeTicks.HasValue
           && state.RunTimeTicks.Value >= TimeSpan.FromMinutes(5).Ticks;

    private TranscodingJob OnTranscodeBeginning(
        string path,
        string? playSessionId,
        string? liveStreamId,
        string transcodingJobId,
        TranscodingJobType type,
        Process process,
        string? deviceId,
        StreamState state,
        CancellationTokenSource cancellationTokenSource,
        TranscodingPipelineInfo? pipeline)
    {
        lock (_activeTranscodingJobs)
        {
            var job = new TranscodingJob(_loggerFactory.CreateLogger<TranscodingJob>())
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
                MediaSource = state.MediaSource,
                Pipeline = pipeline
            };

            _activeTranscodingJobs.Add(job);

            ReportTranscodingProgress(job, state, null, null, null, null, null, null);

            return job;
        }
    }

    /// <inheritdoc />
    public void OnTranscodeEndRequest(TranscodingJob job)
    {
        var activeRequestCount = job.DecrementActiveRequestCount();
        _logger.LogDebug("OnTranscodeEndRequest job.ActiveRequestCount={ActiveRequestCount}", activeRequestCount);
        if (activeRequestCount <= 0)
        {
            PingTimer(job, false);
        }
    }

    private void OnTranscodeFailedToStart(string path, TranscodingJobType type, StreamState state)
    {
        lock (_activeTranscodingJobs)
        {
            var job = _activeTranscodingJobs.FirstOrDefault(j => j.Type == type && string.Equals(j.Path, path, StringComparison.OrdinalIgnoreCase));

            if (job is not null)
            {
                _activeTranscodingJobs.Remove(job);
            }
        }

        if (!string.IsNullOrWhiteSpace(state.Request.DeviceId))
        {
            _sessionManager.ClearTranscodingInfo(state.Request.DeviceId);
        }
    }

    private void OnFfMpegProcessExited(Process process, TranscodingJob job, StreamState state)
    {
        job.HasExited = true;
        job.ExitCode = process.ExitCode;

        ReportTranscodingProgress(job, state, null, null, null, null, null, null);

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
                _encodingHelper.TryStreamCopy(state, encodingOptions);
            }
        }

        if (state.MediaSource.BufferMs.HasValue)
        {
            await Task.Delay(state.MediaSource.BufferMs.Value, cancellationTokenSource.Token).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public TranscodingJob? OnTranscodeBeginRequest(string path, TranscodingJobType type)
    {
        lock (_activeTranscodingJobs)
        {
            var job = _activeTranscodingJobs
                .FirstOrDefault(j => j.Type == type && string.Equals(j.Path, path, StringComparison.OrdinalIgnoreCase));

            if (job is null)
            {
                return null;
            }

            job.IncrementActiveRequestCount();
            if (string.IsNullOrWhiteSpace(job.PlaySessionId) || job.Type == TranscodingJobType.Progressive)
            {
                job.StopKillTimer();
            }

            return job;
        }
    }

    private void OnPlaybackProgress(object? sender, PlaybackProgressEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.PlaySessionId))
        {
            PingTranscodingJob(e.PlaySessionId, e.IsPaused);
        }
    }

    private void DeleteEncodedMediaCache()
    {
        var path = _serverConfigurationManager.GetTranscodePath();
        if (!Directory.Exists(path))
        {
            return;
        }

        foreach (var file in _fileSystem.GetFilePaths(path, true))
        {
            try
            {
                _fileSystem.DeleteFile(file);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting encoded media cache file {Path}", path);
            }
        }
    }

    /// <summary>
    /// Transcoding lock.
    /// </summary>
    /// <param name="outputPath">The output path of the transcoded file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An <see cref="IDisposable"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<IDisposable> LockAsync(string outputPath, CancellationToken cancellationToken)
    {
        return _transcodingLocks.LockAsync(outputPath, cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _sessionManager.PlaybackProgress -= OnPlaybackProgress;
        _sessionManager.PlaybackStart -= OnPlaybackProgress;
        _transcodingLocks.Dispose();
        _graphProbeLock.Dispose();
    }

    /// <summary>
    /// A measured transcode output size and when it was taken.
    /// </summary>
    private readonly record struct TranscodedSize(long? Bytes, DateTime MeasuredAt);
}
