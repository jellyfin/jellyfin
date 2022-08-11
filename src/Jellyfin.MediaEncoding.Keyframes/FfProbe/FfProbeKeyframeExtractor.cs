using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Jellyfin.MediaEncoding.Keyframes.FfProbe;

/// <summary>
/// FfProbe based keyframe extractor.
/// </summary>
public static class FfProbeKeyframeExtractor
{
    private const string DefaultArguments = "-v error -skip_frame nokey -show_entries format=duration -show_entries stream=duration -show_entries packet=pts_time,flags -select_streams v -of csv \"{0}\"";

    /// <summary>
    /// Extracts the keyframes using the ffprobe executable at the specified path.
    /// </summary>
    /// <param name="ffProbePath">The path to the ffprobe executable.</param>
    /// <param name="filePath">The file path.</param>
    /// <returns>An instance of <see cref="KeyframeData"/>.</returns>
    public static KeyframeData GetKeyframeData(string ffProbePath, string filePath)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ffProbePath,
                Arguments = string.Format(CultureInfo.InvariantCulture, DefaultArguments, filePath),

                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,

                WindowStyle = ProcessWindowStyle.Hidden,
                ErrorDialog = false,
            },
            EnableRaisingEvents = true
        };

        process.Start();

        return ParseStream(process.StandardOutput);
    }

    internal static KeyframeData ParseStream(StreamReader reader)
    {
        var keyframes = new List<long>();
        double streamDuration = 0;
        double formatDuration = 0;

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine().AsSpan();
            if (line.IsEmpty)
            {
                continue;
            }

            var firstComma = line.IndexOf(',');
            var lineType = line[..firstComma];
            var rest = line[(firstComma + 1)..];
            if (lineType.Equals("packet", StringComparison.OrdinalIgnoreCase))
            {
                if (rest.EndsWith(",K_"))
                {
                    // Trim the flags from the packet line. Example line: packet,7169.079000,K_
                    var keyframe = double.Parse(rest[..^3], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                    // Have to manually convert to ticks to avoid rounding errors as TimeSpan is only precise down to 1 ms when converting double.
                    keyframes.Add(Convert.ToInt64(keyframe * TimeSpan.TicksPerSecond));
                }
            }
            else if (lineType.Equals("stream", StringComparison.OrdinalIgnoreCase))
            {
                if (double.TryParse(rest, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var streamDurationResult))
                {
                    streamDuration = streamDurationResult;
                }
            }
            else if (lineType.Equals("format", StringComparison.OrdinalIgnoreCase))
            {
                if (double.TryParse(rest, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var formatDurationResult))
                {
                    formatDuration = formatDurationResult;
                }
            }
        }

        // Prefer the stream duration as it should be more accurate
        var duration = streamDuration > 0 ? streamDuration : formatDuration;

        return new KeyframeData(TimeSpan.FromSeconds(duration).Ticks, keyframes);
    }
}
