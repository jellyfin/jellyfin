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
    /// <param name="inputPath">Absolute path to the source file.</param>
    /// <param name="outputPath">Absolute path to the output file.</param>
    /// <param name="startSeconds">Start offset in seconds.</param>
    /// <param name="durationSeconds">Duration in seconds.</param>
    /// <param name="videoEncoder">FFmpeg video encoder name (e.g. "libx264").</param>
    /// <param name="audioEncoder">FFmpeg audio encoder name (e.g. "aac").</param>
    /// <param name="videoBitRate">Source video bitrate in bps, or <c>null</c> to let FFmpeg decide.</param>
    /// <param name="audioBitRate">Source audio bitrate in bps, or <c>null</c> to use default.</param>
    /// <param name="container">Output container (e.g. "mp4", "webm").</param>
    /// <param name="audioStreamIndex">0-based audio-only stream index to map.</param>
    /// <returns>The complete FFmpeg argument string.</returns>
    internal static string Build(
        string inputPath,
        string outputPath,
        double startSeconds,
        double durationSeconds,
        string videoEncoder,
        string audioEncoder,
        int? videoBitRate,
        int? audioBitRate,
        string container,
        int audioStreamIndex)
    {
        var sb = new StringBuilder();

        sb.AppendFormat(CultureInfo.InvariantCulture, "-ss {0:F6} ", startSeconds);
        sb.AppendFormat(CultureInfo.InvariantCulture, "-i file:\"{0}\" ", inputPath);
        sb.AppendFormat(CultureInfo.InvariantCulture, "-t {0:F6} ", durationSeconds);
        sb.AppendFormat(CultureInfo.InvariantCulture, "-map 0:v:0 -map 0:a:{0} -map_metadata -1 -map_chapters -1 ", audioStreamIndex);
        sb.AppendFormat(CultureInfo.InvariantCulture, "-c:v {0} ", videoEncoder);

        if (videoEncoder is "libx264" or "libx265")
        {
            sb.Append("-preset superfast ");
        }
        else if (videoEncoder is "libsvtav1")
        {
            sb.Append("-preset 8 ");
        }

        if (videoBitRate.HasValue && videoBitRate.Value > 0)
        {
            var br = videoBitRate.Value;
            if (videoEncoder is "libx264" or "libx265")
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

        sb.AppendFormat(CultureInfo.InvariantCulture, "-c:a {0} ", audioEncoder);
        if (audioBitRate.HasValue && audioBitRate.Value > 0)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "-b:a {0} ", audioBitRate.Value);
        }

        if (string.Equals(container, "mp4", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append("-movflags +faststart ");
        }

        sb.Append("-y ");
        sb.AppendFormat(CultureInfo.InvariantCulture, "file:\"{0}\"", outputPath);

        return sb.ToString();
    }
}
