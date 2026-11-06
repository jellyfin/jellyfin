using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Jellyfin.Api.Models.TranscodeTestDtos;
using MediaBrowser.Model.Configuration;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// How the transcoding test reads ffmpeg command lines and output, and what it concludes from them.
/// </summary>
internal static partial class TranscodeTestRules
{
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
    /// ruled out, and ffmpeg said it got not a single frame through.
    /// </summary>
    /// <param name="referencePassed">Whether the H.264 8-bit check of the same kind passed earlier in this run.</param>
    /// <param name="otherHalfRuledOut">For decoding, whether the failed run encoded on the CPU; for encoding, always
    /// <c>true</c>, as it decodes on the CPU.</param>
    /// <param name="frames">The frames ffmpeg decoded or encoded, or <c>null</c> when it did not say.</param>
    /// <param name="resourceLine">The line in which ffmpeg said the GPU refused a session or ran out of memory, or
    /// null. Such a failure is never a missing format: the GPU was busy or full, for example with playback.</param>
    /// <returns><see cref="TranscodeTestOutcome.NotSupported"/> when the hardware lacks the format, otherwise
    /// <see cref="TranscodeTestOutcome.Failed"/>.</returns>
    public static TranscodeTestOutcome FailedOutcome(bool referencePassed, bool otherHalfRuledOut, int? frames, string? resourceLine = null)
        => referencePassed && otherHalfRuledOut && frames == 0 && resourceLine is null ? TranscodeTestOutcome.NotSupported : TranscodeTestOutcome.Failed;

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

        return (TranscodeTestOutcome.Failed, hardwareEncodingOn && codecAllowed ? TranscodeTestAdvice.TurnOff : TranscodeTestAdvice.None);
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
    /// Gets whether an ffmpeg encoder runs on the CPU.
    /// </summary>
    /// <param name="encoder">The encoder name.</param>
    /// <returns><c>true</c> for software encoders such as libx264.</returns>
    public static bool IsSoftwareEncoder(string encoder) => encoder.StartsWith("lib", StringComparison.Ordinal);

    [GeneratedRegex(@"graph [-0-9]+ input from stream 0:0 @ [^\]]+\] .*?pixfmt:([a-z0-9_]+)")]
    private static partial Regex InputFormatRegex();

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

    [GeneratedRegex(@"-c:v \S+_(qsv|cuvid|rkmpp|v4l2m2m)\b")]
    private static partial Regex HardwareDecoderRegex();

    // VideoToolbox tone-maps inside scale_vt when it converts to BT.709.
    [GeneratedRegex(@"(tonemap_[a-z]+|tonemapx|libplacebo)[^,""]*|scale_vt=[^,""]*color_transfer=bt709")]
    private static partial Regex TonemapFilterRegex();
}
