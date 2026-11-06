using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.TranscodeTestDtos;

namespace Jellyfin.Api.Tests.Helpers
{
    /// <summary>
    /// Pins how a stress test ends for every ending other than the session limit, full GPU memory or the newest stream
    /// exiting early: the same reason, flags and error text as before those causes were named.
    /// </summary>
    public class TranscodeTestEndPreservationTests
    {
        internal static readonly string?[] Encoders = ["h264_nvenc", "hevc_nvenc", "av1_nvenc", "h264_videotoolbox", "h264_qsv", "hevc_vaapi", "libx264", null];

        internal static readonly string[] Pool =
        [
            "[h264_nvenc @ 0x0] OpenEncodeSessionEx failed: out of memory (10): (no details)",
            "[hevc_nvenc @ 0x0] OpenEncodeSessionEx failed: incompatible client key (21): (no details)",
            TranscodeTestEndClassificationTests.CudaLine,
            "[hevc @ 0x0] Failed to create surface: 2 (resource allocation failed).",
            "[h264_qsv @ 0x0] Error initializing the decoder: failed to allocate memory (-4)",
            "[Parsed_libplacebo_0 @ 0x0] vkAllocateMemory failed: VK_ERROR_OUT_OF_DEVICE_MEMORY",
            "[h264_nvenc @ 0x0] CreateInputBuffer failed: out of memory (10) CUDA_ERROR_OUT_OF_MEMORY",
            "[in#0 @ 0x0] Error opening input: Cannot allocate memory",
            "[Parsed_libplacebo_0 @ 0x0] vkCreateDevice failed: VK_ERROR_OUT_OF_HOST_MEMORY",
            "[h264_qsv @ 0x0] Error initializing the decoder: failed to allocate memory (-5)",
            "Conversion failed!",
            "[h264 @ 0x0] Error while decoding stream #0:0",
            "[vf#0:0 @ 0x0] Error while filtering: Generic error in an external library " + new string('x', 420),
            "frame=    0 fps=0.0 q=0.0 size=N/A time=N/A bitrate=N/A speed=N/A",
            "frame=  120 fps= 60 q=-0.0 size=N/A time=00:00:05.00 bitrate=N/A speed=2.5x",
            "Stream mapping:",
            "   ",
            string.Empty,
        ];

        // GPU memory patterns, written out independently from the classifier.
        private static readonly string[] _gpuMemoryPatterns = ["CUDA_ERROR_OUT_OF_MEMORY", "(resource allocation failed)", "failed to allocate memory (-4)", "VK_ERROR_OUT_OF_DEVICE_MEMORY"];

        [Property(MaxTest = 1000)]
        public Property EveryOtherEnding_IsUnchanged(byte[] picks, bool exited, byte encoder, byte maxStable, bool isNewest)
        {
            var (enc, lines, k) = Input(picks, encoder, maxStable);
            if (IsNamedCauseEnding(exited, enc, lines, k, isNewest))
            {
                return true.ToProperty();
            }

            var actual = TranscodeTestEndClassificationTests.Classify(exited, enc, lines, k, isNewest);
            var expected = EndStressBeforeNamedCauses(exited, enc, lines, k, isNewest);
            return (actual == expected).Label($"expected {expected}, got {actual}");
        }

        [Property(MaxTest = 1000)]
        public Property EveryEnding_UsesOnlySurveyNamesAndNoNvencResultWithoutNvenc(byte[] picks, bool exited, byte encoder, byte maxStable, bool isNewest)
        {
            var (enc, lines, k) = Input(picks, encoder, maxStable);
            var (reason, _, _) = TranscodeTestEndClassificationTests.Classify(exited, enc, lines, k, isNewest);
            var survey = TranscodeTestRules.SurveyFailureReason(reason);
            return (survey is null or TranscodeTestSurveyFailureReason.Performance or TranscodeTestSurveyFailureReason.FailedTimeout or TranscodeTestSurveyFailureReason.FailedNvencLimit).Label("survey name")
                .And(IsNvenc(enc) || reason != TranscodeTestEndReason.NvencSessionLimit).Label("no NVENC result without NVENC");
        }

        internal static (string? Encoder, string[] Lines, int MaxStable) Input(byte[] picks, byte encoder, byte maxStable)
            => (Encoders[encoder % Encoders.Length], picks.Take(30).Select(p => Pool[p % Pool.Length]).ToArray(), maxStable % 33);

        internal static bool IsNvenc(string? encoder) => encoder is not null && encoder.Contains("nvenc", StringComparison.OrdinalIgnoreCase);

        internal static bool SessionText(string line)
            => line.Contains("out of memory (10)", StringComparison.OrdinalIgnoreCase) || line.Contains("incompatible client key (21)", StringComparison.OrdinalIgnoreCase);

        internal static bool GpuMemoryLine(string line)
            => !SessionText(line) && _gpuMemoryPatterns.Any(p => line.Contains(p, StringComparison.OrdinalIgnoreCase));

        internal static bool ReportedFrames(IEnumerable<string> lines)
            => lines.Any(l => Regex.Match(l, @"^frame=\s*(\d+)") is { Success: true } m
                && long.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n >= 1);

        internal static bool IsSessionLimit(string? encoder, string[] lines) => IsNvenc(encoder) && lines.Any(SessionText);

        internal static bool IsGpuMemoryFull(string? encoder, string[] lines, int maxStable)
            => !IsSessionLimit(encoder, lines) && lines.Any(GpuMemoryLine) && maxStable >= 1;

        internal static bool IsEarlyExitOfNewest(bool exited, string? encoder, string[] lines, int maxStable, bool isNewest)
            => exited && isNewest && !ReportedFrames(lines) && maxStable >= 1
                && !IsSessionLimit(encoder, lines) && !IsGpuMemoryFull(encoder, lines, maxStable);

        // The endings the end classification names, over the raw lines: the session limit, full GPU memory or the newest stream exiting early.
        internal static bool IsNamedCauseEnding(bool exited, string? encoder, string[] lines, int maxStable, bool isNewest)
            => IsSessionLimit(encoder, lines) || IsGpuMemoryFull(encoder, lines, maxStable) || IsEarlyExitOfNewest(exited, encoder, lines, maxStable, isNewest);

        // How a run ended before those causes were named, written out independently, without the NVIDIA hint, which was removed
        // (a hang is never the session limit), with only ffmpeg's error lines as a stall's error, and with a stall after
        // the first frame no longer read as the server not starting the stream.
        private static (TranscodeTestEndReason Reason, bool CouldNotStart, string? Error) EndStressBeforeNamedCauses(bool exited, string? encoder, string[] lines, int maxStable, bool isNewest)
        {
            var stream = TranscodeTestEndClassificationTests.Feed(lines);
            if (exited)
            {
                var limit = IsNvenc(encoder) && SessionText(stream.LastError);
                return (limit ? TranscodeTestEndReason.NvencSessionLimit : TranscodeTestEndReason.StreamFailed, false, stream.LastError);
            }

            var error = string.IsNullOrEmpty(stream.LastError) ? null : stream.LastError;
            if (IsNvenc(encoder) && SessionText(stream.StallEvidence))
            {
                return (TranscodeTestEndReason.NvencSessionLimit, false, error);
            }

            return (TranscodeTestEndReason.StreamStalled, isNewest && maxStable >= 1 && !ReportedFrames(lines), error);
        }
    }
}
