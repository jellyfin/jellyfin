using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaBrowser.MediaEncoding.Encoder
{
    public class JobLogger
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private readonly ILogger _logger;

        public JobLogger(ILogger logger)
        {
            _logger = logger;
        }

        public async void StartStreamingLog(EncodingJob transcodingJob, Stream source, Stream target)
        {
            try
            {
                using (var reader = new StreamReader(source))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync().ConfigureAwait(false);

                        ParseLogLine(line, transcodingJob);

                        var bytes = Encoding.UTF8.GetBytes(Environment.NewLine + line);

                        await target.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error reading ffmpeg log", ex);
            }
        }

        private void ParseLogLine(string line, EncodingJob transcodingJob)
        {
            float? framerate = null;
            double? percent = null;
            TimeSpan? transcodingPosition = null;
            long? bytesTranscoded = null;

            var parts = line.Split(' ');

            var totalMs = transcodingJob.RunTimeTicks.HasValue
                ? TimeSpan.FromTicks(transcodingJob.RunTimeTicks.Value).TotalMilliseconds
                : 0;

            var startMs = transcodingJob.Options.StartTimeTicks.HasValue
                ? TimeSpan.FromTicks(transcodingJob.Options.StartTimeTicks.Value).TotalMilliseconds
                : 0;

            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];

                if (string.Equals(part, "fps=", StringComparison.OrdinalIgnoreCase) &&
                    (i + 1 < parts.Length))
                {
                    var rate = parts[i + 1];
                    float val;

                    if (float.TryParse(rate, NumberStyles.Any, _usCulture, out val))
                    {
                        framerate = val;
                    }
                }
                else if (transcodingJob.RunTimeTicks.HasValue &&
                    part.StartsWith("time=", StringComparison.OrdinalIgnoreCase))
                {
                    var time = part.Split(new[] { '=' }, 2).Last();
                    TimeSpan val;

                    if (TimeSpan.TryParse(time, _usCulture, out val))
                    {
                        var currentMs = startMs + val.TotalMilliseconds;

                        var percentVal = currentMs / totalMs;
                        percent = 100 * percentVal;

                        transcodingPosition = val;
                    }
                }
                else if (part.StartsWith("size=", StringComparison.OrdinalIgnoreCase))
                {
                    var size = part.Split(new[] { '=' }, 2).Last();

                    int? scale = null;
                    if (size.IndexOf("kb", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        scale = 1024;
                        size = size.Replace("kb", string.Empty, StringComparison.OrdinalIgnoreCase);
                    }

                    if (scale.HasValue)
                    {
                        long val;

                        if (long.TryParse(size, NumberStyles.Any, _usCulture, out val))
                        {
                            bytesTranscoded = val * scale.Value;
                        }
                    }
                }
            }

            if (framerate.HasValue || percent.HasValue)
            {
                transcodingJob.ReportTranscodingProgress(transcodingPosition, framerate, percent, bytesTranscoded);
            }
        }
    }
}
