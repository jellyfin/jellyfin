using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.TranscodeTestDtos;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    /// <summary>
    /// Pins the stress test's behaviour that the stall fix must not change.
    /// </summary>
    public class TranscodeTestPreservationTests
    {
        private static readonly string?[] _otherEncoders = ["h264_videotoolbox", "hevc_videotoolbox", "h264_qsv", "hevc_vaapi", "av1_amf", "libx264", "libx265", null, string.Empty];

        private static readonly string[] _evidence =
        [
            string.Empty,
            "[h264_nvenc @ 0x0] OpenEncodeSessionEx failed: out of memory (10): (no details)",
            "[hevc_nvenc @ 0x0] OpenEncodeSessionEx failed: incompatible client key (21): (no details)",
            "Conversion failed!",
            "[vost#0:0/h264_qsv @ 0x0] Task finished with error code: -22 (Invalid argument)",
        ];

        private static readonly string[] _lines =
        [
            "frame=  120 fps= 60 q=-0.0 size=N/A time=00:00:05.00 bitrate=N/A speed=2.5x",
            "frame=abc",
            "Input #0, matroska,webm, from 'a.mkv':",
            "[h264 @ 0x0] Error while decoding stream #0:0",
            "OpenEncodeSessionEx failed: out of memory (10)",
            "Invalid argument",
            "   ",
            string.Empty,
            "Stream mapping:",
        ];

        [Property]
        public Property IsNvencSessionLimit_IsFalseForEveryOtherEncoder(byte encoder, byte evidence, string? extra)
        {
            var errors = _evidence[evidence % _evidence.Length] + (extra ?? string.Empty);
            return (!TranscodeTestRules.IsNvencSessionLimit(_otherEncoders[encoder % _otherEncoders.Length], errors)).ToProperty();
        }

        [Fact]
        public void SurveyFailureReason_MatchesTodaysNames()
        {
            var expected = new Dictionary<TranscodeTestEndReason, TranscodeTestSurveyFailureReason?>
            {
                [TranscodeTestEndReason.None] = null,
                [TranscodeTestEndReason.LimitReached] = TranscodeTestSurveyFailureReason.Performance,
                [TranscodeTestEndReason.MaxStreams] = null,
                [TranscodeTestEndReason.StreamFailed] = null,
                [TranscodeTestEndReason.StreamStalled] = TranscodeTestSurveyFailureReason.FailedTimeout,
                [TranscodeTestEndReason.NvencSessionLimit] = TranscodeTestSurveyFailureReason.FailedNvencLimit,
                [TranscodeTestEndReason.Cancelled] = null,
                [TranscodeTestEndReason.Error] = null,
                [TranscodeTestEndReason.GpuMemoryFull] = null,
                [TranscodeTestEndReason.NvencSessionRefused] = TranscodeTestSurveyFailureReason.FailedNvencLimit,
            };

            foreach (var reason in Enum.GetValues<TranscodeTestEndReason>())
            {
                Assert.Equal(expected[reason], TranscodeTestRules.SurveyFailureReason(reason));
            }
        }

        [Property]
        public Property LastError_KeepsTheLastWholeErrorLines(byte[] picks, string?[] extra)
        {
            var lines = new List<string>();
            for (var i = 0; i < picks.Length; i++)
            {
                lines.Add(_lines[picks[i] % _lines.Length]);
                if (i < extra.Length)
                {
                    lines.Add(extra[i] ?? string.Empty);
                }
            }

            using var process = new Process();
            var stream = new TranscodeTestStream(1, process, "/tmp", "p_", 24f, 0, 6);
            var now = 0.0;
            foreach (var line in lines)
            {
                stream.OnLine(line, now += 0.1);
            }

            return (stream.LastError == ReferenceLastError(lines)).Label("LastError matches the reference rule");
        }

        // The rule, written out independently: progress lines are skipped, and the last four whole lines that look like
        // errors are joined with " | ", leaving out a line identical to the one before it.
        private static string ReferenceLastError(IEnumerable<string> lines)
        {
            var kept = new List<string>();
            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"^frame=\s*(\d+)");
                if (match.Success && long.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                {
                    continue;
                }

                if ((line.Contains("rror", StringComparison.Ordinal) || line.Contains("ailed", StringComparison.Ordinal) || line.Contains("nvalid", StringComparison.Ordinal))
                    && (kept.Count == 0 || kept[^1] != line.Trim()))
                {
                    kept.Add(line.Trim());
                }
            }

            return string.Join(" | ", kept.TakeLast(4));
        }
    }
}
