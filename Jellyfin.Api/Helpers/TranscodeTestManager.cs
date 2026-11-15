using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.TranscodeTestDtos;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Runs the dashboard's transcoding tests: a compatibility check of hardware decoding, encoding and tone mapping with
/// generated clips, and a stress test that adds streams of the playback HLS command until one falls behind real time.
/// </summary>
public sealed class TranscodeTestManager : IDisposable
{
    /// <summary>
    /// Each stream must keep this fraction of the source frame rate: real time. A stream below it falls further behind
    /// the longer it plays, until the client runs out of buffer. Short dips are allowed by <see cref="FailSeconds"/>.
    /// </summary>
    internal const double FpsRatio = 1.0;

    /// <summary>
    /// Seconds every stream must keep up before another is added.
    /// </summary>
    internal const double RampSeconds = 12;

    /// <summary>
    /// Seconds below the threshold that end the stress test.
    /// </summary>
    internal const double FailSeconds = 5;

    /// <summary>
    /// Seconds a test process may take to start, a stress test stream to report its first frame, or a clip probe to
    /// complete.
    /// </summary>
    internal const double StartTimeoutSeconds = 30;

    /// <summary>
    /// Seconds a source should last at least. Each stream restarts at the end of the file, which briefly disturbs the
    /// measurement.
    /// </summary>
    internal const double ShortSourceSeconds = 60;

    /// <summary>
    /// Arguments put before the stress test's playback command. The level tag tells errors from warnings, and without
    /// the banner a stalled stream's last lines are ffmpeg's own output, not its build configuration.
    /// </summary>
    internal const string StressLogArguments = "-hide_banner -loglevel +level ";

    private const string FolderName = "transcode-test";
    private const int CheckFrames = 96;
    private const int MaxLogLines = 500;

    /// <summary>
    /// How long to wait for an exited stream's last output before judging it.
    /// </summary>
    private static readonly TimeSpan _outputDrainTimeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// How long shutdown waits, in all, for the stress test's ffmpeg processes to exit after they were killed.
    /// </summary>
    private static readonly TimeSpan _shutdownWaitTimeout = TimeSpan.FromSeconds(5);

    private static readonly string[] _allDecodeCodecs = ["h264", "hevc", "mpeg2video", "mpeg4", "vc1", "vp8", "vp9", "av1"];

    private static readonly string[] _encodeCodecs = ["h264", "hevc", "av1"];

    private static readonly ClipSpec[] _clips =
    [
        new("h264", 8, false, "h264.mkv", "libx264", "yuv420p", string.Empty),
        new("hevc", 8, false, "hevc8.mkv", "libx265", "yuv420p", "-x265-params log-level=none"),
        new("hevc", 10, false, "hevc10.mkv", "libx265", "yuv420p10le", "-x265-params log-level=none"),
        new("vp8", 8, false, "vp8.webm", "libvpx", "yuv420p", "-deadline realtime -cpu-used 8 -b:v 4M"),
        new("vp9", 8, false, "vp9.webm", "libvpx-vp9", "yuv420p", "-deadline realtime -cpu-used 8 -b:v 4M"),
        new("av1", 10, false, "av1.mkv", "libsvtav1", "yuv420p10le", "-preset 10"),
        new("hevc", 10, true, "hevc10-hdr10.mkv", "libx265", "yuv420p10le", "-x265-params log-level=none:hdr10=1"),
    ];

    private readonly object _lock = new();
    private readonly IServerConfigurationManager _configurationManager;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly EncodingHelper _encodingHelper;
    private readonly HlsCommandBuilder _hlsCommandBuilder;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly ITranscodeManager _transcodeManager;
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<TranscodeTestManager> _logger;
    private readonly TranscodeTestProcessOwner _compatibilityProcesses = new();

    private TranscodeTestState _state = TranscodeTestState.Idle;
    private TranscodeTestKind _kind;
    private TranscodeTestRequestDto? _request;
    private DateTime? _startedAt;
    private DateTime? _endedAt;
    private List<TranscodeTestLogLine> _log = [];
    private List<TranscodeTestCheckResult> _checks = [];
    private TranscodeTestStressRun? _stress;
    private CancellationTokenSource? _cancellation;
    private Stopwatch _clock = new();
    private double _lastCheckedAt;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscodeTestManager"/> class.
    /// </summary>
    /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="encodingHelper">Instance of <see cref="EncodingHelper"/>.</param>
    /// <param name="hlsCommandBuilder">Instance of <see cref="HlsCommandBuilder"/>.</param>
    /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="transcodeManager">Instance of the <see cref="ITranscodeManager"/> interface.</param>
    /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{TranscodeTestManager}"/> interface.</param>
    public TranscodeTestManager(
        IServerConfigurationManager configurationManager,
        IMediaEncoder mediaEncoder,
        EncodingHelper encodingHelper,
        HlsCommandBuilder hlsCommandBuilder,
        IMediaSourceManager mediaSourceManager,
        IUserManager userManager,
        ILibraryManager libraryManager,
        ITranscodeManager transcodeManager,
        ISessionManager sessionManager,
        ILogger<TranscodeTestManager> logger)
    {
        _configurationManager = configurationManager;
        _mediaEncoder = mediaEncoder;
        _encodingHelper = encodingHelper;
        _hlsCommandBuilder = hlsCommandBuilder;
        _mediaSourceManager = mediaSourceManager;
        _userManager = userManager;
        _libraryManager = libraryManager;
        _transcodeManager = transcodeManager;
        _sessionManager = sessionManager;
        _logger = logger;
    }

    /// <summary>
    /// Starts a test.
    /// </summary>
    /// <param name="request">What to test.</param>
    /// <returns><c>false</c> if a test is already running.</returns>
    public bool TryStart(TranscodeTestRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        CancellationToken token;
        lock (_lock)
        {
            if (_state == TranscodeTestState.Running || _disposed)
            {
                return false;
            }

            _cancellation?.Dispose();
            _cancellation = new CancellationTokenSource();
            token = _cancellation.Token;
            _state = TranscodeTestState.Running;
            _kind = request.Kind;
            _request = request;
            _startedAt = DateTime.UtcNow;
            _endedAt = null;
            _log = [];
            _checks = [];
            _stress = request.Kind == TranscodeTestKind.StressTest ? new TranscodeTestStressRun(request.MaxStreams) : null;
            _clock = Stopwatch.StartNew();
            _lastCheckedAt = 0;
        }

        _ = Task.Run(() => RunAsync(request, token), CancellationToken.None);
        return true;
    }

    /// <summary>
    /// Stops the running test, or clears the results of the last one.
    /// </summary>
    public void Cancel()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            if (_state == TranscodeTestState.Running)
            {
                _cancellation?.Cancel();
                return;
            }

            _state = TranscodeTestState.Idle;
            _startedAt = null;
            _endedAt = null;
            _log = [];
            _checks = [];
            _stress = null;
        }
    }

    /// <summary>
    /// Gets the state and results so far.
    /// </summary>
    /// <returns>A snapshot of the test.</returns>
    public TranscodeTestStatusDto GetStatus()
    {
        lock (_lock)
        {
            // A stress test nobody checks on stops after a while, see TranscodeTestRules.IsAbandoned.
            _lastCheckedAt = _clock.Elapsed.TotalSeconds;
            return new TranscodeTestStatusDto
            {
                State = _state,
                Kind = _kind,
                Request = _request,
                StartedAt = _startedAt,
                EndedAt = _endedAt,
                Log = _log.ToArray(),
                Checks = _checks.ToArray(),
                Stress = _stress?.Snapshot(_clock.Elapsed.TotalSeconds)
            };
        }
    }

    /// <summary>
    /// Stops a running test at once: the server is shutting down and will not wait for the test to end, so the
    /// test's ffmpeg processes are killed here, waited for briefly, and stress-test files removed.
    /// </summary>
    public void Dispose()
    {
        TranscodeTestStressRun? stress;
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            stress = _state == TranscodeTestState.Running ? _stress : null;
            _cancellation?.Cancel();
        }

        if (stress is not null)
        {
            StopNow(stress, _shutdownWaitTimeout, _logger);
        }

        _compatibilityProcesses.Dispose();

        // The test's task only reads the token, which stays cancelled after the source is released.
        _cancellation?.Dispose();
    }

    /// <summary>
    /// Kills every stream of a stress test, waits for them to exit up to a total time, releases them and removes
    /// their files. Safe to call while the test's own task stops them too.
    /// </summary>
    /// <param name="run">The stress test.</param>
    /// <param name="wait">How long to wait, in all, for the processes to exit.</param>
    /// <param name="logger">Where to report files that could not be removed.</param>
    internal static void StopNow(TranscodeTestStressRun run, TimeSpan wait, ILogger logger)
    {
        var streams = run.Stop();
        foreach (var s in streams)
        {
            s.Kill();
        }

        var deadline = Stopwatch.StartNew();
        foreach (var s in streams)
        {
            var left = wait - deadline.Elapsed;
            if (left > TimeSpan.Zero)
            {
                s.WaitForExit(left);
            }
        }

        foreach (var s in streams)
        {
            s.DisposeProcess();
        }

        DeleteTestFiles(run.Folder, run.FilePrefix, logger);
    }

    private async Task RunAsync(TranscodeTestRequestDto request, CancellationToken cancellationToken)
    {
        var state = TranscodeTestState.Completed;
        try
        {
            var options = _configurationManager.GetEncodingOptions();
            Log($"ffmpeg {_mediaEncoder.EncoderVersion} · hardware acceleration: {options.HardwareAccelerationType}");
            var vaapiIdentified = _mediaEncoder.IsVaapiDeviceAmd || _mediaEncoder.IsVaapiDeviceInteliHD || _mediaEncoder.IsVaapiDeviceInteli965;
            if (TranscodeTestRules.NeedsVaapiRestart(options.HardwareAccelerationType, OperatingSystem.IsLinux(), vaapiIdentified))
            {
                Log("Note: Jellyfin has not identified the VAAPI device. It only does that at startup, so if you changed the hardware acceleration type or device, restart Jellyfin and run the test again.");
            }

            var busy = _sessionManager.Sessions.Count(s => s.TranscodingInfo is not null);
            if (busy > 0)
            {
                Log($"Warning: {busy} other transcode(s) are running; results will be lower than the server's full capacity.");
            }

            if (request.Kind == TranscodeTestKind.Compatibility)
            {
                await RunCompatibilityAsync(options, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await RunStressTestAsync(request, options, cancellationToken).ConfigureAwait(false);
            }

            Log("Done.");
        }
        catch (OperationCanceledException)
        {
            state = TranscodeTestState.Cancelled;
            _stress?.End(TranscodeTestEndReason.Cancelled, _clock.Elapsed.TotalSeconds);
            Log("Stopped.");
        }
        catch (Exception ex)
        {
            state = TranscodeTestState.Failed;
            _logger.LogError(ex, "Transcoding test failed");
            _stress?.End(TranscodeTestEndReason.Error, _clock.Elapsed.TotalSeconds, error: ex.Message);
            Log("The test failed: " + ex.Message);
        }

        lock (_lock)
        {
            _state = state;
            _endedAt = DateTime.UtcNow;
        }
    }

    // ---- compatibility check -----------------------------------------------------------------

    private async Task RunCompatibilityAsync(EncodingOptions saved, CancellationToken cancellationToken)
    {
        if (saved.HardwareAccelerationType == HardwareAccelerationType.none)
        {
            Log("Hardware acceleration is off, so there is no hardware to check. Choose an acceleration type and save first.");
            return;
        }

        Log("Creating test clips (once; they are kept in the cache folder) …");
        var clips = await GetClipsAsync(
            _clips,
            GetClipAsync,
            (clip, error, notCreated) => AddCheck(new TranscodeTestCheckResult
            {
                Kind = clip.Hdr ? TranscodeTestCheckKind.Tonemap : TranscodeTestCheckKind.Decode,
                Codec = clip.Codec,
                BitDepth = clip.BitDepth,
                Hdr = clip.Hdr,
                Outcome = TranscodeTestOutcome.Skipped,
                // The encoder names what could not create the clip. A clip that exists but could not be checked has none.
                Method = notCreated ? clip.Encoder : null,
                Error = error
            }),
            cancellationToken).ConfigureAwait(false);

        Log("Checking hardware decoding …");
        foreach (var (clip, source) in clips.Where(c => !c.Clip.Hdr))
        {
            await CheckDecodeAsync(clip, source, saved, cancellationToken).ConfigureAwait(false);
        }

        var h264 = clips.FirstOrDefault(c => c.Clip.FileName == "h264.mkv").Source;
        if (h264 is not null)
        {
            Log("Checking hardware encoding …");
            foreach (var codec in _encodeCodecs)
            {
                await CheckEncodeAsync(codec, h264, saved, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            foreach (var codec in _encodeCodecs)
            {
                AddCheck(new TranscodeTestCheckResult
                {
                    Kind = TranscodeTestCheckKind.Encode,
                    Codec = codec,
                    BitDepth = 8,
                    Outcome = TranscodeTestOutcome.Skipped,
                    Error = "The encoders are checked with the h264.mkv test clip, which is not available."
                });
            }
        }

        var hdr = clips.FirstOrDefault(c => c.Clip.Hdr).Source;
        if (hdr is not null)
        {
            Log("Checking HDR to SDR tone mapping …");
            await CheckTonemapAsync(hdr, saved, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets every test clip in turn. A clip that could not be created, or whose cache entry could not be verified or
    /// probed in time, becomes a skipped row and the other clips are still checked, so one clip never ends the whole
    /// check. A cache entry that could not be verified is kept: that failure says nothing about the clip.
    /// </summary>
    /// <typeparam name="T">The clip description.</typeparam>
    /// <param name="clips">The clips to get.</param>
    /// <param name="getClip">Gets one clip, or null and why ffmpeg could not create it; throws
    /// <see cref="IOException"/> when its cache entry could not be verified.</param>
    /// <param name="skip">Records a clip that is not available, with the reason and whether it could not be created.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The clips that are available, in order.</returns>
    internal static async Task<List<(T Clip, MediaSourceInfo Source)>> GetClipsAsync<T>(
        IEnumerable<T> clips,
        Func<T, CancellationToken, Task<(MediaSourceInfo? Source, string? Error)>> getClip,
        Action<T, string?, bool> skip,
        CancellationToken cancellationToken)
    {
        var available = new List<(T Clip, MediaSourceInfo Source)>();
        foreach (var clip in clips)
        {
            cancellationToken.ThrowIfCancellationRequested();
            MediaSourceInfo? source;
            string? error;
            try
            {
                (source, error) = await getClip(clip, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                skip(clip, ex.Message, false);
                continue;
            }

            if (source is null)
            {
                skip(clip, error, true);
                continue;
            }

            available.Add((clip, source));
        }

        return available;
    }

    /// <summary>
    /// Gets a test clip, creating it the first time.
    /// </summary>
    /// <returns>The clip, or null and how ffmpeg failed to create it.</returns>
    private async Task<(MediaSourceInfo? Source, string? Error)> GetClipAsync(ClipSpec clip, CancellationToken cancellationToken)
    {
        var folder = Path.Combine(_configurationManager.ApplicationPaths.CachePath, FolderName);
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, clip.FileName);
        var (info, error) = await TranscodeTestClipCache.GetAsync(
            path,
            (newTemporary, token) => TranscodeTestClipCache.GenerateAsync(
                newTemporary,
                async (temporary, small, attemptToken) =>
                {
                    // Encoders are not in Jellyfin's capability list, so just try: a missing one makes ffmpeg fail.
                    var result = await RunFfmpegAsync(TranscodeTestRules.ClipArguments(clip.Encoder, clip.PixelFormat, clip.ExtraArgs, clip.Hdr, temporary, small), null, null, attemptToken, temporary).ConfigureAwait(false);
                    if (result.ExitCode == 0 && File.Exists(temporary) && new FileInfo(temporary).Length > 0)
                    {
                        return (true, true, string.Empty);
                    }

                    var failure = $"{TranscodeTestRules.DescribeExit(result.ExitCode)}: {DescribeErrors(result)}";
                    if (small)
                    {
                        Log($"  Could not create the {clip.FileName} test clip at 640x360 either ({failure})");
                    }
                    else if (!result.NotStarted)
                    {
                        Log($"  Could not create the {clip.FileName} test clip ({clip.Encoder}, {TranscodeTestRules.DescribeExit(result.ExitCode)}): {DescribeErrors(result)}. Trying again at 640x360, which needs far less memory …");
                    }

                    return (false, !result.NotStarted, result.NotStarted ? DescribeErrors(result) : failure);
                },
                token),
            async (candidate, token) =>
            {
                try
                {
                    using var probeTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                    probeTimeout.CancelAfter(TimeSpan.FromSeconds(StartTimeoutSeconds));
                    var probeToken = probeTimeout.Token;
                    // MediaEncoder starts ffprobe synchronously before its first await. Keep a blocked launch
                    // off this task; its cancelled token stops a process that starts later.
                    var probeTask = Task.Run(
                        () => _mediaEncoder.GetMediaInfo(
                        new MediaInfoRequest
                        {
                            MediaSource = new MediaSourceInfo { Path = candidate, Protocol = MediaProtocol.File, VideoType = VideoType.VideoFile, Id = clip.FileName },
                            MediaType = DlnaProfileType.Video
                        },
                        probeToken),
                        CancellationToken.None);
                    try
                    {
                        var probed = await probeTask.WaitAsync(probeToken).ConfigureAwait(false);
                        if (!TranscodeTestClipValidation.IsValid(probed, clip.Codec, clip.BitDepth, clip.Hdr))
                        {
                            return null;
                        }

                        // Header metadata is not proof that all 96 frames survived. Decode with no hardware
                        // acceleration before using or publishing this source; GPU test failures never evict it.
                        var verifyArgs = string.Create(
                            CultureInfo.InvariantCulture,
                            $"-hide_banner -v verbose -xerror -nostdin -hwaccel none -i \"{candidate}\" -map 0:v:0 -an -sn -frames:v {TranscodeTestClipValidation.ClipFrames} -f null -");
                        var verified = await RunFfmpegAsync(verifyArgs, TranscodeTestClipValidation.ClipFrames, null, token).ConfigureAwait(false);
                        var complete = TranscodeTestClipValidation.IsCompleteSoftwareDecode(verified.ExitCode, verified.FramesEncoded, verified.Output);
                        if (complete is null)
                        {
                            throw new IOException($"Could not verify test clip {clip.FileName}; its cache entry was preserved: {DescribeErrors(verified)}");
                        }

                        if (!complete.Value)
                        {
                            Log($"  Incomplete or corrupt test clip {clip.FileName}. It will not be kept in the cache.");
                            return null;
                        }

                        return probed;
                    }
                    catch (OperationCanceledException)
                    {
                        _ = ObserveCompatibilityOutputAsync(probeTask);
                        token.ThrowIfCancellationRequested();
                        throw new IOException($"Probing test clip {clip.FileName} timed out after {StartTimeoutSeconds:0} s; its cache entry was preserved.");
                    }
                }
                catch (FfmpegException ex)
                {
                    Log($"  Invalid test clip {clip.FileName}: {ex.Message}. It will not be kept in the cache.");
                    return null;
                }
            },
            cancellationToken).ConfigureAwait(false);
        if (info is null)
        {
            return (null, error);
        }

        info.Path = path;
        info.Protocol = MediaProtocol.File;
        info.VideoType = VideoType.VideoFile;
        info.Id = clip.FileName;
        return (info, null);
    }

    private async Task CheckDecodeAsync(ClipSpec clip, MediaSourceInfo source, EncodingOptions saved, CancellationToken cancellationToken)
    {
        var enabled = saved.HardwareDecodingCodecs.Contains(clip.Codec, StringComparison.OrdinalIgnoreCase)
            && !(clip.BitDepth == 10 && clip.Codec == "hevc" && !saved.EnableDecodingColorDepth10Hevc)
            && !(clip.BitDepth == 10 && clip.Codec == "vp9" && !saved.EnableDecodingColorDepth10Vp9);

        // Test what the hardware can do, not only what is switched on.
        var options = CloneOptions(saved);
        options.HardwareDecodingCodecs = _allDecodeCodecs;
        options.EnableDecodingColorDepth10Hevc = true;
        options.EnableDecodingColorDepth10Vp9 = true;

        var result = new TranscodeTestCheckResult
        {
            Kind = TranscodeTestCheckKind.Decode,
            Codec = clip.Codec,
            BitDepth = clip.BitDepth,
            EnabledInSettings = enabled
        };
        var job = CreateJob(source, options, "h264", 1280);
        var hasHardwareDecoder = TranscodeTestRules.UsesHardwareDecoder(_encodingHelper.GetInputArgument(job, options, null));
        var run = hasHardwareDecoder ? await RunJobAsync(job, options, cancellationToken).ConfigureAwait(false) : null;
        if (run is { CheckExitCode: not 0 } && options.EnableHardwareEncoding)
        {
            // The pipeline also encodes on the GPU. Retry with the CPU encoder, so a broken hardware encoder is not blamed on the decoder.
            Log($"Decoding {clip.Codec} failed. Trying again with the CPU encoder to see whether the decoder is the cause …");
            options.EnableHardwareEncoding = false;
            job = CreateJob(source, options, "h264", 1280);
            run = await RunJobAsync(job, options, cancellationToken).ConfigureAwait(false);
        }

        (result.Outcome, result.Advice) = TranscodeTestRules.Decode(hasHardwareDecoder, run?.CheckExitCode, run?.InputPixelFormat, enabled);
        result.Fps = run?.CheckExitCode == 0 ? run.Fps : null;
        result.Error = run?.CheckExitCode is null or 0 ? null : run.Errors;
        if (result.Outcome == TranscodeTestOutcome.Failed)
        {
            // Zero frames and a working reference do not prove missing format support without capability evidence.
            result.Outcome = TranscodeTestRules.FailedOutcome(ReferencePassed(TranscodeTestCheckKind.Decode), !options.EnableHardwareEncoding, run!.FramesDecoded, run.ResourceLine, run.Output);
            result.ReferencePassed = result.Outcome == TranscodeTestOutcome.NotSupported;
            result.Advice = result.Outcome == TranscodeTestOutcome.NotSupported && enabled ? TranscodeTestAdvice.TurnOff : TranscodeTestAdvice.None;
            NoteResourceFailure(result, run);
        }

        if (result.Outcome == TranscodeTestOutcome.Failed)
        {
            // When frames were decoded, a later step such as a filter or the encoder failed, not the decoder's start.
            result.FramesDecoded = run!.FramesDecoded;
            result.FailedAfterDecoding = TranscodeTestRules.FailedAfterDecoding(run.FramesDecoded, run.Errors);
        }

        AddCheck(result);
    }

    private async Task CheckEncodeAsync(string codec, MediaSourceInfo source, EncodingOptions saved, CancellationToken cancellationToken)
    {
        var result = new TranscodeTestCheckResult
        {
            Kind = TranscodeTestCheckKind.Encode,
            Codec = codec,
            BitDepth = 8,
            EnabledInSettings = TranscodeTestRules.IsEncodeEnabled(codec, saved)
        };

        var options = CloneOptions(saved);
        options.EnableHardwareEncoding = true;

        // Decode on the CPU, so a failure here is the encoder's and not the saved hardware decoder's.
        options.HardwareDecodingCodecs = [];
        var job = CreateJob(source, options, codec, 1280);
        var encoder = _encodingHelper.GetVideoEncoder(job, options);
        result.Method = encoder;
        var run = TranscodeTestRules.IsSoftwareEncoder(encoder) ? null : await RunJobAsync(job, options, cancellationToken).ConfigureAwait(false);
        (result.Outcome, result.Advice) = TranscodeTestRules.Encode(encoder, run?.CheckExitCode, saved.EnableHardwareEncoding, TranscodeTestRules.IsEncodeAllowed(codec, saved));
        result.Fps = run?.CheckExitCode == 0 ? run.Fps : null;
        result.Error = run?.CheckExitCode is null or 0 ? null : run.Errors;
        if (result.Outcome == TranscodeTestOutcome.Failed)
        {
            // Initialization can fail for reasons other than format support, even when H.264 passed.
            result.Outcome = TranscodeTestRules.FailedOutcome(ReferencePassed(TranscodeTestCheckKind.Encode), true, run!.FramesEncoded, run.ResourceLine, run.Output);
            result.ReferencePassed = result.Outcome == TranscodeTestOutcome.NotSupported;
            result.Advice = result.Outcome == TranscodeTestOutcome.NotSupported && result.EnabledInSettings == true ? TranscodeTestAdvice.TurnOff : TranscodeTestAdvice.None;
            NoteResourceFailure(result, run);
        }

        AddCheck(result);
    }

    /// <summary>
    /// Names the cause of a check that failed because the GPU refused a session or ran out of memory, which says
    /// nothing about the format, and warns when other transcodes may have taken them.
    /// </summary>
    private void NoteResourceFailure(TranscodeTestCheckResult result, FfmpegRun run)
    {
        if (run.ResourceLine is null)
        {
            return;
        }

        result.Error = TranscodeTestRules.PrependCause(run.ResourceLine, result.Error ?? string.Empty);
        var busy = _sessionManager.Sessions.Count(s => s.TranscodingInfo is not null);
        Log(busy > 0
            ? $"  The GPU refused an encoding session or ran out of memory, so this check says nothing about the format. {busy} other transcode(s) are running and may hold them; run the check again when the server is idle."
            : "  The GPU refused an encoding session or ran out of memory, so this check says nothing about the format.");
    }

    /// <summary>
    /// Gets whether the H.264 8-bit check of a kind passed earlier in this run, so the hardware and its setup work.
    /// </summary>
    private bool ReferencePassed(TranscodeTestCheckKind kind)
    {
        lock (_lock)
        {
            return _checks.Any(c => c.Kind == kind && c.Codec == "h264" && c.BitDepth == 8 && c.Outcome == TranscodeTestOutcome.Passed);
        }
    }

    private async Task CheckTonemapAsync(MediaSourceInfo source, EncodingOptions saved, CancellationToken cancellationToken)
    {
        var savedOn = TonemapFilter(source, saved) is not null;
        var options = CloneOptions(saved);
        options.HardwareDecodingCodecs = _allDecodeCodecs;
        options.EnableDecodingColorDepth10Hevc = true;
        options.EnableTonemapping = true;

        // Keep the saved VPP and VideoToolbox choices. Jellyfin's Intel pipelines prefer VPP over OpenCL when it is on,
        // and on macOS the VideoToolbox option replaces the Metal filter (tonemap_videotoolbox) with the tone mapping
        // built into scale_vt, so forcing either would test a tone mapping filter that playback does not use.
        var job = CreateJob(source, options, "h264", 1920);
        var filter = TonemapFilter(source, options);
        var result = new TranscodeTestCheckResult
        {
            Kind = TranscodeTestCheckKind.Tonemap,
            Codec = "hevc",
            BitDepth = 10,
            Hdr = true,
            EnabledInSettings = savedOn,
            Method = filter?.Split('=')[0]
        };
        var run = filter is null ? null : await RunJobAsync(job, options, cancellationToken).ConfigureAwait(false);
        (result.Outcome, result.Advice) = TranscodeTestRules.Tonemap(filter, run?.CheckExitCode, savedOn);
        result.Fps = run?.CheckExitCode == 0 ? run.Fps : null;
        result.Error = run?.CheckExitCode is null or 0 ? null : run.Errors;
        AddCheck(result);
    }

    private string? TonemapFilter(MediaSourceInfo source, EncodingOptions options)
    {
        var job = CreateJob(source, options, "h264", 1920);
        return TranscodeTestRules.FindTonemapFilter(_encodingHelper.GetVideoProcessingFilterParam(job, options, _encodingHelper.GetVideoEncoder(job, options)));
    }

    private EncodingJobInfo CreateJob(MediaSourceInfo source, EncodingOptions options, string outputCodec, int maxWidth)
    {
        var job = new EncodingJobInfo(TranscodingJobType.Progressive)
        {
            IsVideoRequest = true,
            IsInputVideo = true,
            BaseRequest = new BaseEncodingJobOptions { MaxWidth = maxWidth, VideoCodec = outputCodec, VideoBitRate = 4_000_000 },
            OutputVideoCodec = outputCodec,
            OutputVideoBitrate = 4_000_000
        };
        _encodingHelper.AttachMediaSourceInfo(job, options, source, string.Empty);
        job.VideoType = VideoType.VideoFile;
        return job;
    }

    private async Task<FfmpegRun> RunJobAsync(EncodingJobInfo job, EncodingOptions options, CancellationToken cancellationToken)
    {
        var encoder = _encodingHelper.GetVideoEncoder(job, options);
        var inputModifier = _encodingHelper.GetInputModifier(job, options, null);
        var input = _encodingHelper.GetInputArgument(job, options, null);
        var filters = _encodingHelper.GetVideoProcessingFilterParam(job, options, encoder);

        // The encoder options playback uses (preset, profile, level, bitrate, Intel low-power mode, i915 workaround),
        // so an encoder fails or passes here as it would during playback.
        var quality = _encodingHelper.GetVideoQualityParam(job, encoder, options, EncoderPreset.veryfast);
        var args = string.Create(
            CultureInfo.InvariantCulture,
            $"-hide_banner -v verbose {inputModifier} {input} -map 0:v:0 -an -sn -c:v {encoder}{quality}{filters} -frames:v {CheckFrames} -f null -");
        return await RunFfmpegAsync(args, CheckFrames, encoder, cancellationToken).ConfigureAwait(false);
    }

    private static EncodingOptions CloneOptions(EncodingOptions options)
        => JsonSerializer.Deserialize<EncodingOptions>(JsonSerializer.Serialize(options, JsonDefaults.Options), JsonDefaults.Options)
            ?? throw new InvalidOperationException("Could not copy the encoding options.");

    private async Task<FfmpegRun> RunFfmpegAsync(string args, int? frames, string? encoder, CancellationToken cancellationToken, string? temporaryOutput = null)
    {
        _logger.LogDebug("Transcoding test: {Ffmpeg} {Args}", _mediaEncoder.EncoderPath, args);
        var timer = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(60));
        using var process = await TranscodeTestLauncher.StartAsync(
            () =>
            {
                var started = new Process
                {
                    StartInfo = new ProcessStartInfo(_mediaEncoder.EncoderPath, args)
                    {
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                try
                {
                    started.Start();
                    if (!_compatibilityProcesses.TryAdd(started))
                    {
                        AbandonCompatibilityProcess(started, temporaryOutput);
                        throw new OperationCanceledException("The server stopped while ffmpeg was starting.", cancellationToken);
                    }

                    return started;
                }
                catch
                {
                    started.Dispose();
                    throw;
                }
            },
            TimeSpan.FromSeconds(StartTimeoutSeconds),
            late => AbandonCompatibilityProcess(late, temporaryOutput),
            cancellationToken).ConfigureAwait(false);
        if (process is null)
        {
            return new FfmpegRun(-1, null, null, "ffmpeg did not start within 30 s", null, null, null, NotStarted: true);
        }

        Task<string>? stderr = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            stderr = process.StandardError.ReadToEndAsync(CancellationToken.None);
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            var output = await stderr.WaitAsync(_outputDrainTimeout, timeout.Token).ConfigureAwait(false);
            var elapsed = timer.Elapsed.TotalSeconds;
            var pixelFormat = TranscodeTestRules.DecodedPixelFormat(output);
            var encodedFrames = encoder is null ? TranscodeTestRules.FramesEncoded(output) : TranscodeTestRules.EncodedFrames(output, encoder);
            var incomplete = frames.HasValue && encodedFrames is int actual && actual < frames.Value;
            var errors = TranscodeTestRules.CheckErrors(process.ExitCode, encodedFrames, frames, TranscodeTestRules.ErrorLines(output));

            return new FfmpegRun(
                process.ExitCode,
                frames.HasValue && encodedFrames.HasValue && elapsed > 0 && !incomplete ? Math.Round(encodedFrames.Value / elapsed, 1) : null,
                pixelFormat,
                errors,
                TranscodeTestRules.FramesDecoded(output),
                encodedFrames,
                TranscodeTestRules.ResourceLine(output),
                output,
                !incomplete);
        }
        catch (OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new FfmpegRun(-1, null, null, "timed out after 60 s", null, null, null);
        }
        catch (TimeoutException)
        {
            return new FfmpegRun(-1, null, null, "ffmpeg output did not drain within 2 s", null, null, null);
        }
        finally
        {
            TranscodeTestProcessOwner.Kill(process);
            TranscodeTestProcessOwner.WaitForExit(process, _shutdownWaitTimeout);
            _compatibilityProcesses.Remove(process);
            if (stderr is not null)
            {
                _ = ObserveCompatibilityOutputAsync(stderr);
            }
        }
    }

    private void AbandonCompatibilityProcess(Process process, string? temporaryOutput)
    {
        TranscodeTestProcessOwner.Kill(process);
        TranscodeTestProcessOwner.WaitForExit(process, _shutdownWaitTimeout);
        _compatibilityProcesses.Remove(process);
        process.Dispose();
        if (temporaryOutput is not null)
        {
            try
            {
                File.Delete(temporaryOutput);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Could not remove late compatibility output {Path}", temporaryOutput);
            }
        }
    }

    private static async Task ObserveCompatibilityOutputAsync(Task output)
        => await output.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

    // ---- stress test ------------------------------------------------------------------------

    private async Task RunStressTestAsync(TranscodeTestRequestDto request, EncodingOptions options, CancellationToken cancellationToken)
    {
        var run = _stress!;
        var item = _libraryManager.GetItemById(request.ItemId!.Value) ?? throw new ArgumentException("The item was not found.");
        var codec = request.VideoCodec is "hevc" or "av1" ? request.VideoCodec : "h264";
        // Like playback, write into the transcode folder itself: the startup cleanup task removes empty sub-folders.
        var folder = _configurationManager.GetTranscodePath();
        var prefix = FolderName + "-" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
        run.Folder = folder;
        run.FilePrefix = prefix;
        run.MaxOutputBytes = TranscodeTestRules.NeededSpace(request.MaxStreams, request.VideoBitrate, 6);
        var deadline = _clock.Elapsed.TotalSeconds + TranscodeTestRules.TimeLimitSeconds(request.MaxStreams);
        Log(string.Create(CultureInfo.InvariantCulture, $"Stress test: \"{item.Name}\" → {request.MaxWidth} px {codec} {request.VideoBitrate / 1e6:0.#} Mbps."));
        CheckFreeSpace(folder, request);
        var sourceSeconds = item.RunTimeTicks is long ticks ? TimeSpan.FromTicks(ticks).TotalSeconds : (double?)null;
        run.ShortSource = TranscodeTestRules.IsShortSource(sourceSeconds);
        if (TranscodeTestRules.DescribeShortSource(sourceSeconds) is string shortSource)
        {
            Log(shortSource);
        }

        try
        {
            using var firstCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var firstLaunch = StartStreamAsync(run, request, codec, folder, prefix, options, item.Name ?? string.Empty, firstCancellation.Token);
            TranscodeTestStream? first;
            try
            {
                while (!firstLaunch.IsCompleted)
                {
                    await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                    var now = _clock.Elapsed.TotalSeconds;
                    StopIfAbandoned(now);
                    run.Sample(now);
                    if (now >= deadline)
                    {
                        run.End(TranscodeTestRules.TimeLimitEndReason(0, request.MaxStreams), now, timeLimitReached: true);
                        return;
                    }
                }

                first = await firstLaunch.ConfigureAwait(false);
            }
            finally
            {
                await firstCancellation.CancelAsync().ConfigureAwait(false);
                await ObservePendingLaunchAsync(firstLaunch).ConfigureAwait(false);
            }

            if (first is null)
            {
                return;
            }

            run.SourceFps = first.SourceFps;
            run.ThresholdFps = Math.Round(first.SourceFps * FpsRatio, 3);
            Log(string.Create(CultureInfo.InvariantCulture, $"Each stream must keep {TranscodeTestRules.FormatFps(run.ThresholdFps)} fps ({FpsRatio * 100:0}% of the source's {first.SourceFps:0.###} fps). {TranscodeTestRules.DescribeRamp(RampSeconds, TranscodeTestStream.WindowSeconds)}"));
            await EscalateAsync(run, request, codec, folder, prefix, options, item.Name ?? string.Empty, deadline, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await StopAllAsync(run).ConfigureAwait(false);
            DeleteTestFiles(folder, prefix, _logger);
        }

        var summary = run.Snapshot(_clock.Elapsed.TotalSeconds);
        if (summary.SingleStreamFps is double single)
        {
            Log($"One stream alone ran at {single:0.0} fps ({summary.SingleStreamSpeed:0.00}× real time).");
        }

        Log(TranscodeTestRules.DescribeResult(summary));
    }

    private async Task EscalateAsync(TranscodeTestStressRun run, TranscodeTestRequestDto request, string codec, string folder, string prefix, EncodingOptions options, string itemName, double deadline, CancellationToken cancellationToken)
    {
        var timeLimit = TranscodeTestRules.TimeLimitSeconds(request.MaxStreams);
        var controller = new TranscodeTestStressController(run.ThresholdFps, RampSeconds, FailSeconds, StartTimeoutSeconds, request.MaxStreams, 0, TranscodeTestStream.WindowSeconds, deadline);
        controller.StreamAdded(_clock.Elapsed.TotalSeconds);
        using var launches = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task<TranscodeTestStream?>? pending = null;
        var pendingRestart = false;
        var waitingForRestart = new Queue<TranscodeTestStream>();
        var pendingIds = new HashSet<int>();
        var exitedAt = new Dictionary<TranscodeTestStream, double>();
        run.SetPhase(TranscodeTestStressPhase.WarmingUp, _clock.Elapsed.TotalSeconds + RampSeconds);
        try
        {
            while (true)
            {
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                var now = _clock.Elapsed.TotalSeconds;
                StopIfAbandoned(now);
                run.Sample(now);
                if (now >= deadline)
                {
                    Log(TranscodeTestRules.DescribeTimeLimit(timeLimit, controller.MaxStable));
                    run.End(TranscodeTestRules.TimeLimitEndReason(controller.MaxStable, request.MaxStreams), now, timeLimitReached: true);
                    return;
                }

                if (pending?.IsCompleted == true)
                {
                    var started = await pending.ConfigureAwait(false);
                    pending = null;
                    if (started is null)
                    {
                        return;
                    }

                    pendingIds.Remove(started.Id);
                    if (!pendingRestart)
                    {
                        controller.StreamAdded(now);
                    }
                }

                foreach (var started in run.GetStreams().Where(s => !s.FirstFrameLogged))
                {
                    if (started.FirstFrameAt.HasValue && started.DescribeFirstFrame(TranscodeTestProcessIo.BytesRead(started.Process)) is string firstFrame)
                    {
                        started.FirstFrameLogged = true;
                        Log(firstFrame);
                    }
                }

                foreach (var ended in run.GetStreams().Where(s => s.HasExited && !s.Stopping && !s.Handled))
                {
                    pendingIds.Add(ended.Id);
                    // The process can exit before its last lines are read; they may name the cause.
                    if (!ended.OutputCompleted)
                    {
                        if (!exitedAt.TryGetValue(ended, out var observed))
                        {
                            exitedAt[ended] = now;
                            continue;
                        }

                        if (now - observed < _outputDrainTimeout.TotalSeconds)
                        {
                            continue;
                        }
                    }

                    ended.Handled = true;
                    exitedAt.Remove(ended);
                    var lived = now - ended.StartedAt;
                    if (TranscodeTestRules.CanRestart(ended.ExitCode, ended.ReportedFrames, lived, ended.AtEndOfFile, ended.Restarts))
                    {
                        // The file ended: start the stream again so the load stays the same.
                        Log($"Stream {ended.Id} reached the end of the file; starting it again.");
                        RetireStream(ended, _logger);
                        waitingForRestart.Enqueue(ended);
                        pendingIds.Add(ended.Id);
                        controller.StreamRestarted(ended.Id, ended.RunRate);
                        continue;
                    }

                    if (TranscodeTestRules.IsRestartLimit(ended.ExitCode, ended.ReportedFrames, ended.AtEndOfFile, ended.Restarts))
                    {
                        // A file this short ends the test, not the stream: the kept-up count is still the result.
                        Log(TranscodeTestRules.DescribeRestartLimit(ended.Id, ended.Restarts, controller.MaxStable));
                        run.End(TranscodeTestRules.TimeLimitEndReason(controller.MaxStable, request.MaxStreams), now);
                        return;
                    }

                    EndStream(run, ended, TranscodeTestEndKind.Exited, controller.MaxStable, now, TranscodeTestRules.DescribeNoRestart(ended.ExitCode, ended.ReportedFrames, lived, ended.PositionSeconds, ended.SourceSeconds, ended.Restarts));
                    return;
                }

                if (pending is null && waitingForRestart.TryDequeue(out var previous))
                {
                    pendingRestart = true;
                    pending = StartStreamAsync(run, request, codec, folder, prefix, options, itemName, launches.Token, previous);
                }

                var readings = run.GetStreams().Where(s => !s.HasExited).Select(s => new TranscodeTestStreamReading(s.Id, s.Fps(now), now - s.StartedAt, s.ReportedFrames, s.NearEndAt(now))).ToList();
                // A pending launch remains a logical stream, but no rate has yet been observed under the intended load.
                readings.AddRange(pendingIds.Where(id => readings.All(s => s.Id != id)).Select(id => new TranscodeTestStreamReading(id, null, 0, true)));
                var decision = controller.Tick(now, readings);
                run.MaxStable = controller.MaxStable;
                switch (decision.Action)
                {
                    case TranscodeTestStressAction.Stalled:
                        // The controller only judges running streams; Last picks the current one when a restart reused the id.
                        EndStream(run, run.GetStreams().Last(s => s.Id == decision.StreamId!.Value && !s.HasExited), TranscodeTestEndKind.Stalled, controller.MaxStable, now);
                        return;
                    case TranscodeTestStressAction.LimitReached:
                        Log($"With {TranscodeTestRules.StreamCount(readings.Count)} running, stream {decision.StreamId} fell to {TranscodeTestRules.FormatFps(TranscodeTestRules.RoundFps(decision.Fps!.Value, run.ThresholdFps))} fps, under {TranscodeTestRules.FormatFps(run.ThresholdFps)} fps, for {FailSeconds:0} s.");
                        run.End(TranscodeTestEndReason.LimitReached, now, decision.StreamId, decision.Fps);
                        return;
                    case TranscodeTestStressAction.MaxStreams:
                        Log($"All {request.MaxStreams} streams kept up; that is the limit you set.");
                        run.End(TranscodeTestEndReason.MaxStreams, now);
                        return;
                    case TranscodeTestStressAction.TimeLimit:
                        Log(TranscodeTestRules.DescribeTimeLimit(timeLimit, controller.MaxStable));
                        run.End(TranscodeTestRules.TimeLimitEndReason(controller.MaxStable, request.MaxStreams), now, timeLimitReached: true);
                        return;
                    case TranscodeTestStressAction.AddStream:
                        run.RecordSingleStream(now);
                        pendingRestart = false;
                        pending = StartStreamAsync(run, request, codec, folder, prefix, options, itemName, launches.Token);
                        pendingIds.Add(run.LastStreamId);
                        run.SetPhase(TranscodeTestStressPhase.WarmingUp, now + RampSeconds);
                        break;
                    default:
                        if (decision.Phase == TranscodeTestStressPhase.FallingBehind)
                        {
                            if (decision.DipStarted && decision.Fps is double dipFps)
                            {
                                // Once per dip: the next stream waits until all have kept up for the settle time.
                                Log(TranscodeTestRules.DescribeDip(decision.StreamId!.Value, dipFps, run.ThresholdFps, TranscodeTestStream.WindowSeconds, readings.Count < request.MaxStreams ? run.LastStreamId + 1 : null));
                            }

                            run.SetFallingBehind(decision.StreamId!.Value, decision.EndsAt!.Value);
                        }
                        else
                        {
                            run.SetPhase(decision.Phase, decision.NextStreamAt);
                        }

                        break;
                }
            }
        }
        finally
        {
            await launches.CancelAsync().ConfigureAwait(false);
            if (pending is not null)
            {
                await ObservePendingLaunchAsync(pending).ConfigureAwait(false);
            }
        }
    }

    private static async Task ObservePendingLaunchAsync(Task<TranscodeTestStream?> pending)
    {
        try
        {
            await pending.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // The supervisor reports a completed launch's failure; cancellation or an earlier terminal reason wins.
        }
    }

    private async Task<TranscodeTestStream?> StartStreamAsync(TranscodeTestStressRun run, TranscodeTestRequestDto request, string codec, string folder, string prefix, EncodingOptions options, string itemName, CancellationToken cancellationToken, TranscodeTestStream? previous = null)
    {
        var id = previous?.Id ?? run.NextStreamId();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(StartTimeoutSeconds));
        var launchToken = timeout.Token;
        var launchClock = Stopwatch.StartNew();
        try
        {
            var container = codec == "h264" ? "ts" : "mp4";
            var streamingRequest = new VideoRequestDto
            {
                Id = request.ItemId!.Value,
                MediaSourceId = request.MediaSourceId ?? request.ItemId.Value.ToString("N", CultureInfo.InvariantCulture),
                DeviceId = "transcode-test",
                PlaySessionId = "transcode-test-" + id.ToString(CultureInfo.InvariantCulture),
                VideoCodec = codec,
                AudioCodec = "aac",
                AudioBitRate = 192_000,
                MaxAudioChannels = 2,
                TranscodingMaxAudioChannels = 2,
                MaxWidth = request.MaxWidth,
                VideoBitRate = request.VideoBitrate,
                SegmentContainer = container,
                SegmentLength = 6
            };

            var stateTask = Task.Run(
                async () => await StreamingHelpers.GetStreamingState(
                        streamingRequest,
                        $"/Videos/{streamingRequest.Id:N}/hls1/main/0.{container}",
                        new Dictionary<string, string?>(),
                        "Jellyfin transcoding test",
                        Guid.Empty,
                        _mediaSourceManager,
                        _userManager,
                        _libraryManager,
                        _configurationManager,
                        _mediaEncoder,
                        _encodingHelper,
                        _transcodeManager,
                        TranscodingJobType.Hls,
                        launchToken)
                    .ConfigureAwait(false),
                launchToken);
            StreamState state;
            try
            {
                state = await stateTask.WaitAsync(launchToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _ = DisposeLateStreamingStateAsync(stateTask);
                throw;
            }

            using var stateLifetime = state;
            launchToken.ThrowIfCancellationRequested();
            var streamPrefix = string.Create(CultureInfo.InvariantCulture, $"{prefix}-s{id}-{Guid.NewGuid():N}_");
            var playlist = Path.Combine(folder, streamPrefix + ".m3u8");
            // Playback's command, plus flags that tag each log line with its level so errors can be told from warnings, and
            // hide the banner. They change only ffmpeg's log text, not the transcode.
            var args = StressLogArguments + _hlsCommandBuilder.GetCommandLineArguments(playlist, state, options, false, 0);
            _logger.LogDebug("Transcoding test stream {Id}: {Ffmpeg} {Args}", id, _mediaEncoder.EncoderPath, args);
            var preferredFps = state.VideoStream?.ReferenceFrameRate;
            if (preferredFps is null && state.VideoStream?.AverageFrameRate is not null)
            {
                // ReferenceFrameRate discarded an unusable average (for example 1000 fps or NaN).
                preferredFps = float.NaN;
            }

            var sourceFps = SourceFrameRate(preferredFps, state.VideoStream?.RealFrameRate);
            if (run.Input is null)
            {
                if (state.VideoStream?.AverageFrameRate is null && state.VideoStream?.RealFrameRate is null)
                {
                    Log("The source has no frame-rate metadata; the test uses 24 fps.");
                }

                DescribeStream(run, state, options, args, itemName, sourceFps);
            }

            var startInfo = new ProcessStartInfo(_mediaEncoder.EncoderPath, args)
            {
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            // Off the test's loop and with a time limit, so a launch that hangs does not stop the test from watching the
            // other streams, the time limit and the idle stop for good.
            var process = await TranscodeTestLauncher.StartAsync(
                    () => StartProcess(startInfo),
                    TimeSpan.FromSeconds(Math.Max(0, StartTimeoutSeconds - launchClock.Elapsed.TotalSeconds)),
                    late => AbandonProcess(late, id, folder, streamPrefix, _logger),
                    launchToken)
                .ConfigureAwait(false);
            if (process is null)
            {
                EndLaunch(run, id, previous is not null);
                return null;
            }

            var stream = new TranscodeTestStream(id, process, folder, streamPrefix, sourceFps, _clock.Elapsed.TotalSeconds, state.SegmentLength)
            {
                // A stream restarted at the end of the file is not news, as with its start below.
                FirstFrameLogged = previous is not null,
                SourceSeconds = state.RunTimeTicks is long ticks ? TimeSpan.FromTicks(ticks).TotalSeconds : null
            };
            if (previous is not null)
            {
                stream.CarryOver(previous);
                stream.Restarts = previous.Restarts + 1;
            }

            try
            {
                stream.StartReading(_clock);
                if (!run.Add(stream))
                {
                    // The server is shutting down and already stopped the other streams.
                    throw new OperationCanceledException(cancellationToken);
                }
            }
            catch
            {
                stream.Kill();
                stream.WaitForExit(TimeSpan.FromSeconds(1));
                stream.DisposeProcess();
                DeleteTestFiles(folder, streamPrefix, _logger);
                throw;
            }

            if (previous is null)
            {
                Log($"Stream {id} started; {run.GetStreams().Count(s => !s.HasExited)} running.");
            }

            return stream;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            EndLaunch(run, id, previous is not null);
            return null;
        }
    }

    /// <summary>
    /// Selects a finite positive source rate, falling back only when both metadata rates are absent.
    /// </summary>
    /// <param name="sourceFps">The preferred metadata rate.</param>
    /// <param name="alternativeFps">The other metadata rate.</param>
    /// <returns>The usable rate.</returns>
    internal static float SourceFrameRate(float? sourceFps, float? alternativeFps = null)
    {
        if (sourceFps is float fps && float.IsFinite(fps) && fps > 0)
        {
            return fps;
        }

        if (alternativeFps is float alternate && float.IsFinite(alternate) && alternate > 0)
        {
            return alternate;
        }

        if (sourceFps is null && alternativeFps is null)
        {
            return 24f;
        }

        throw new InvalidOperationException("The source has an invalid frame rate. Select a source with a known positive frame rate.");
    }

    /// <summary>
    /// Collects the final output statistics, releases an exited process and removes its entire owned prefix.
    /// </summary>
    /// <param name="stream">The exited stream.</param>
    /// <param name="logger">The cleanup logger.</param>
    internal static void RetireStream(TranscodeTestStream stream, ILogger logger)
    {
        stream.CollectSegments(true);
        stream.DisposeProcess(false);
        DeleteTestFiles(stream.Folder, stream.FilePrefix, logger);
    }

    private static async Task DisposeLateStreamingStateAsync(Task<StreamState> pending)
    {
        try
        {
            (await pending.ConfigureAwait(false)).Dispose();
        }
        catch (Exception)
        {
            // Observe a late setup error after its launch was cancelled.
        }
    }

    private static Process StartProcess(ProcessStartInfo startInfo)
    {
        var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
            return process;
        }
        catch
        {
            process.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Stops a process that started after the test stopped waiting for it, or after the test ended.
    /// </summary>
    /// <param name="process">The late process.</param>
    /// <param name="streamId">The logical stream number.</param>
    /// <param name="folder">The output folder.</param>
    /// <param name="prefix">The prefix exclusively owned by this launch.</param>
    /// <param name="logger">The cleanup logger.</param>
    internal static void AbandonProcess(Process process, int streamId, string folder, string prefix, ILogger logger)
    {
        logger.LogWarning("Transcoding test stream {Id} started too late; stopping it", streamId);
        try
        {
            process.Kill(true);
            process.WaitForExit(TimeSpan.FromSeconds(1));
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
        {
            // It exited meanwhile, or it cannot be stopped.
        }
        finally
        {
            process.Dispose();
            DeleteTestFiles(folder, prefix, logger);
        }
    }

    /// <summary>
    /// Ends a stress test because the system did not run a stream's ffmpeg within the start timeout. The stream has no
    /// process to judge: it did not start.
    /// </summary>
    private void EndLaunch(TranscodeTestStressRun run, int streamId, bool restart)
    {
        var now = _clock.Elapsed.TotalSeconds;
        var maxStable = run.MaxStable;
        var error = TranscodeTestRules.DescribeLaunchTimeout(StartTimeoutSeconds);
        Log($"Stream {streamId} could not start. {error}");
        // As in EndStream: a stream restarted at the end of the file played before, so it is not one more stream that
        // could not start.
        var couldNotStart = !restart && maxStable >= 1;
        if (couldNotStart)
        {
            Log(TranscodeTestRules.DescribeCouldNotStart(TranscodeTestEndKind.Exited, maxStable, streamId, StartTimeoutSeconds));
        }

        run.End(TranscodeTestEndReason.StreamFailed, now, streamId, error: error, couldNotStartStream: couldNotStart);
    }

    private void DescribeStream(TranscodeTestStressRun run, StreamState state, EncodingOptions options, string args, string itemName, float sourceFps)
    {
        var video = state.VideoStream;
        var tonemap = TranscodeTestRules.FindTonemapFilter(args);
        var encoder = _encodingHelper.GetVideoEncoder(state, options);
        run.Input = new TranscodeTestMediaInfo
        {
            Name = itemName,
            Width = video?.Width,
            Height = video?.Height,
            Codec = video?.Codec,
            BitDepth = video?.BitDepth,
            VideoRangeType = video?.VideoRangeType.ToString(),
            BitrateMbps = state.MediaSource.Bitrate.HasValue ? Math.Round(state.MediaSource.Bitrate.Value / 1e6, 1) : null,
            Fps = Math.Round(sourceFps, 3)
        };
        run.Output = new TranscodeTestOutputInfo
        {
            Width = state.OutputWidth,
            Height = state.OutputHeight,
            Codec = state.ActualOutputVideoCodec ?? string.Empty,
            BitrateMbps = Math.Round((state.OutputVideoBitrate ?? 0) / 1e6, 1),
            AudioCodec = state.ActualOutputAudioCodec,
            AudioChannels = state.OutputAudioChannels,
            Encoder = encoder,
            HardwareDecoding = TranscodeTestRules.UsesHardwareDecoder(args),
            HardwareEncoding = !TranscodeTestRules.IsSoftwareEncoder(encoder),
            Tonemap = TranscodeTestRules.TonemapMode(tonemap)
        };
    }

    /// <summary>
    /// Stops every stream. Safe to call more than once, also while <see cref="Dispose"/> stops them.
    /// </summary>
    private static async Task StopAllAsync(TranscodeTestStressRun run)
    {
        // The output is thrown away, so there is no need to let ffmpeg finish its files.
        var streams = run.Stop();
        var live = streams.Where(s => !s.HasExited).ToList();
        foreach (var s in live)
        {
            s.Kill();
        }

        using var timeout = new CancellationTokenSource(_shutdownWaitTimeout);
        foreach (var s in live)
        {
            await s.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }

        foreach (var s in streams)
        {
            s.DisposeProcess();
        }
    }

    /// <summary>
    /// Removes every file a stress test wrote.
    /// </summary>
    private static void DeleteTestFiles(string? folder, string? prefix, ILogger logger)
    {
        if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(prefix))
        {
            return;
        }

        try
        {
            foreach (var file in Directory.EnumerateFiles(folder, prefix + "*"))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    logger.LogWarning(ex, "Could not remove {File}", file);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Could not list {Folder}", folder);
        }
    }

    /// <summary>
    /// Stops a stress test that nobody has checked on for <see cref="TranscodeTestRules.IdleStopSeconds"/>, such as one
    /// left running in a browser that was closed.
    /// </summary>
    private void StopIfAbandoned(double now)
    {
        double lastCheckedAt;
        lock (_lock)
        {
            lastCheckedAt = _lastCheckedAt;
        }

        if (TranscodeTestRules.IsAbandoned(now, lastCheckedAt))
        {
            Log($"Nobody has checked on the test for {TranscodeTestRules.IdleStopSeconds / 60:0} min, so it stops.");
            throw new OperationCanceledException();
        }
    }

    /// <summary>
    /// Ends the test before it starts when the transcode folder is too small for the segments the streams would keep.
    /// </summary>
    private void CheckFreeSpace(string folder, TranscodeTestRequestDto request)
    {
        long free;
        try
        {
            free = new DriveInfo(folder).AvailableFreeSpace;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return;
        }

        var needed = TranscodeTestRules.NeededSpace(request.MaxStreams, request.VideoBitrate, 6);
        if (free < needed)
        {
            throw new InvalidOperationException(FormatLog($"The transcode folder has {free / 1e9:0.0} GB free, and {request.MaxStreams} streams at {request.VideoBitrate / 1e6:0.#} Mbps need about {needed / 1e9:0.0} GB. Free up space, or lower the bitrate or the number of streams."));
        }
    }

    // ---- shared -----------------------------------------------------------------------------

    /// <summary>
    /// Formats a log line the same way whatever the server's culture, so numbers always use a decimal point.
    /// </summary>
    /// <param name="message">The line.</param>
    /// <returns>The line as text.</returns>
    internal static string FormatLog(FormattableString message) => FormattableString.Invariant(message);

    // Preferred over Log(string) for interpolated strings, which C# would otherwise format with the current culture.
    [OverloadResolutionPriority(1)]
    private void Log(FormattableString message) => Log(FormatLog(message));

    private void Log(string message)
    {
        _logger.LogInformation("Transcoding test: {Message}", message);
        lock (_lock)
        {
            _log.Add(new TranscodeTestLogLine { Seconds = Math.Round(_clock.Elapsed.TotalSeconds, 1), Message = message });
            if (_log.Count > MaxLogLines)
            {
                _log.RemoveAt(0);
            }
        }
    }

    private void AddCheck(TranscodeTestCheckResult result)
    {
        var fps = result.Fps.HasValue ? string.Create(CultureInfo.InvariantCulture, $" at {result.Fps:0} fps") : string.Empty;
        var advice = result.Advice == TranscodeTestAdvice.None ? string.Empty : $"; advice: {result.Advice}";
        Log(FormatLog($"  {result.Kind} {result.Codec} {result.BitDepth}-bit{(result.Hdr ? " HDR" : string.Empty)}: {result.Outcome}{fps}{advice} {result.Method} {result.Error}").TrimEnd());
        lock (_lock)
        {
            _checks.Add(result);
        }
    }

    /// <summary>
    /// Ends a stress test because one stream exited with a non-zero code or produced no frames before the start
    /// timeout, naming the cause ffmpeg gave.
    /// </summary>
    private void EndStream(TranscodeTestStressRun run, TranscodeTestStream stream, TranscodeTestEndKind kind, int maxStable, double now, string? noRestart = null)
    {
        var otherSessions = run.GetStreams().Count(s => s != stream && !s.HasExited);
        // A stream restarted at the end of the file started long ago; only one that never produced a frame did not.
        var outcome = TranscodeTestRules.EndOutcome(
            kind,
            run.Output?.Encoder,
            stream.SessionLimitLine,
            stream.GpuMemoryLine,
            maxStable,
            stream.Id == run.LastStreamId,
            stream.EverReportedFrames,
            otherSessions);
        var error = TranscodeTestRules.EndEvidence(
            kind,
            outcome.EndReason,
            outcome.CouldNotStartStream,
            stream.SessionLimitLine,
            stream.GpuMemoryLine,
            stream.ErrorLines);
        if (string.IsNullOrEmpty(error) && noRestart is not null)
        {
            // ffmpeg exited cleanly, so it printed no error: say why the stream was not started again.
            error = noRestart;
        }

        var processorTime = ProcessorTime(stream.Process);
        // ffmpeg had not opened the file and barely used the CPU: it was waiting on the storage, not the GPU.
        var waitingOnInput = outcome.EndReason == TranscodeTestEndReason.StreamStalled
            && TranscodeTestRules.IsWaitingOnInput(kind, stream.SawInput, processorTime);

        // The result names only the cause when there is one; the log keeps all of ffmpeg's last error lines.
        var logged = string.IsNullOrEmpty(error)
            ? error
            : TranscodeTestRules.WithCause(TranscodeTestRules.EndCause(outcome.EndReason, stream.SessionLimitLine, stream.GpuMemoryLine), stream.ErrorLines);
        if (kind == TranscodeTestEndKind.Exited)
        {
            if (string.IsNullOrEmpty(logged) && noRestart is not null)
            {
                Log($"Stream {stream.Id} ended (exit {stream.ExitCode}): {noRestart}");
            }
            else if (string.IsNullOrEmpty(logged))
            {
                Log($"Stream {stream.Id} failed (exit {stream.ExitCode}). ffmpeg printed no error.");
            }
            else
            {
                Log($"Stream {stream.Id} failed (exit {stream.ExitCode}): {logged}");
            }
        }
        else
        {
            // ffmpeg's last lines help diagnose a hang, but they are not its error, so they only go in the log. When it
            // printed nothing, the diagnostics line says so.
            var output = stream.StallEvidence;
            if (!string.IsNullOrEmpty(logged))
            {
                Log($"Stream {stream.Id} produced no frames in {StartTimeoutSeconds:0} s. ffmpeg's error: {logged}");
            }
            else if (!string.IsNullOrEmpty(output))
            {
                Log($"Stream {stream.Id} produced no frames in {StartTimeoutSeconds:0} s. ffmpeg printed no error. Its last output: {output}");
            }
            else if (!stream.PrintedNothing)
            {
                Log($"Stream {stream.Id} produced no frames in {StartTimeoutSeconds:0} s. ffmpeg printed no error.");
            }

            // Read before the process is stopped.
            Log(stream.StallDiagnostics(now, processorTime, TranscodeTestProcessIo.BytesRead(stream.Process)));
        }

        if (outcome.EndReason == TranscodeTestEndReason.NvencSessionLimit)
        {
            LogNvencRefusal(stream.Id);
        }
        else if (outcome.EndReason == TranscodeTestEndReason.NvencSessionRefused)
        {
            Log($"NVENC refused a new session for stream {stream.Id} (a driver session limit or GPU memory). {maxStable} stream(s) kept up with real time before that.");
        }
        else if (outcome.EndReason == TranscodeTestEndReason.GpuMemoryFull)
        {
            Log($"The GPU ran out of memory on stream {stream.Id}. {maxStable} stream(s) kept up with real time before that; memory, not speed, is the limit here.");
        }
        else if (waitingOnInput)
        {
            Log(TranscodeTestRules.DescribeWaitingOnInput(outcome.CouldNotStartStream ? maxStable : null, stream.Id, StartTimeoutSeconds, processorTime.GetValueOrDefault()));
        }
        else if (outcome.CouldNotStartStream)
        {
            Log(TranscodeTestRules.DescribeCouldNotStart(kind, maxStable, stream.Id, StartTimeoutSeconds));
        }

        run.End(
            outcome.EndReason,
            now,
            stream.Id,
            error: error,
            couldNotStartStream: outcome.CouldNotStartStream,
            waitingOnInput: waitingOnInput);
    }

    private void LogNvencRefusal(int streamId)
        => Log($"NVIDIA's driver refused stream {streamId}. Consumer NVIDIA GPUs allow a limited number of encoding sessions at once (from 3 on older drivers to 12 on recent ones). That is a driver limit, not what the GPU can do.");

    private static TimeSpan? ProcessorTime(Process process)
    {
        try
        {
            return process.TotalProcessorTime;
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            return null;
        }
    }

    private static string DescribeErrors(FfmpegRun run) => string.IsNullOrEmpty(run.Errors) ? "no error message" : run.Errors;

    private sealed record ClipSpec(string Codec, int BitDepth, bool Hdr, string FileName, string Encoder, string PixelFormat, string ExtraArgs);

    private sealed record FfmpegRun(int ExitCode, double? Fps, string? InputPixelFormat, string Errors, int? FramesDecoded, int? FramesEncoded, string? ResourceLine, string? Output = null, bool Complete = true, bool NotStarted = false)
    {
        // Preserve ffmpeg's actual exit code, but an early clean EOF is not a completed 96-frame check.
        public int CheckExitCode => ExitCode == 0 && !Complete ? -1 : ExitCode;
    }
}
