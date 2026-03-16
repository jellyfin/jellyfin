using System;
using System.Globalization;
using System.Text;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Builds the FFmpeg argument string for frame-accurate clip extraction.
/// Pure function — no external dependencies.
/// </summary>
internal static class ClipFfmpegArgsBuilder
{
    /// <summary>
    /// Builds the FFmpeg argument string for a clip extraction job.
    /// -ss before -i for fast input seek; -t for duration limit.
    /// Resolution kept native (scale=trunc(iw/2)*2:trunc(ih/2)*2 ensures even dimensions).
    /// Bitrate matches source.
    /// </summary>
    /// <param name="options">All parameters for the FFmpeg clip command.</param>
    /// <returns>The complete FFmpeg argument string.</returns>
    internal static string Build(ClipFfmpegOptions options)
    {
        var sb = new StringBuilder();

        sb.AppendFormat(CultureInfo.InvariantCulture, "-ss {0:F6} ", options.StartSeconds);
        sb.AppendFormat(CultureInfo.InvariantCulture, "-i file:\"{0}\" ", options.InputPath);
        sb.AppendFormat(CultureInfo.InvariantCulture, "-t {0:F6} ", options.DurationSeconds);
        sb.AppendFormat(CultureInfo.InvariantCulture, "-map 0:v:0 -map 0:a:{0} -map_metadata -1 -map_chapters -1 ", options.AudioStreamIndex);
        sb.AppendFormat(CultureInfo.InvariantCulture, "-c:v {0} ", options.VideoEncoder);

        if (options.VideoEncoder is "libx264" or "libx265")
        {
            sb.Append("-preset superfast ");
        }
        else if (options.VideoEncoder is "libsvtav1")
        {
            sb.Append("-preset 8 ");
        }

        if (options.VideoBitRate.HasValue && options.VideoBitRate.Value > 0)
        {
            var br = options.VideoBitRate.Value;
            if (options.VideoEncoder is "libx264" or "libx265")
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "-b:v {0} -maxrate {0} -bufsize {1} ", br, br * 2);
            }
            else
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "-b:v {0} ", br);
            }
        }

        // Keep native resolution — only force even dimensions (required by most encoders)
        sb.Append("-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" ");

        sb.AppendFormat(CultureInfo.InvariantCulture, "-c:a {0} ", options.AudioEncoder);
        if (options.AudioBitRate.HasValue && options.AudioBitRate.Value > 0)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "-b:a {0} ", options.AudioBitRate.Value);
        }

        if (string.Equals(options.Container, "mp4", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append("-movflags +faststart ");
        }

        sb.Append("-y ");
        sb.AppendFormat(CultureInfo.InvariantCulture, "file:\"{0}\"", options.OutputPath);

        return sb.ToString();
    }
}
