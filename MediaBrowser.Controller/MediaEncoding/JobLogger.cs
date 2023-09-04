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
                    while (!reader.EndOfStream && reader.BaseStream.CanRead)
                    {
                        var line = await reader.ReadLineAsync().ConfigureAwait(false);

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

                if (string.Equals(part, "fps=", StringComparison.OrdinalIgnoreCase) &&
                    (i + 1 < parts.Length))
                {
                    var rate = parts[i + 1];

                    if (float.TryParse(rate, CultureInfo.InvariantCulture, out var val))
                    {
                        framerate = val;
                    }
                }
                else if (part.StartsWith("fps=", StringComparison.OrdinalIgnoreCase))
                {
                    var rate = part.Split('=', 2)[^1];

                    if (float.TryParse(rate, CultureInfo.InvariantCulture, out var val))
                    {
                        framerate = val;
                    }
                }
                else if (state.RunTimeTicks.HasValue &&
                    part.StartsWith("time=", StringComparison.OrdinalIgnoreCase))
                {
                    var time = part.Split('=', 2)[^1];

                    if (TimeSpan.TryParse(time, CultureInfo.InvariantCulture, out var val))
                    {
                        var currentMs = startMs + val.TotalMilliseconds;

                        percent = 100.0 * currentMs / totalMs;

                        transcodingPosition = TimeSpan.FromMilliseconds(currentMs);
                    }
                }
                else if (part.StartsWith("size=", StringComparison.OrdinalIgnoreCase))
                {
                    var size = part.Split('=', 2)[^1];

                    int? scale = null;
                    if (size.Contains("kb", StringComparison.OrdinalIgnoreCase))
                    {
                        scale = 1024;
                        size = size.Replace("kb", string.Empty, StringComparison.OrdinalIgnoreCase);
                    }

                    if (scale.HasValue)
                    {
                        if (long.TryParse(size, CultureInfo.InvariantCulture, out var val))
                        {
                            bytesTranscoded = val * scale.Value;
                        }
                    }
                }
                else if (part.StartsWith("bitrate=", StringComparison.OrdinalIgnoreCase))
                {
                    var rate = part.Split('=', 2)[^1];

                    int? scale = null;
                    if (rate.Contains("kbits/s", StringComparison.OrdinalIgnoreCase))
                    {
                        scale = 1024;
                        rate = rate.Replace("kbits/s", string.Empty, StringComparison.OrdinalIgnoreCase);
                    }

                    if (scale.HasValue)
                    {
                        if (float.TryParse(rate, CultureInfo.InvariantCulture, out var val))
                        {
                            bitRate = (int)Math.Ceiling(val * scale.Value);
                        }
                    }
                }
            }

            if (framerate.HasValue || percent.HasValue)
            {
                state.ReportTranscodingProgress(transcodingPosition, framerate, percent, bytesTranscoded, bitRate);
            }
        }
    }
}
