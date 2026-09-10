#nullable disable

#pragma warning disable CS1591

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.MediaEncoding
{
    public class JobLogger
    {
        private readonly ILogger _logger;

        public JobLogger(ILogger logger)
        {
            _logger = logger;
        }

        public async Task StartStreamingLog(EncodingJobInfo state, StreamReader reader, Stream target)
        {
            try
            {
                using (target)
                using (reader)
                {
                    string line = await reader.ReadLineAsync().ConfigureAwait(false);
                    while (line is not null && reader.BaseStream.CanRead)
                    {
                        ParseLogLine(line, state);

                        var bytes = Encoding.UTF8.GetBytes(Environment.NewLine + line);

                        // If ffmpeg process is closed, the state is disposed, so don't write to target in that case
                        if (!target.CanWrite)
                        {
                            break;
                        }

                        await target.WriteAsync(bytes).ConfigureAwait(false);

                        // Check again, the stream could have been closed
                        if (!target.CanWrite)
                        {
                            break;
                        }

                        await target.FlushAsync().ConfigureAwait(false);
                        line = await reader.ReadLineAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading ffmpeg log");
            }
        }

        private void ParseLogLine(string line, EncodingJobInfo state)
        {
            float? framerate = null;
            double? percent = null;
            TimeSpan? transcodingPosition = null;
            long? bytesTranscoded = null;
            int? bitRate = null;
            float? encodingSpeed = null;

            var parts = line.Split(' ');

            var totalMs = state.RunTimeTicks.HasValue
                ? TimeSpan.FromTicks(state.RunTimeTicks.Value).TotalMilliseconds
                : 0;

            var startMs = state.BaseRequest.StartTimeTicks.HasValue
                ? TimeSpan.FromTicks(state.BaseRequest.StartTimeTicks.Value).TotalMilliseconds
                : 0;

            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];

                if (part.StartsWith("fps=", StringComparison.OrdinalIgnoreCase))
                {
                    var rate = GetStatValue(parts, i);

                    if (float.TryParse(rate, CultureInfo.InvariantCulture, out var val))
                    {
                        framerate = val;
                    }
                }
                else if (state.RunTimeTicks.HasValue &&
                    part.StartsWith("time=", StringComparison.OrdinalIgnoreCase))
                {
                    var time = GetStatValue(parts, i);

                    if (TimeSpan.TryParse(time, CultureInfo.InvariantCulture, out var val))
                    {
                        var currentMs = startMs + val.TotalMilliseconds;

                        percent = 100.0 * currentMs / totalMs;

                        transcodingPosition = TimeSpan.FromMilliseconds(currentMs);
                    }
                }
                else if (part.StartsWith("size=", StringComparison.OrdinalIgnoreCase)
                    || part.StartsWith("Lsize=", StringComparison.OrdinalIgnoreCase))
                {
                    // The final summary line spells it "Lsize=".
                    bytesTranscoded = ParseSize(GetStatValue(parts, i)) ?? bytesTranscoded;
                }
                else if (part.StartsWith("bitrate=", StringComparison.OrdinalIgnoreCase))
                {
                    bitRate = ParseBitrate(GetStatValue(parts, i)) ?? bitRate;
                }
                else if (part.StartsWith("speed=", StringComparison.OrdinalIgnoreCase))
                {
                    encodingSpeed = ParseSpeed(GetStatValue(parts, i)) ?? encodingSpeed;
                }
            }

            if (framerate.HasValue || percent.HasValue)
            {
                state.ReportTranscodingProgress(transcodingPosition, framerate, percent, bytesTranscoded, bitRate, encodingSpeed);
            }
        }

        /// <summary>
        /// Reads the value belonging to the <c>key=</c> token at <paramref name="index"/>. ffmpeg
        /// right-pads its stats values ("size=       0KiB", "bitrate=   0.3kbits/s"), and the line
        /// is split on single spaces, so the number usually lands several empty tokens later.
        /// Scanning stops at the next <c>key=value</c> token so a genuinely absent value is not
        /// mistaken for the following field.
        /// </summary>
        private static string GetStatValue(string[] parts, int index)
        {
            var inline = parts[index].Split('=', 2)[^1];
            if (!string.IsNullOrEmpty(inline))
            {
                return inline;
            }

            for (var i = index + 1; i < parts.Length; i++)
            {
                var candidate = parts[i];
                if (string.IsNullOrEmpty(candidate))
                {
                    continue;
                }

                return candidate.Contains('=', StringComparison.Ordinal) ? string.Empty : candidate;
            }

            return string.Empty;
        }

        /// <summary>
        /// Parses an ffmpeg <c>size=</c> value into bytes. ffmpeg emits binary units (KiB/MiB/GiB,
        /// powers of 1024) by default and decimal units (kB/MB/GB, powers of 1000) in legacy
        /// formatting; these are different multipliers and are handled distinctly. Returns
        /// <see langword="null"/> for <c>N/A</c> (e.g. segmented HLS/DASH output) or unparseable input.
        /// </summary>
        private static long? ParseSize(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Contains("N/A", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Most specific suffixes first so "KiB" is not matched by the bare "B" rule.
            (string Suffix, double Scale)[] units =
            [
                ("KiB", 1024d),
                ("MiB", 1024d * 1024),
                ("GiB", 1024d * 1024 * 1024),
                ("kB", 1000d),
                ("MB", 1000d * 1000),
                ("GB", 1000d * 1000 * 1000),
                ("B", 1d)
            ];

            foreach (var (suffix, scale) in units)
            {
                if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    var number = value[..^suffix.Length];
                    return double.TryParse(number, CultureInfo.InvariantCulture, out var val)
                        ? (long)(val * scale)
                        : null;
                }
            }

            return long.TryParse(value, CultureInfo.InvariantCulture, out var raw) ? raw : null;
        }

        /// <summary>
        /// Parses an ffmpeg <c>bitrate=</c> value into bits per second. ffmpeg reports decimal-scaled
        /// rates (kbits/s = 1000 bit/s, Mbits/s = 1e6 bit/s). Returns <see langword="null"/> for
        /// <c>N/A</c> or unparseable input.
        /// </summary>
        private static int? ParseBitrate(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Contains("N/A", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            (string Suffix, double Scale)[] units =
            [
                ("kbits/s", 1000d),
                ("Mbits/s", 1000d * 1000),
                ("Gbits/s", 1000d * 1000 * 1000),
                ("bits/s", 1d)
            ];

            foreach (var (suffix, scale) in units)
            {
                if (value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    var number = value[..^suffix.Length];
                    if (!float.TryParse(number, CultureInfo.InvariantCulture, out var val))
                    {
                        return null;
                    }

                    // Gbits/s rates can exceed int.MaxValue; saturate rather than wrap.
                    var scaled = Math.Ceiling(val * scale);
                    return scaled >= int.MaxValue ? int.MaxValue : (int)scaled;
                }
            }

            return null;
        }

        /// <summary>
        /// Parses an ffmpeg <c>speed=</c> value (e.g. <c>4.42x</c>) into a realtime multiplier.
        /// Returns <see langword="null"/> for <c>N/A</c> or unparseable input.
        /// </summary>
        private static float? ParseSpeed(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Contains("N/A", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var number = value.TrimEnd('x', 'X');
            return float.TryParse(number, CultureInfo.InvariantCulture, out var val) ? val : null;
        }
    }
}
