using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.TranscodeTestDtos;
using Jellyfin.Extensions;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Runs the dashboard's transcoding test: a compatibility check of hardware decoding, encoding and tone mapping with
/// generated clips.
/// </summary>
public sealed class TranscodeTestManager : IDisposable
{
    private const string FolderName = "transcode-test";
    private const int CheckFrames = 96;
    private const int MaxLogLines = 500;

    private static readonly string[] _allDecodeCodecs = ["h264", "hevc", "mpeg2video", "mpeg4", "vc1", "vp8", "vp9", "av1"];

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
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<TranscodeTestManager> _logger;

    private TranscodeTestState _state = TranscodeTestState.Idle;
    private TranscodeTestKind _kind;
    private TranscodeTestRequestDto? _request;
    private DateTime? _startedAt;
    private DateTime? _endedAt;
    private List<TranscodeTestLogLine> _log = [];
    private List<TranscodeTestCheckResult> _checks = [];
    private CancellationTokenSource? _cancellation;
    private Stopwatch _clock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscodeTestManager"/> class.
    /// </summary>
    /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="encodingHelper">Instance of <see cref="EncodingHelper"/>.</param>
    /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{TranscodeTestManager}"/> interface.</param>
    public TranscodeTestManager(
        IServerConfigurationManager configurationManager,
        IMediaEncoder mediaEncoder,
        EncodingHelper encodingHelper,
        ISessionManager sessionManager,
        ILogger<TranscodeTestManager> logger)
    {
        _configurationManager = configurationManager;
        _mediaEncoder = mediaEncoder;
        _encodingHelper = encodingHelper;
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
            if (_state == TranscodeTestState.Running)
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
            _clock = Stopwatch.StartNew();
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
            return new TranscodeTestStatusDto
            {
                State = _state,
                Kind = _kind,
                Request = _request,
                StartedAt = _startedAt,
                EndedAt = _endedAt,
                Log = _log.ToArray(),
                Checks = _checks.ToArray()
            };
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cancellation?.Cancel();
        _cancellation?.Dispose();
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

            await RunCompatibilityAsync(options, cancellationToken).ConfigureAwait(false);

            Log("Done.");
        }
        catch (OperationCanceledException)
        {
            state = TranscodeTestState.Cancelled;
            Log("Stopped.");
        }
        catch (Exception ex)
        {
            state = TranscodeTestState.Failed;
            _logger.LogError(ex, "Transcoding test failed");
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
        var clips = new List<(ClipSpec Clip, MediaSourceInfo Source)>();
        foreach (var clip in _clips)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (source, error) = await GetClipAsync(clip, cancellationToken).ConfigureAwait(false);
            if (source is null)
            {
                AddCheck(new TranscodeTestCheckResult
                {
                    Kind = clip.Hdr ? TranscodeTestCheckKind.Tonemap : TranscodeTestCheckKind.Decode,
                    Codec = clip.Codec,
                    BitDepth = clip.BitDepth,
                    Hdr = clip.Hdr,
                    Outcome = TranscodeTestOutcome.Skipped,
                    Method = clip.Encoder,
                    Error = error
                });
                continue;
            }

            clips.Add((clip, source));
        }

        Log("Checking hardware decoding …");
        foreach (var (clip, source) in clips.Where(c => !c.Clip.Hdr))
        {
            await CheckDecodeAsync(clip, source, saved, cancellationToken).ConfigureAwait(false);
        }

        var h264 = clips.FirstOrDefault(c => c.Clip.FileName == "h264.mkv").Source;
        if (h264 is not null)
        {
            Log("Checking hardware encoding …");
            foreach (var codec in new[] { "h264", "hevc", "av1" })
            {
                await CheckEncodeAsync(codec, h264, saved, cancellationToken).ConfigureAwait(false);
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
    /// Gets a test clip, creating it the first time.
    /// </summary>
    /// <returns>The clip, or null and how ffmpeg failed to create it.</returns>
    private async Task<(MediaSourceInfo? Source, string? Error)> GetClipAsync(ClipSpec clip, CancellationToken cancellationToken)
    {
        var folder = Path.Combine(_configurationManager.ApplicationPaths.CachePath, FolderName);
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, clip.FileName);
        if (!File.Exists(path))
        {
            // Encoders are not in Jellyfin's capability list, so just try: a missing one makes ffmpeg fail.
            var result = await RunFfmpegAsync(TranscodeTestRules.ClipArguments(clip.Encoder, clip.PixelFormat, clip.ExtraArgs, clip.Hdr, path, false), null, null, cancellationToken).ConfigureAwait(false);
            if (result.ExitCode != 0 || !File.Exists(path))
            {
                Log($"  Could not create the {clip.FileName} test clip ({clip.Encoder}, {TranscodeTestRules.DescribeExit(result.ExitCode)}): {DescribeErrors(result)}. Trying again at 640x360, which needs far less memory …");
                File.Delete(path);
                result = await RunFfmpegAsync(TranscodeTestRules.ClipArguments(clip.Encoder, clip.PixelFormat, clip.ExtraArgs, clip.Hdr, path, true), null, null, cancellationToken).ConfigureAwait(false);
            }

            if (result.ExitCode != 0 || !File.Exists(path))
            {
                var error = $"{TranscodeTestRules.DescribeExit(result.ExitCode)}: {DescribeErrors(result)}";
                Log($"  Could not create the {clip.FileName} test clip at 640x360 either ({error})");
                File.Delete(path);
                return (null, error);
            }
        }

        var info = await _mediaEncoder.GetMediaInfo(
            new MediaInfoRequest
            {
                MediaSource = new MediaSourceInfo { Path = path, Protocol = MediaProtocol.File, VideoType = VideoType.VideoFile, Id = clip.FileName },
                MediaType = DlnaProfileType.Video
            },
            cancellationToken).ConfigureAwait(false);
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
        if (run is { ExitCode: not 0 } && options.EnableHardwareEncoding)
        {
            // The pipeline also encodes on the GPU. Retry with the CPU encoder, so a broken hardware encoder is not blamed on the decoder.
            Log($"Decoding {clip.Codec} failed. Trying again with the CPU encoder to see whether the decoder is the cause …");
            options.EnableHardwareEncoding = false;
            job = CreateJob(source, options, "h264", 1280);
            run = await RunJobAsync(job, options, cancellationToken).ConfigureAwait(false);
        }

        (result.Outcome, result.Advice) = TranscodeTestRules.Decode(hasHardwareDecoder, run?.ExitCode, run?.InputPixelFormat, enabled);
        result.Fps = run?.ExitCode == 0 ? run.Fps : null;
        result.Error = run?.ExitCode is null or 0 ? null : run.Errors;
        if (result.Outcome == TranscodeTestOutcome.Failed)
        {
            // Not one frame decoded, with the CPU encoder, while H.264 decodes here: the GPU lacks this format. The
            // advice to turn it off stays.
            result.Outcome = TranscodeTestRules.FailedOutcome(ReferencePassed(TranscodeTestCheckKind.Decode), !options.EnableHardwareEncoding, run!.FramesDecoded, run.ResourceLine);
            result.ReferencePassed = result.Outcome == TranscodeTestOutcome.NotSupported;
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
        (result.Outcome, result.Advice) = TranscodeTestRules.Encode(encoder, run?.ExitCode, saved.EnableHardwareEncoding, TranscodeTestRules.IsEncodeAllowed(codec, saved));
        result.Fps = run?.ExitCode == 0 ? run.Fps : null;
        result.Error = run?.ExitCode is null or 0 ? null : run.Errors;
        if (result.Outcome == TranscodeTestOutcome.Failed)
        {
            // Not one frame encoded, or the encoder failed before any output, from a clip decoded on the CPU, while
            // H.264 encodes here: the GPU lacks this format. The advice to turn it off stays.
            result.Outcome = TranscodeTestRules.FailedOutcome(ReferencePassed(TranscodeTestCheckKind.Encode), true, run!.FramesEncoded, run.ResourceLine);
            result.ReferencePassed = result.Outcome == TranscodeTestOutcome.NotSupported;
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
        (result.Outcome, result.Advice) = TranscodeTestRules.Tonemap(filter, run?.ExitCode, savedOn);
        result.Fps = run?.ExitCode == 0 ? run.Fps : null;
        result.Error = run?.ExitCode is null or 0 ? null : run.Errors;
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

    private async Task<FfmpegRun> RunFfmpegAsync(string args, int? frames, string? encoder, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Transcoding test: {Ffmpeg} {Args}", _mediaEncoder.EncoderPath, args);
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(_mediaEncoder.EncoderPath, args)
            {
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        var timer = Stopwatch.StartNew();
        process.Start();
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(60));
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            cancellationToken.ThrowIfCancellationRequested();
            return new FfmpegRun(-1, null, null, "timed out after 60 s", null, null, null);
        }

        var output = await stderr.ConfigureAwait(false);
        var elapsed = timer.Elapsed.TotalSeconds;
        var pixelFormat = TranscodeTestRules.DecodedPixelFormat(output);
        return new FfmpegRun(
            process.ExitCode,
            frames.HasValue && elapsed > 0 ? Math.Round(frames.Value / elapsed, 1) : null,
            pixelFormat,
            TranscodeTestRules.ErrorLines(output),
            TranscodeTestRules.FramesDecoded(output),
            encoder is null ? TranscodeTestRules.FramesEncoded(output) : TranscodeTestRules.EncodedFrames(output, encoder),
            TranscodeTestRules.ResourceLine(output));
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

    private static string DescribeErrors(FfmpegRun run) => string.IsNullOrEmpty(run.Errors) ? "no error message" : run.Errors;

    private sealed record ClipSpec(string Codec, int BitDepth, bool Hdr, string FileName, string Encoder, string PixelFormat, string ExtraArgs);

    private sealed record FfmpegRun(int ExitCode, double? Fps, string? InputPixelFormat, string Errors, int? FramesDecoded, int? FramesEncoded, string? ResourceLine);
}
