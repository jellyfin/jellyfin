using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Jellyfin.Api.Models.TranscodeTestDtos;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// How the transcoding test reads ffmpeg command lines and output, and what it concludes from them.
/// </summary>
internal static partial class TranscodeTestRules
{
    /// <summary>
    /// CPU seconds under which a stalled stream that never opened its file is taken to have been waiting on reading
    /// it. Decoding even one 4K frame takes longer; probing a file that reads quickly takes far less.
    /// </summary>
    internal const double WaitingOnInputCpuSeconds = 3;

    /// <summary>
    /// The fraction of the target frame rate under which streams at the limit are taken to have collapsed together.
    /// </summary>
    internal const double CollapseFraction = 0.25;

    /// <summary>
    /// The shortest time limit of a stress test, in seconds. The limit grows with the stream limit, see
    /// <see cref="TimeLimitSeconds"/>.
    /// </summary>
    internal const double MinTimeLimitSeconds = 15 * 60;

    /// <summary>
    /// Seconds after which a stress test that nobody checks on, such as one left running in a closed browser, stops.
    /// </summary>
    internal const double IdleStopSeconds = 10 * 60;

    /// <summary>
    /// How often one stream may restart at the end of the file. Only a source far shorter than a minute gets close
    /// within the time limit.
    /// </summary>
    internal const int MaxRestartsPerStream = 100;

    /// <summary>
    /// What NVENC's code 21 usually means, hedged: it names a license restriction, not a count.
    /// </summary>
    internal const string NvencCode21Hint = "Code 21 is NVIDIA's license restriction; on GeForce cards it is commonly the limit on concurrent encoding sessions, and sessions outside this test count too.";

    /// <summary>
    /// The smallest gap, in seconds, between the source duration and the final encoded position accepted as the end
    /// of the file. Audio often runs a little past the video, and the container duration covers both.
    /// </summary>
    internal const double MinEndOfFileToleranceSeconds = 2;

    /// <summary>
    /// The largest gap, in seconds, between the source duration and the final encoded position accepted as the end
    /// of the file. Between the two limits the allowance is one percent of the duration.
    /// </summary>
    internal const double MaxEndOfFileToleranceSeconds = 10;

    /// <summary>
    /// How many segments per stream the transcode folder must have room for: the ones kept, the one being written and
    /// a spare.
    /// </summary>
    internal const int SegmentsPerStreamOnDisk = 5;

    // How ffmpeg says the GPU ran out of memory: CUDA (NVDEC, cuvid, CUDA filters) through cuda_check.h; VAAPI through
    // libva's vaErrorStr for VA_STATUS_ERROR_ALLOCATION_FAILED; QSV MFX_ERR_MEMORY_ALLOC through ff_qsv_print_error;
    // Vulkan and libplacebo through ff_vk_ret2str.
    private static readonly string[] _gpuMemoryPatterns =
    [
        "CUDA_ERROR_OUT_OF_MEMORY",
        "(resource allocation failed)",
        "failed to allocate memory (-4)",
        "VK_ERROR_OUT_OF_DEVICE_MEMORY",
    ];

    private static readonly string[] _hardwareFrameFormats = ["videotoolbox_vld", "cuda", "qsv", "vaapi", "d3d11", "dxva2_vld", "drm_prime", "vulkan", "opencl"];
    private static readonly string[] _hardwareEncoderSuffixes = ["_amf", "_nvenc", "_qsv", "_vaapi", "_videotoolbox", "_v4l2m2m", "_rkmpp"];

    /// <summary>
    /// Gets whether ffmpeg arguments ask for a hardware decoder.
    /// </summary>
    /// <param name="arguments">The ffmpeg arguments.</param>
    /// <returns><c>true</c> if they use a hardware decoder.</returns>
    public static bool UsesHardwareDecoder(string arguments)
        => arguments.Contains("-hwaccel ", StringComparison.Ordinal) || HardwareDecoderRegex().IsMatch(arguments);

    /// <summary>
    /// Gets the pixel format of the decoded frames from ffmpeg's verbose output.
    /// </summary>
    /// <param name="output">ffmpeg's standard error at <c>-v verbose</c>.</param>
    /// <returns>The pixel format, or <c>null</c> if it is not in the output.</returns>
    public static string? DecodedPixelFormat(string output)
        => InputFormatRegex().Match(output) is { Success: true } match ? match.Groups[1].Value : null;

    /// <summary>
    /// Gets whether a pixel format is a hardware frame format, meaning the frames were decoded on the GPU.
    /// </summary>
    /// <param name="pixelFormat">The pixel format.</param>
    /// <returns><c>true</c> for hardware frames.</returns>
    public static bool IsHardwareFrameFormat(string? pixelFormat)
        => pixelFormat is not null && _hardwareFrameFormats.Contains(pixelFormat, StringComparer.Ordinal);

    /// <summary>
    /// Finds the tone mapping filter in a filter chain or command line.
    /// </summary>
    /// <param name="filters">The filter chain or command line.</param>
    /// <returns>The filter and its options, or <c>null</c> when nothing tone-maps.</returns>
    public static string? FindTonemapFilter(string filters)
        => TonemapFilterRegex().Match(filters) is { Success: true } match ? match.Value : null;

    /// <summary>
    /// Gets where a tone mapping filter runs.
    /// </summary>
    /// <param name="filter">The filter from <see cref="FindTonemapFilter"/>.</param>
    /// <returns>How HDR is tone-mapped.</returns>
    public static TranscodeTestTonemapMode TonemapMode(string? filter)
    {
        if (filter is null)
        {
            return TranscodeTestTonemapMode.None;
        }

        return filter.StartsWith("tonemapx", StringComparison.Ordinal) || filter.StartsWith("zscale", StringComparison.Ordinal)
            ? TranscodeTestTonemapMode.Software
            : TranscodeTestTonemapMode.Hardware;
    }

    /// <summary>
    /// Concludes a hardware decoding check.
    /// </summary>
    /// <param name="hasHardwareDecoder">Whether Jellyfin would use a hardware decoder for the clip.</param>
    /// <param name="exitCode">ffmpeg's exit code, when it ran.</param>
    /// <param name="decodedPixelFormat">The pixel format of the decoded frames.</param>
    /// <param name="enabled">Whether hardware decoding of the format is on in the settings.</param>
    /// <returns>The outcome and the advice.</returns>
    public static (TranscodeTestOutcome Outcome, TranscodeTestAdvice Advice) Decode(bool hasHardwareDecoder, int? exitCode, string? decodedPixelFormat, bool enabled)
    {
        TranscodeTestOutcome outcome;
        if (!hasHardwareDecoder)
        {
            outcome = TranscodeTestOutcome.NotSupported;
        }
        else if (exitCode != 0)
        {
            outcome = TranscodeTestOutcome.Failed;
        }
        else if (IsHardwareFrameFormat(decodedPixelFormat))
        {
            outcome = TranscodeTestOutcome.Passed;
        }
        else
        {
            // ffmpeg quietly fell back to the CPU.
            outcome = TranscodeTestOutcome.Software;
        }

        var advice = (outcome, enabled) switch
        {
            (TranscodeTestOutcome.Passed, false) => TranscodeTestAdvice.TurnOn,
            (TranscodeTestOutcome.Passed, true) => TranscodeTestAdvice.None,
            (TranscodeTestOutcome.Failed, _) => TranscodeTestAdvice.None,
            (_, true) => TranscodeTestAdvice.TurnOff,
            _ => TranscodeTestAdvice.None
        };
        return (outcome, advice);
    }

    /// <summary>
    /// Gets how many frames ffmpeg decoded, from its final statistics at <c>-v verbose</c>.
    /// </summary>
    /// <param name="output">ffmpeg's standard error.</param>
    /// <returns>The most frames any input stream decoded, or <c>null</c> when ffmpeg did not say.</returns>
    public static int? FramesDecoded(string output)
    {
        int? frames = null;
        foreach (Match match in FramesDecodedRegex().Matches(output))
        {
            if (int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var n))
            {
                frames = Math.Max(frames ?? 0, n);
            }
        }

        return frames;
    }

    /// <summary>
    /// Gets whether a failed decoding check decoded frames before it failed, so a later step of the pipeline, such as
    /// a filter or the encoder, failed and not the start of decoding.
    /// </summary>
    /// <param name="framesDecoded">How many frames ffmpeg decoded, see <see cref="FramesDecoded"/>.</param>
    /// <param name="errors">ffmpeg's error lines.</param>
    /// <returns><c>true</c> when frames were decoded, or, when ffmpeg did not say, when the error came from an output
    /// stream (<c>vost#</c>).</returns>
    public static bool FailedAfterDecoding(int? framesDecoded, string? errors)
        => framesDecoded is int frames
            ? frames > 0
            : errors is not null && errors.Contains("[vost#", StringComparison.Ordinal);

    /// <summary>
    /// Gets how many frames ffmpeg encoded, from its final statistics at <c>-v verbose</c>.
    /// </summary>
    /// <param name="output">ffmpeg's standard error.</param>
    /// <returns>The most frames any output stream encoded, or <c>null</c> when ffmpeg did not say.</returns>
    public static int? FramesEncoded(string output)
    {
        int? frames = null;
        foreach (Match match in FramesEncodedRegex().Matches(output))
        {
            if (int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var n))
            {
                frames = Math.Max(frames ?? 0, n);
            }
        }

        return frames;
    }

    /// <summary>
    /// Gets how many frames an encoder got out. When ffmpeg gave no count, the encoder is taken to have got none out
    /// only if ffmpeg's progress never passed frame 0 and an error names this encoder, its output stream, or opening
    /// the encoder or output stream.
    /// </summary>
    /// <param name="output">ffmpeg's standard error.</param>
    /// <param name="encoder">The encoder under test.</param>
    /// <returns>The frames encoded, 0 when the encoder failed before any output, or <c>null</c> when ffmpeg did not
    /// say.</returns>
    public static int? EncodedFrames(string output, string encoder)
    {
        if (FramesEncoded(output) is int frames)
        {
            return frames;
        }

        foreach (Match match in ProgressFramesRegex().Matches(output))
        {
            if (!int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var n) || n > 0)
            {
                return null;
            }
        }

        var lines = output.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return lines.Any(l => IsEncoderError(l, encoder)) ? 0 : null;
    }

    private static bool IsEncoderError(string line, string encoder)
    {
        var named = false;
        foreach (Match match in StreamTagRegex().Matches(line))
        {
            if (!string.Equals(match.Groups[1].Value, encoder, StringComparison.Ordinal))
            {
                // About another stream or encoder.
                return false;
            }

            named = true;
        }

        return named
            ? line.Contains("rror", StringComparison.Ordinal) || line.Contains("ailed", StringComparison.Ordinal) || line.Contains("Could not open", StringComparison.Ordinal)
            : line.Contains("Could not open encoder", StringComparison.Ordinal)
                || line.Contains("Error while opening encoder", StringComparison.Ordinal)
                || line.Contains("Error initializing output stream", StringComparison.Ordinal);
    }

    /// <summary>
    /// Concludes a decoding or encoding check that failed. It means the hardware lacks the format, not that the setup is
    /// broken, only when the H.264 8-bit check of the same kind passed in this run, the other half of the pipeline was
    /// ruled out, ffmpeg said it got not a single frame through, and a hardware codec explicitly rejected the format.
    /// Generic initialization, parameter and device failures do not prove missing format support.
    /// </summary>
    /// <param name="referencePassed">Whether the H.264 8-bit check of the same kind passed earlier in this run.</param>
    /// <param name="otherHalfRuledOut">For decoding, whether the failed run encoded on the CPU; for encoding, always
    /// <c>true</c>, as it decodes on the CPU.</param>
    /// <param name="frames">The frames ffmpeg decoded or encoded, or <c>null</c> when it did not say.</param>
    /// <param name="resourceLine">The line in which ffmpeg said the GPU refused a session or ran out of memory, or
    /// null. Such a failure is never a missing format: the GPU was busy or full, for example with playback.</param>
    /// <param name="output">ffmpeg's output, including any explicit format-capability diagnostic.</param>
    /// <returns><see cref="TranscodeTestOutcome.NotSupported"/> when the hardware lacks the format, otherwise
    /// <see cref="TranscodeTestOutcome.Failed"/>.</returns>
    public static TranscodeTestOutcome FailedOutcome(bool referencePassed, bool otherHalfRuledOut, int? frames, string? resourceLine = null, string? output = null)
        => referencePassed && otherHalfRuledOut && frames == 0 && resourceLine is null && output is not null
            && UnsupportedFormatLine(output) is not null && ResourceLine(output) is null
            ? TranscodeTestOutcome.NotSupported
            : TranscodeTestOutcome.Failed;

    /// <summary>
    /// Finds an explicit format-capability failure from a hardware codec. A generic "not supported" message may be
    /// about a filter, optional encoder setting, driver or device, so it is not sufficient.
    /// </summary>
    /// <param name="output">ffmpeg's output.</param>
    /// <returns>The capability diagnostic, or null when format support cannot be concluded.</returns>
    public static string? UnsupportedFormatLine(string? output)
    {
        if (output is null)
        {
            return null;
        }

        foreach (var line in output.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // These messages come from NVENC/CUVID's codec, chroma and bit-depth capability checks.
            if (HardwareCapabilityLogRegex().IsMatch(line)
                && UnsupportedFormatMessageRegex().IsMatch(line[(line.LastIndexOf(']') + 1)..].Trim()))
            {
                return line;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the first line in which ffmpeg said the GPU refused an encoding session or ran out of memory.
    /// </summary>
    /// <param name="output">ffmpeg's standard error.</param>
    /// <returns>The line, trimmed, or null.</returns>
    public static string? ResourceLine(string output)
        => output.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(l => IsSessionLimitText(l) || IsGpuMemoryLine(l));

    /// <summary>
    /// Puts the line that names the cause before ffmpeg's error lines, unless they already hold it.
    /// </summary>
    /// <param name="cause">The line, or null.</param>
    /// <param name="errors">The error lines, joined with " | ".</param>
    /// <returns>The error text.</returns>
    public static string PrependCause(string? cause, string errors)
        => string.IsNullOrEmpty(cause) || errors.Contains(cause, StringComparison.Ordinal)
            ? errors
            : string.IsNullOrEmpty(errors) ? cause : cause + " | " + errors;

    /// <summary>
    /// Gets whether the stress test can play an item: any video, such as a movie, an episode, a music video or a home
    /// video.
    /// </summary>
    /// <param name="item">The item, or <c>null</c> when it was not found.</param>
    /// <returns><c>true</c> for a video.</returns>
    public static bool IsStressTestItem(BaseItem? item) => item is Video;

    /// <summary>
    /// Describes a short source, and the distinction between continuous local loops and fallback restarts.
    /// </summary>
    /// <param name="seconds">The source's run time, or <c>null</c> when it is not known.</param>
    /// <returns>The line for the log, or <c>null</c> when the source is long enough or its length is not known.</returns>
    public static string? DescribeShortSource(double? seconds)
        => IsShortSource(seconds)
            ? string.Create(CultureInfo.InvariantCulture, $"This file is {seconds:0.#} s long. Finite local inputs loop within each process, and every pass resets the decoder and repeats a few frames, which can raise the result slightly; other inputs may restart at EOF, which interrupts measurement. A file of at least a minute gives steadier results.")
            : null;

    /// <summary>
    /// Gets whether a source is shorter than <see cref="TranscodeTestManager.ShortSourceSeconds"/>.
    /// </summary>
    /// <param name="seconds">The source's run time, or <c>null</c> when it is not known.</param>
    /// <returns><c>true</c> for a known run time under the minimum.</returns>
    public static bool IsShortSource(double? seconds) => seconds is double s && s > 0 && s < TranscodeTestManager.ShortSourceSeconds;

    /// <summary>
    /// Describes why the stress test refuses a source shorter than <see cref="TranscodeTestManager.MinimumSourceSeconds"/>.
    /// A source whose length is not known, such as a remote stream, is allowed.
    /// </summary>
    /// <param name="runTimeTicks">The source's run time, or <c>null</c> when it is not known.</param>
    /// <returns>The reason, or <c>null</c> when the source is long enough or its length is not known.</returns>
    public static string? DescribeTooShortSource(long? runTimeTicks)
        => runTimeTicks is long ticks && ticks > 0 && TimeSpan.FromTicks(ticks).TotalSeconds < TranscodeTestManager.MinimumSourceSeconds
            ? string.Create(CultureInfo.InvariantCulture, $"The stress test needs a video of at least {TranscodeTestManager.MinimumSourceSeconds:0} seconds; this one is {TimeSpan.FromTicks(ticks).TotalSeconds:0.0##} s. Choose a longer video.")
            : null;

    /// <summary>
    /// Gets whether the saved settings let Jellyfin pick an output codec. Jellyfin only prefers HEVC and AV1 when
    /// "Allow encoding in HEVC/AV1 format" is on, whatever the hardware can do.
    /// </summary>
    /// <param name="codec">The output codec.</param>
    /// <param name="saved">The saved encoding options.</param>
    /// <returns><c>false</c> for HEVC or AV1 when their "Allow" option is off, otherwise <c>true</c>.</returns>
    public static bool IsEncodeAllowed(string codec, EncodingOptions saved) => codec.ToLowerInvariant() switch
    {
        "hevc" or "h265" => saved.AllowHevcEncoding,
        "av1" => saved.AllowAv1Encoding,
        _ => true
    };

    /// <summary>
    /// Gets whether the saved settings would encode to a codec on the GPU.
    /// </summary>
    /// <param name="codec">The output codec.</param>
    /// <param name="saved">The saved encoding options.</param>
    /// <returns><c>true</c> if hardware encoding is on and the codec is allowed.</returns>
    public static bool IsEncodeEnabled(string codec, EncodingOptions saved)
        => saved.EnableHardwareEncoding && IsEncodeAllowed(codec, saved);

    /// <summary>
    /// Concludes a hardware encoding check.
    /// </summary>
    /// <param name="encoder">The encoder Jellyfin chose with hardware encoding on.</param>
    /// <param name="exitCode">ffmpeg's exit code, when it ran.</param>
    /// <param name="hardwareEncodingOn">Whether "Enable hardware encoding" is on in the settings.</param>
    /// <param name="codecAllowed">Whether the codec is allowed in the settings, see <see cref="IsEncodeAllowed"/>.</param>
    /// <returns>The outcome and the advice.</returns>
    public static (TranscodeTestOutcome Outcome, TranscodeTestAdvice Advice) Encode(string encoder, int? exitCode, bool hardwareEncodingOn, bool codecAllowed)
    {
        if (IsSoftwareEncoder(encoder))
        {
            return (TranscodeTestOutcome.NotSupported, TranscodeTestAdvice.None);
        }

        // Never advise allowing HEVC or AV1: that depends on the clients, not on the hardware.
        if (exitCode == 0)
        {
            return (TranscodeTestOutcome.Passed, !hardwareEncodingOn && codecAllowed ? TranscodeTestAdvice.TurnOn : TranscodeTestAdvice.None);
        }

        return (TranscodeTestOutcome.Failed, TranscodeTestAdvice.None);
    }

    /// <summary>
    /// Concludes a tone mapping check.
    /// </summary>
    /// <param name="filter">The tone mapping filter with tone mapping on, or <c>null</c> if there is none.</param>
    /// <param name="exitCode">ffmpeg's exit code, when it ran.</param>
    /// <param name="enabled">Whether the saved settings tone-map.</param>
    /// <returns>The outcome and the advice.</returns>
    public static (TranscodeTestOutcome Outcome, TranscodeTestAdvice Advice) Tonemap(string? filter, int? exitCode, bool enabled)
    {
        if (filter is null)
        {
            return (TranscodeTestOutcome.NotSupported, TranscodeTestAdvice.None);
        }

        if (exitCode != 0)
        {
            return (TranscodeTestOutcome.Failed, enabled ? TranscodeTestAdvice.TurnOff : TranscodeTestAdvice.None);
        }

        var outcome = TonemapMode(filter) == TranscodeTestTonemapMode.Software ? TranscodeTestOutcome.Software : TranscodeTestOutcome.Passed;
        return (outcome, enabled ? TranscodeTestAdvice.None : TranscodeTestAdvice.TurnOn);
    }

    /// <summary>
    /// Describes a compatibility check's failure. A clean exit with too few frames says so first; a failed run's own
    /// error lines say why, and the count would only push them out of view.
    /// </summary>
    /// <param name="exitCode">ffmpeg's exit code.</param>
    /// <param name="encodedFrames">The frames ffmpeg encoded, or null when it did not say.</param>
    /// <param name="requiredFrames">The frames the check needs, or null when it needs no count.</param>
    /// <param name="errors">ffmpeg's error lines.</param>
    /// <returns>The error text.</returns>
    public static string CheckErrors(int exitCode, int? encodedFrames, int? requiredFrames, string errors)
        => exitCode == 0 && encodedFrames is int actual && requiredFrames is int required && actual < required
            ? string.Create(CultureInfo.InvariantCulture, $"The check produced only {actual} of {required} required frames. {errors}").TrimEnd()
            : errors;

    /// <summary>
    /// Picks the lines of ffmpeg's output that explain a failure.
    /// </summary>
    /// <param name="output">ffmpeg's standard error.</param>
    /// <returns>Up to the last three error lines, joined with " | ".</returns>
    public static string ErrorLines(string output)
        => string.Join(
            " | ",
            output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(l => l.Contains("rror", StringComparison.Ordinal) || l.Contains("ailed", StringComparison.Ordinal))
                // Encoder libraries print harmless warnings such as SVT-AV1's "Svt[warn]: Failed to set thread priority".
                .Where(l => !l.Contains("[warn", StringComparison.OrdinalIgnoreCase))
                .TakeLast(3));

    /// <summary>
    /// Describes how ffmpeg ended, for a failure that may have printed nothing.
    /// </summary>
    /// <param name="exitCode">ffmpeg's exit code.</param>
    /// <returns>The exit code, and the signal when the process was killed by one, with what the common ones mean.</returns>
    public static string DescribeExit(int exitCode)
    {
        if (exitCode is not (> 128 and < 160))
        {
            return string.Create(CultureInfo.InvariantCulture, $"exit code {exitCode}");
        }

        var signal = exitCode - 128;
        var meaning = signal switch
        {
            4 => ", illegal instruction (the CPU may lack an instruction this encoder needs)",
            9 => ", killed (often because it ran out of memory)",
            _ => string.Empty
        };
        return string.Create(CultureInfo.InvariantCulture, $"exit code {exitCode}, ended by signal {signal}{meaning}");
    }

    /// <summary>
    /// Gets whether to remind the administrator to restart Jellyfin before testing VAAPI.
    /// </summary>
    /// <remarks>
    /// Jellyfin identifies the VAAPI device only at startup, and only when VAAPI is the saved acceleration type.
    /// Until then it uses its generic VAAPI pipeline, so results differ from those after a restart.
    /// </remarks>
    /// <param name="type">The saved hardware acceleration type.</param>
    /// <param name="isLinux">Whether the server runs on Linux, the only system where Jellyfin identifies the device.</param>
    /// <param name="deviceIdentified">Whether Jellyfin identified the device as AMD or Intel.</param>
    /// <returns><c>true</c> when the device was not identified.</returns>
    public static bool NeedsVaapiRestart(HardwareAccelerationType type, bool isLinux, bool deviceIdentified)
        => type == HardwareAccelerationType.vaapi && isLinux && !deviceIdentified;

    /// <summary>
    /// Builds the ffmpeg arguments that make a test clip from ffmpeg's test pattern.
    /// </summary>
    /// <remarks>
    /// The smaller form is a retry for small servers. At 1080p, libsvtav1 needs more than 512 MB, so a container with a
    /// tight memory limit kills it, or it crashes. At 640x360 it fits in 256 MB, and the clip only has to exercise the
    /// decoder. Thread options are left alone: "-threads 1" does not lower libsvtav1's memory, and its "lp=1"
    /// parameter crashes the macOS build.
    /// </remarks>
    /// <param name="encoder">The software encoder.</param>
    /// <param name="pixelFormat">The clip's pixel format.</param>
    /// <param name="extraArgs">Encoder options.</param>
    /// <param name="hdr">Whether to tag the clip as HDR10.</param>
    /// <param name="path">Where to write the clip.</param>
    /// <param name="smaller">Whether to make the 640x360 clip.</param>
    /// <returns>The arguments.</returns>
    public static string ClipArguments(string encoder, string pixelFormat, string extraArgs, bool hdr, string path, bool smaller)
    {
        var size = smaller ? "640x360" : "1920x1080";
        var hdrArgs = hdr ? "-vf setparams=color_primaries=bt2020:color_trc=smpte2084:colorspace=bt2020nc " : string.Empty;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"-hide_banner -v error -y -f lavfi -i testsrc2=size={size}:rate=24 -t 4 {hdrArgs}-c:v {encoder} -pix_fmt {pixelFormat} {extraArgs} \"{path}\"");
    }

    /// <summary>
    /// Gets whether an NVENC stream reported a session refusal, without identifying its cause.
    /// </summary>
    /// <remarks>
    /// Codes 10 and 21 identify resource or licensing restrictions, not a numerical session cap. They are kept as
    /// refusal diagnostics without claiming which limit caused this run to fail.
    /// </remarks>
    /// <param name="encoder">The stream's video encoder.</param>
    /// <param name="errors">ffmpeg's error lines.</param>
    /// <returns><c>true</c> for an NVENC encoder that failed with one of those errors.</returns>
    public static bool IsNvencSessionLimit(string? encoder, string? errors)
        => IsNvencEncoder(encoder) && IsSessionLimitText(errors);

    /// <summary>
    /// Describes NVENC refusing a stream's session, for the log. The refusal does not prove a session-count limit;
    /// code 21 adds a hedged hint.
    /// </summary>
    /// <param name="streamId">The refused stream.</param>
    /// <param name="maxStable">The most streams that all kept up.</param>
    /// <param name="sessionLine">ffmpeg's refusal line, or null.</param>
    /// <returns>The line for the log.</returns>
    public static string DescribeNvencRefusal(int streamId, int maxStable, string? sessionLine)
    {
        var text = string.Create(CultureInfo.InvariantCulture, $"NVENC refused a new session for stream {streamId}; this does not identify the cause, see ffmpeg's diagnostic above. {StreamCount(maxStable)} kept up with real time before that.");
        return sessionLine?.Contains("(21)", StringComparison.Ordinal) == true
            ? text + " " + NvencCode21Hint
            : text;
    }

    /// <summary>
    /// Gets whether text holds NVIDIA's session refusal, whatever the encoder.
    /// </summary>
    /// <param name="text">ffmpeg output.</param>
    /// <returns><c>true</c> for "out of memory (10)" or "incompatible client key (21)".</returns>
    internal static bool IsSessionLimitText(string? text)
        => text is not null
            && (text.Contains("out of memory (10)", StringComparison.OrdinalIgnoreCase)
                || text.Contains("incompatible client key (21)", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets whether an ffmpeg line says the GPU ran out of memory.
    /// </summary>
    /// <remarks>
    /// Each pattern is how ffmpeg prints device memory exhaustion for one backend. Generic text such as "Cannot
    /// allocate memory" is left out, because it is also printed for host memory. NVIDIA's session refusal, which says
    /// "out of memory (10)", is never a GPU memory line.
    /// </remarks>
    /// <param name="line">One line of ffmpeg output.</param>
    /// <returns><c>true</c> when the line matches a GPU memory pattern.</returns>
    internal static bool IsGpuMemoryLine(string? line)
        => line is not null
            && !IsSessionLimitText(line)
            && _gpuMemoryPatterns.Any(p => line.Contains(p, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Concludes a stress-test stream that produced no frames before the start timeout.
    /// </summary>
    /// <remarks>
    /// The same rules as <see cref="EndOutcome"/> for a stall, with the stream's evidence as the session text and no
    /// GPU memory line.
    /// </remarks>
    /// <param name="encoder">The streams' video encoder.</param>
    /// <param name="evidence">The stalled stream's last ffmpeg output.</param>
    /// <param name="maxStable">The most streams that all kept up.</param>
    /// <param name="isNewest">Whether the stalled stream is the most recently added one.</param>
    /// <param name="otherSessions">How many other streams were running.</param>
    /// <returns>Why the test ended, and whether the server could not start one more stream.</returns>
    public static (TranscodeTestEndReason EndReason, bool CouldNotStartStream) StallOutcome(string? encoder, string? evidence, int maxStable, bool isNewest, int otherSessions)
        => EndOutcome(TranscodeTestEndKind.Stalled, encoder, evidence, null, maxStable, isNewest, false, otherSessions);

    /// <summary>
    /// Concludes a stress-test stream that exited with a non-zero code or stalled.
    /// </summary>
    /// <remarks>
    /// NVIDIA's session refusal is matched first, whatever came before or after it. Neither code 10 nor code 21
    /// proves a numerical driver session cap: NVIDIA defines them as allocation failure and unavailable licensed
    /// functionality, respectively. Counts, including historical limits and external playback, cannot prove a cap.
    /// Then a GPU memory error after at
    /// least one stream kept up means memory was the limit: the kept-up count is the result. Otherwise, the newest
    /// stream ending before its first frame after at least one stream kept up means the server could not start one
    /// more stream. A silent hang is never blamed on NVIDIA's session limit: drivers refuse a session with an error and
    /// exit, and that refusal is matched first.
    /// </remarks>
    /// <param name="kind">How the stream ended.</param>
    /// <param name="encoder">The streams' video encoder.</param>
    /// <param name="sessionLimitLine">The stream's first line with NVIDIA's session refusal text, or null.</param>
    /// <param name="gpuMemoryLine">The stream's first line saying the GPU ran out of memory, or null.</param>
    /// <param name="maxStable">The most streams that all kept up.</param>
    /// <param name="isNewest">Whether the stream is the most recently added one.</param>
    /// <param name="reportedFrames">Whether the stream reported at least one encoded frame, in this process or in one
    /// before it that reached the end of the file.</param>
    /// <param name="otherSessions">How many other streams were running when it ended.</param>
    /// <returns>Why the test ended, and whether the server could not start one more stream.</returns>
    internal static (TranscodeTestEndReason EndReason, bool CouldNotStartStream) EndOutcome(
        TranscodeTestEndKind kind,
        string? encoder,
        string? sessionLimitLine,
        string? gpuMemoryLine,
        int maxStable,
        bool isNewest,
        bool reportedFrames,
        int otherSessions)
    {
        if (IsNvencSessionLimit(encoder, sessionLimitLine))
        {
            return (TranscodeTestEndReason.NvencSessionRefused, false);
        }

        if (gpuMemoryLine is not null && maxStable >= 1)
        {
            return (TranscodeTestEndReason.GpuMemoryFull, false);
        }

        // Only a stream that never reported a frame never started, whether it then exited or stalled.
        var neverStarted = !reportedFrames;
        var couldNotStart = isNewest && maxStable >= 1 && neverStarted;
        var reason = kind == TranscodeTestEndKind.Stalled ? TranscodeTestEndReason.StreamStalled : TranscodeTestEndReason.StreamFailed;
        return (reason, couldNotStart);
    }

    /// <summary>
    /// Describes the newest stream not starting after others kept up.
    /// </summary>
    /// <param name="kind">How the stream ended: an exit before its first frame, or no frames before the timeout.</param>
    /// <param name="maxStable">The most streams that all kept up.</param>
    /// <param name="streamId">The stream that did not start.</param>
    /// <param name="startTimeoutSeconds">How long a stream may take to report its first frame.</param>
    /// <returns>The sentence for the log.</returns>
    internal static string DescribeCouldNotStart(TranscodeTestEndKind kind, int maxStable, int streamId, double startTimeoutSeconds)
        => kind == TranscodeTestEndKind.Stalled
            ? string.Create(CultureInfo.InvariantCulture, $"{maxStable} stream(s) kept up; stream {streamId} started but produced no frames in {startTimeoutSeconds:0} s.")
            : string.Create(CultureInfo.InvariantCulture, $"{maxStable} stream(s) kept up; the server could not start stream {streamId}.");

    /// <summary>
    /// Describes a stream whose ffmpeg the system did not run within the start timeout.
    /// </summary>
    /// <param name="startTimeoutSeconds">How long the test waited for it.</param>
    /// <returns>The reason, for the result and the log.</returns>
    internal static string DescribeLaunchTimeout(double startTimeoutSeconds)
        => string.Create(CultureInfo.InvariantCulture, $"The system did not run ffmpeg within {startTimeoutSeconds:0} s; check that the storage holding ffmpeg responds.");

    /// <summary>
    /// Gets whether a stream that produced no frames was waiting rather than working: ffmpeg had not opened the file,
    /// so it was still probing it, and it used little CPU time, so it was waiting on reading the media.
    /// </summary>
    /// <param name="kind">How the stream ended.</param>
    /// <param name="sawInput">Whether ffmpeg said it opened the file.</param>
    /// <param name="processorTime">ffmpeg's CPU time, or null when it could not be read.</param>
    /// <returns><c>true</c> for a stall before the file was opened, with less than
    /// <see cref="WaitingOnInputCpuSeconds"/> of CPU time.</returns>
    internal static bool IsWaitingOnInput(TranscodeTestEndKind kind, bool sawInput, TimeSpan? processorTime)
        => kind == TranscodeTestEndKind.Stalled && !sawInput && processorTime is TimeSpan p && p.TotalSeconds < WaitingOnInputCpuSeconds;

    /// <summary>
    /// Describes a stream that did not start because ffmpeg was waiting on reading the file.
    /// </summary>
    /// <param name="maxStable">The most streams that all kept up, or null when the stream is not a capacity result.</param>
    /// <param name="streamId">The stream that did not start.</param>
    /// <param name="startTimeoutSeconds">How long a stream may take to report its first frame.</param>
    /// <param name="processorTime">ffmpeg's CPU time.</param>
    /// <returns>The sentence for the log.</returns>
    internal static string DescribeWaitingOnInput(int? maxStable, int streamId, double startTimeoutSeconds, TimeSpan processorTime)
    {
        var text = string.Create(
            CultureInfo.InvariantCulture,
            $"Stream {streamId} did not start within {startTimeoutSeconds:0} s: ffmpeg had not opened the file yet and used {processorTime.TotalSeconds:0.0} s of CPU time, so it was waiting, probably on reading the media. Slow or shared storage, not the GPU, may be the limit.");
        return maxStable is int kept ? string.Create(CultureInfo.InvariantCulture, $"{kept} stream(s) kept up. {text}") : text;
    }

    /// <summary>
    /// Gets the line that names why a stress test ended, for the endings that have one.
    /// </summary>
    /// <param name="reason">Why the test ended.</param>
    /// <param name="sessionLimitLine">The stream's first session refusal line, or null.</param>
    /// <param name="gpuMemoryLine">The stream's first GPU memory line, or null.</param>
    /// <returns>The session refusal for the driver limit, the memory line for full GPU memory, otherwise null.</returns>
    internal static string? EndCause(TranscodeTestEndReason reason, string? sessionLimitLine, string? gpuMemoryLine) => reason switch
    {
        TranscodeTestEndReason.NvencSessionRefused => sessionLimitLine,
        TranscodeTestEndReason.GpuMemoryFull => gpuMemoryLine,
        _ => null
    };

    /// <summary>
    /// Picks the error text for the stream that ended a stress test.
    /// </summary>
    /// <param name="kind">How the stream ended.</param>
    /// <param name="reason">Why the test ended.</param>
    /// <param name="couldNotStart">Whether the server could not start the stream.</param>
    /// <param name="sessionLimitLine">The stream's first session refusal line, or null.</param>
    /// <param name="gpuMemoryLine">The stream's first GPU memory line, or null.</param>
    /// <param name="errorLines">The stream's last error lines.</param>
    /// <returns>Only the matched line for the driver limit and full GPU memory; the other error lines stay in the
    /// server log. Otherwise ffmpeg's error lines joined with " | ". Other output, such as warnings, is never returned
    /// as the error. Empty for a plain failure without error lines, and null for a stream that never started without
    /// them.</returns>
    internal static string? EndEvidence(
        TranscodeTestEndKind kind,
        TranscodeTestEndReason reason,
        bool couldNotStart,
        string? sessionLimitLine,
        string? gpuMemoryLine,
        IReadOnlyList<string> errorLines)
    {
        var cause = EndCause(reason, sessionLimitLine, gpuMemoryLine);
        if (!string.IsNullOrEmpty(cause))
        {
            return cause;
        }

        // A plain failure keeps its error lines even when empty, as before; a stream that never started has null.
        var text = string.Join(" | ", errorLines);
        return (kind == TranscodeTestEndKind.Exited && !couldNotStart) || !string.IsNullOrEmpty(text) ? text : null;
    }

    /// <summary>
    /// Puts the line that names the cause before the kept lines, unless one of them is that line.
    /// </summary>
    /// <param name="cause">The matched line, or null.</param>
    /// <param name="kept">The kept error lines.</param>
    /// <returns>The lines joined with " | ", with the cause once.</returns>
    internal static string WithCause(string? cause, IReadOnlyList<string> kept)
        => string.IsNullOrEmpty(cause) || kept.Contains(cause, StringComparer.Ordinal)
            ? string.Join(" | ", kept)
            : string.Join(" | ", kept.Prepend(cause));

    private static bool IsNvencEncoder(string? encoder)
        => encoder is not null && encoder.Contains("nvenc", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Names why a stress test ended the way the Jellyfin hardware survey (hwa.jellyfin.org) does, so results can be
    /// compared with it.
    /// </summary>
    /// <param name="reason">Why the test ended.</param>
    /// <returns>The survey's name; <c>null</c> for an ending the survey has no name for. The survey has one name for
    /// every NVENC session refusal.</returns>
    public static TranscodeTestSurveyFailureReason? SurveyFailureReason(TranscodeTestEndReason reason) => reason switch
    {
        TranscodeTestEndReason.LimitReached => TranscodeTestSurveyFailureReason.Performance,
        TranscodeTestEndReason.StreamStalled => TranscodeTestSurveyFailureReason.FailedTimeout,
        TranscodeTestEndReason.NvencSessionRefused => TranscodeTestSurveyFailureReason.FailedNvencLimit,
        _ => null
    };

    /// <summary>
    /// Gets how long a stress test may run at most: long enough for every stream to be added, fall behind once and
    /// take the longest start, and at least <see cref="MinTimeLimitSeconds"/>.
    /// </summary>
    /// <param name="maxStreams">The most streams the test may run.</param>
    /// <returns>The limit in seconds.</returns>
    public static double TimeLimitSeconds(int maxStreams)
        => Math.Max(MinTimeLimitSeconds, maxStreams * (TranscodeTestManager.RampSeconds + TranscodeTestManager.FailSeconds + TranscodeTestManager.StartTimeoutSeconds));

    /// <summary>
    /// Gets why a stress test that ran for its time limit ended.
    /// </summary>
    /// <param name="maxStable">The most streams that all kept up.</param>
    /// <param name="maxStreams">The most streams the test may run.</param>
    /// <returns><see cref="TranscodeTestEndReason.MaxStreams"/> when every allowed stream kept up, otherwise
    /// <see cref="TranscodeTestEndReason.Inconclusive"/> otherwise: the kept-up count is only a lower bound.</returns>
    public static TranscodeTestEndReason TimeLimitEndReason(int maxStable, int maxStreams)
        => maxStable >= maxStreams ? TranscodeTestEndReason.MaxStreams : TranscodeTestEndReason.Inconclusive;

    /// <summary>
    /// Describes a stress test that ran for its time limit, for the log.
    /// </summary>
    /// <param name="limitSeconds">The limit.</param>
    /// <param name="maxStable">The most streams that all kept up.</param>
    /// <returns>The sentence for the log.</returns>
    public static string DescribeTimeLimit(double limitSeconds, int maxStable)
        => maxStable == 0
            ? string.Create(CultureInfo.InvariantCulture, $"The test reached its time limit of {limitSeconds / 60:0} min without measuring a kept-up stream. The result is inconclusive; no performance limit was established.")
            : string.Create(CultureInfo.InvariantCulture, $"The test reached its time limit of {limitSeconds / 60:0} min; at least {StreamCount(maxStable)} kept up, but no performance limit was established.");

    /// <summary>
    /// Gets whether a stress test was left alone too long and should stop.
    /// </summary>
    /// <param name="now">Seconds since the test started.</param>
    /// <param name="lastCheckedAt">When its status was last asked for, in seconds since the start.</param>
    /// <returns><c>true</c> after <see cref="IdleStopSeconds"/> without a status request.</returns>
    public static bool IsAbandoned(double now, double lastCheckedAt) => now - lastCheckedAt >= IdleStopSeconds;

    /// <summary>
    /// Gets whether ffmpeg's last reported position reaches the source's duration within a small, bounded allowance:
    /// one percent of the duration, at least <see cref="MinEndOfFileToleranceSeconds"/> and at most
    /// <see cref="MaxEndOfFileToleranceSeconds"/>, and never more than a quarter of the duration.
    /// </summary>
    /// <param name="positionSeconds">ffmpeg's last reported position in the source, or null before its first frame.</param>
    /// <param name="sourceSeconds">The source's run time, or null when it is not known.</param>
    /// <returns><c>true</c> or <c>false</c>, or null when either value is not known.</returns>
    public static bool? IsAtEndOfFile(double? positionSeconds, double? sourceSeconds)
    {
        if (positionSeconds is not double position || sourceSeconds is not double length
            || !double.IsFinite(position) || !double.IsFinite(length) || position < 0 || length <= 0)
        {
            return null;
        }

        // A file of a few seconds keeps three quarters of its length, so a stream that stopped halfway is not at its end.
        var tolerance = Math.Min(Math.Clamp(length * 0.01, MinEndOfFileToleranceSeconds, MaxEndOfFileToleranceSeconds), length / 4);
        return position >= length - tolerance;
    }

    /// <summary>
    /// Gets whether a stream that exited on its own reached the end of the file and should start again. It must have
    /// exited cleanly, produced frames itself and not have restarted too often. When its position and the source's
    /// run time are known, the position must be at the end of the file, see <see cref="IsAtEndOfFile"/>. An unknown
    /// duration or position cannot verify EOF; the process's lifetime alone is not evidence of it.
    /// </summary>
    /// <param name="exitCode">ffmpeg's exit code.</param>
    /// <param name="reportedFrames">Whether this process reported at least one encoded frame.</param>
    /// <param name="livedSeconds">How long the process ran.</param>
    /// <param name="atEndOfFile">Whether the process's position was at the end of the file, or null when not known.</param>
    /// <param name="restarts">How often the stream restarted before.</param>
    /// <returns><c>true</c> to start it again.</returns>
    public static bool CanRestart(int exitCode, bool reportedFrames, double livedSeconds, bool? atEndOfFile, int restarts)
        => exitCode == 0
            && reportedFrames
            && atEndOfFile == true
            && restarts < MaxRestartsPerStream;

    /// <summary>
    /// Gets whether a stream reached the end of the file cleanly but has restarted <see cref="MaxRestartsPerStream"/>
    /// times, so the file is too short to keep the test going.
    /// </summary>
    /// <param name="exitCode">ffmpeg's exit code.</param>
    /// <param name="reportedFrames">Whether this process reported at least one encoded frame.</param>
    /// <param name="atEndOfFile">Whether the process's position was at the end of the file, or null when not known.</param>
    /// <param name="restarts">How often the stream restarted before.</param>
    /// <returns><c>true</c> when only the restart limit stopped a restart.</returns>
    public static bool IsRestartLimit(int exitCode, bool reportedFrames, bool? atEndOfFile, int restarts)
        => exitCode == 0 && reportedFrames && atEndOfFile == true && restarts >= MaxRestartsPerStream;

    /// <summary>
    /// Describes a test that stopped because a stream restarted too often, for the log.
    /// </summary>
    /// <param name="streamId">The stream.</param>
    /// <param name="restarts">How often it restarted.</param>
    /// <param name="maxStable">The most streams that all kept up.</param>
    /// <returns>The sentence for the log.</returns>
    public static string DescribeRestartLimit(int streamId, int restarts, int maxStable)
        => maxStable == 0
            ? string.Create(CultureInfo.InvariantCulture, $"Stream {streamId} restarted {restarts} times, so the test stops without measuring capacity. The result is inconclusive; use a longer file.")
            : string.Create(CultureInfo.InvariantCulture, $"Stream {streamId} restarted {restarts} times, so the test stops; at least {StreamCount(maxStable)} kept up, but no performance limit was established. Use a longer file.");

    /// <summary>
    /// Gets whether ffmpeg affirmatively reported an incomplete input, even when its exit code was zero.
    /// </summary>
    /// <param name="line">One ffmpeg diagnostic line.</param>
    /// <returns>Whether the line names premature or partial input.</returns>
    internal static bool IsPrematureEndLine(string line)
        => PrematureEndRegex().IsMatch(line.Trim());

    /// <summary>
    /// Gets whether ffmpeg could no longer read its input: a storage outage, a vanished or unreadable file, or a
    /// source that stopped delivering. That is a failure of the input, not a measure of the server's capacity.
    /// </summary>
    /// <param name="line">The diagnostic after its level tag has been removed.</param>
    /// <returns>Whether the line reports an input read failure.</returns>
    /// <remarks>
    /// End of file is not one: ffmpeg prints "EOF while reading input" on every pass of a looped input, and the
    /// position check already tells a non-looped input's real end from an early one.
    /// </remarks>
    internal static bool IsInputFailureLine(string line)
        => InputFailureRegex().IsMatch(line.Trim());

    /// <summary>
    /// Gets whether the selected input video's decoder affirmatively finished, rather than only its audio stream.
    /// </summary>
    /// <param name="line">The diagnostic after its level tag has been removed.</param>
    /// <returns>Whether ffmpeg reported normal video-decoder EOF.</returns>
    internal static bool IsVideoEndOfFileLine(string line)
        => VideoEndOfFileRegex().IsMatch(line);

    /// <summary>
    /// Describes why a stream that exited cleanly was not started again, for a failure that ffmpeg gave no error for.
    /// </summary>
    /// <param name="exitCode">ffmpeg's exit code.</param>
    /// <param name="reportedFrames">Whether the process reported at least one encoded frame.</param>
    /// <param name="livedSeconds">How long the process ran.</param>
    /// <param name="positionSeconds">ffmpeg's last reported position in the source, or null before its first frame.</param>
    /// <param name="sourceSeconds">The source's run time, or null when it is not known.</param>
    /// <param name="restarts">How often the stream restarted before.</param>
    /// <returns>The reason, or null for a non-zero exit, which ffmpeg's error lines explain.</returns>
    public static string? DescribeNoRestart(int exitCode, bool reportedFrames, double livedSeconds, double? positionSeconds, double? sourceSeconds, int restarts)
    {
        if (exitCode != 0)
        {
            return null;
        }

        if (!reportedFrames)
        {
            return "ffmpeg exited without encoding a frame.";
        }

        if (restarts >= MaxRestartsPerStream)
        {
            return string.Create(CultureInfo.InvariantCulture, $"The stream restarted {restarts} times; use a longer file.");
        }

        if (IsAtEndOfFile(positionSeconds, sourceSeconds) == false)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"ffmpeg exited cleanly at {positionSeconds:0.0} s of the {sourceSeconds:0.0} s file ({positionSeconds / sourceSeconds:0%}), before the end of the file.");
        }

        return "ffmpeg exited cleanly, but its final position or the source duration is unknown, so the end of the file could not be verified.";
    }

    /// <summary>
    /// Estimates the space a stress test needs in the transcode folder.
    /// </summary>
    /// <param name="maxStreams">The most streams.</param>
    /// <param name="videoBitrate">The video bitrate, in bits per second.</param>
    /// <param name="segmentSeconds">The segment length.</param>
    /// <returns>The bytes needed, with audio and muxing overhead.</returns>
    public static long NeededSpace(int maxStreams, int videoBitrate, int segmentSeconds)
        => (long)(maxStreams * SegmentsPerStreamOnDisk * segmentSeconds * ((videoBitrate + 192_000) / 8.0) * 1.1);

    /// <summary>
    /// Gets whether an ffmpeg encoder runs on the CPU.
    /// </summary>
    /// <param name="encoder">The encoder name.</param>
    /// <returns><c>true</c> for software encoders such as libx264.</returns>
    public static bool IsSoftwareEncoder(string encoder) => encoder.StartsWith("lib", StringComparison.Ordinal);

    /// <summary>
    /// Gets whether the encoder selected by Jellyfin is a hardware encoder, not stream copy or a software encoder.
    /// </summary>
    /// <param name="encoder">The selected encoder name, or null when it is not known.</param>
    /// <returns><c>true</c> for Jellyfin's hardware encoder backends.</returns>
    public static bool IsHardwareEncoder(string? encoder)
        => encoder is not null && _hardwareEncoderSuffixes.Any(suffix =>
            encoder.Equals("h264" + suffix, StringComparison.OrdinalIgnoreCase)
            || encoder.Equals("hevc" + suffix, StringComparison.OrdinalIgnoreCase)
            || encoder.Equals("av1" + suffix, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Rounds a stress test frame rate reading to one decimal, or to two or three when fewer would move it onto or
    /// across the threshold, so a reading under the threshold never looks equal to it or above it.
    /// </summary>
    /// <param name="fps">The reading.</param>
    /// <param name="thresholdFps">The frame rate each stream must keep, at most three decimals.</param>
    /// <returns>The rounded reading, on the same side of the threshold as <paramref name="fps"/>.</returns>
    public static double RoundFps(double fps, double thresholdFps)
    {
        var side = Math.Sign(fps.CompareTo(thresholdFps));
        for (var decimals = 1; decimals <= 3; decimals++)
        {
            var rounded = Math.Round(fps, decimals);
            if (Math.Sign(rounded.CompareTo(thresholdFps)) == side)
            {
                return rounded;
            }
        }

        // Within a thousandth of the threshold: round away from it.
        return side < 0 ? Math.Floor(fps * 1000) / 1000 : Math.Ceiling(fps * 1000) / 1000;
    }

    /// <summary>
    /// Formats a stress test frame rate with at least one and at most three decimals: 23.976, 24.0, 23.96.
    /// </summary>
    /// <param name="fps">The frame rate, already rounded with <see cref="RoundFps"/> or to the source's precision.</param>
    /// <returns>The frame rate as text.</returns>
    public static string FormatFps(double fps) => fps.ToString("0.0##", CultureInfo.InvariantCulture);

    /// <summary>
    /// Counts streams for the log: "1 stream", "5 streams".
    /// </summary>
    /// <param name="count">The number of streams.</param>
    /// <returns>The count as text.</returns>
    public static string StreamCount(int count)
        => count == 1 ? "1 stream" : string.Create(CultureInfo.InvariantCulture, $"{count} streams");

    /// <summary>
    /// Describes how a stress test ended, for the log.
    /// </summary>
    /// <param name="result">The finished test.</param>
    /// <returns>How many streams kept up and, when one fell behind, how many were running then.</returns>
    public static string DescribeResult(TranscodeTestStressResult result)
    {
        if (result.EndReason == TranscodeTestEndReason.Inconclusive)
        {
            return result.MaxStableStreams == 0
                ? "Result: inconclusive. No concurrent capacity was measured; no performance limit was established."
                : string.Create(CultureInfo.InvariantCulture, $"Result: inconclusive. At least {result.MaxStableStreams} stream(s) kept up with real time, but no performance limit was established.");
        }

        var text = string.Create(CultureInfo.InvariantCulture, $"Result: {result.MaxStableStreams} stream(s) kept up with real time.");
        if (result.EndReason == TranscodeTestEndReason.LimitReached && result.FailedStreamId is int id && result.FailedStreamFps is double fps)
        {
            text += string.Create(CultureInfo.InvariantCulture, $" With {StreamCount(result.StreamsAtEnd)} running, stream {id} fell to {FormatFps(fps)} fps.");
        }

        if (result.TimeLimitReached)
        {
            text += " The test stopped at its time limit, not because a stream fell behind.";
        }

        if (result.Collapsed)
        {
            text += " All streams slowed down sharply at once, rather than one falling a little behind. This does not identify the cause; check GPU memory, drivers, CPU load and storage.";
        }

        return text;
    }

    /// <summary>
    /// Gets whether the streams collapsed together when the limit was reached: all streams but at most one, and at
    /// least two, are under <see cref="CollapseFraction"/> of the current target. A historical aggregate peak at a
    /// different stream count is not comparable. One stream falling behind while the others keep up is not a collapse.
    /// </summary>
    /// <param name="fps">The running streams' frame rates, <c>null</c> for a stream not measured yet.</param>
    /// <param name="thresholdFps">The frame rate each stream must keep.</param>
    /// <returns><c>true</c> for a collapse.</returns>
    public static bool IsCollapse(IReadOnlyList<double?> fps, double thresholdFps)
    {
        if (!double.IsFinite(thresholdFps) || thresholdFps <= 0)
        {
            return false;
        }

        var low = fps.Count(f => f is double measured && double.IsFinite(measured) && measured >= 0 && measured < thresholdFps * CollapseFraction);
        return low >= 2 && low >= fps.Count - 1;
    }

    /// <summary>
    /// Describes a stream dipping below the threshold, which holds back the next stream until every stream keeps up again.
    /// </summary>
    /// <param name="streamId">The stream that dipped.</param>
    /// <param name="fps">Its frame rate.</param>
    /// <param name="thresholdFps">The frame rate each stream must keep.</param>
    /// <param name="settleSeconds">How long every stream must keep up after a dip before a stream is added.</param>
    /// <param name="nextStreamId">The stream to add next, or null when the test is at its maximum.</param>
    /// <returns>The line for the log.</returns>
    public static string DescribeDip(int streamId, double fps, double thresholdFps, double settleSeconds, int? nextStreamId)
    {
        var reading = FormatFps(RoundFps(fps, thresholdFps));
        return nextStreamId is int next
            ? string.Create(CultureInfo.InvariantCulture, $"Stream {streamId} dipped to {reading} fps; stream {next} waits until every stream has kept up for {settleSeconds:0} s.")
            : string.Create(CultureInfo.InvariantCulture, $"Stream {streamId} dipped to {reading} fps; the test ends once every stream has kept up for {settleSeconds:0} s.");
    }

    /// <summary>
    /// Describes when the stress test adds a stream.
    /// </summary>
    /// <param name="rampSeconds">The least time between two added streams.</param>
    /// <param name="windowSeconds">How long each stream's frame rate is measured over.</param>
    /// <returns>The sentence for the log.</returns>
    public static string DescribeRamp(double rampSeconds, double windowSeconds)
        => string.Create(CultureInfo.InvariantCulture, $"A stream is added at most every {rampSeconds:0} s, once every running stream has kept up for {windowSeconds:0} s.");

    [GeneratedRegex(@"graph [-0-9]+ input from stream 0:0 @ [^\]]+\] .*?pixfmt:([a-z0-9_]+)")]
    private static partial Regex InputFormatRegex();

    [GeneratedRegex(@"^\[vist#0:\d+/[^\]]+\] (?:\[dec:[^\]]+\] )?Decoder returned EOF, finishing$")]
    private static partial Regex VideoEndOfFileRegex();

    [GeneratedRegex(@"^(?:\[[^\]\r\n]+\]\s*)*(?:File ended prematurely|(?:stream \d+, offset 0x[0-9a-f]+: )?partial file)\.?$", RegexOptions.IgnoreCase)]
    private static partial Regex PrematureEndRegex();

    // Read failures as ffmpeg prints them, anchored to the diagnostic so input paths and titles cannot match:
    // "[in#0/matroska,webm @ 0x1] Read error at pos. 190103455 (0xb54c99f)",
    // "[in#0/matroska,webm @ 0x1] Error during demuxing: Input/output error" and
    // "[in#0 @ 0x1] Error opening input: No such file or directory".
    [GeneratedRegex(@"^(?:\[[^\]\r\n]+\]\s*)*(?:Error during demuxing: (?!End of file\b).+|Read error at pos\. \d+.*|Error opening input(?: files?)?: (?:Input/output error|No such file or directory|Stale file handle|Permission denied|Operation not permitted|Device not configured)|\[[^\]\r\n]+ @ 0x[0-9a-f]+\]\s*(?:Error (?:reading|seeking)[^:]*|Seek[^:]*|Read[^:]*): (?:Input/output error|Stale file handle))\.?$", RegexOptions.IgnoreCase)]
    private static partial Regex InputFailureRegex();

    // ffmpeg's final statistics: "Input stream #0:0 (video): 96 packets read (123 bytes); 96 frames decoded; ...".
    [GeneratedRegex(@"\b(\d+) frames decoded")]
    private static partial Regex FramesDecodedRegex();

    // ffmpeg's final statistics: "Output stream #0:0 (video): 96 frames encoded; 96 packets muxed (123 bytes);".
    [GeneratedRegex(@"\b(\d+) frames encoded")]
    private static partial Regex FramesEncodedRegex();

    // ffmpeg's progress: "frame=   96 fps=...".
    [GeneratedRegex(@"\bframe=\s*(\d+)")]
    private static partial Regex ProgressFramesRegex();

    // The output stream or encoder an ffmpeg line is about: "[vost#0:0/av1_nvenc @ 0x0]" or "[enc:av1_nvenc @ 0x0]".
    [GeneratedRegex(@"\[(?:vost#\d+:\d+/|enc:)([^ \]]+) @")]
    private static partial Regex StreamTagRegex();

    [GeneratedRegex(@"^\[(?:vost#\d+:\d+/|enc:)?[a-z0-9]+_(?:nvenc|cuvid) @ [^\]]+\]", RegexOptions.IgnoreCase)]
    private static partial Regex HardwareCapabilityLogRegex();

    [GeneratedRegex(@"^(?:Codec not supported|10 bit encode not supported|YUV(?:444|422)P not supported|unsupported bit depth: \d+|Codec \w+ is not supported with this chroma format\.|Bit depth \d+ with this chroma format is not supported\.)$", RegexOptions.IgnoreCase)]
    private static partial Regex UnsupportedFormatMessageRegex();

    [GeneratedRegex(@"-c:v \S+_(qsv|cuvid|rkmpp|v4l2m2m)\b")]
    private static partial Regex HardwareDecoderRegex();

    // VideoToolbox tone-maps inside scale_vt when it converts to BT.709.
    [GeneratedRegex(@"(tonemap_[a-z]+|tonemapx|libplacebo)[^,""]*|scale_vt=[^,""]*color_transfer=bt709")]
    private static partial Regex TonemapFilterRegex();
}
