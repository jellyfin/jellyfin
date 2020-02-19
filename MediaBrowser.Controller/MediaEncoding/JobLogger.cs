using System;
using System.Globalization;
using System.IO;
using System.Linq;
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

        public async Task StartStreamingLog(EncodingJobInfo state, Stream source, Stream target)
        {
            try
            {
                using (target)
                using (var reader = new StreamReader(source))
                {
                    while (!reader.EndOfStream && reader.BaseStream.CanRead)
                    {
                        var line = await reader.ReadLineAsync().ConfigureAwait(false);

                        var (framerate, transcodingPosition, bytesTranscoded, bitRate) = ParseLogLine(line);

                        double? percent = null;
                        if (transcodingPosition.HasValue)
                        {
                            var startMs = state.BaseRequest.StartTimeTicks.HasValue
                                ? TimeSpan.FromTicks(state.BaseRequest.StartTimeTicks.Value).TotalMilliseconds
                                : 0;

                            var totalMs = state.RunTimeTicks.HasValue
                                ? TimeSpan.FromTicks(state.RunTimeTicks.Value).TotalMilliseconds
                                : 0;

                            var currentMs = startMs + transcodingPosition.Value.TotalMilliseconds;
                            percent = 100.0 * currentMs / totalMs;
                        }

                        if (framerate.HasValue || percent.HasValue)
                        {
                            state.ReportTranscodingProgress(transcodingPosition, framerate, percent, bytesTranscoded, bitRate);
                        }

                        var bytes = Encoding.UTF8.GetBytes(Environment.NewLine + line);

                        // If ffmpeg process is closed, the state is disposed, so don't write to target in that case
                        if (!target.CanWrite)
                        {
                            break;
                        }

                        await target.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);

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

        internal static (float? framerate,TimeSpan? transcodingPosition, long? bytesTranscoded, int? bitRate) ParseLogLine(ReadOnlySpan<char> logLine)
        {
            float? framerate = null;
            TimeSpan? transcodingPosition = null;
            long? bytesTranscoded = null;
            int? bitRate = null;

            var val = GetParameterValue(logLine, "fps=");
            if (float.TryParse(val, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var f)) { framerate = f; }

            val = GetParameterValue(logLine, "time=");
            if (TimeSpan.TryParse(val, NumberFormatInfo.InvariantInfo, out var ts)) { transcodingPosition = ts;}

            val = GetParameterValue(logLine, "size=");
            var idx = val.IndexOf("kb");
            if (idx > 0)
            {
                val = val.Slice(0,idx);
                if (long.TryParse(val, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var sz)) { bytesTranscoded = sz * 1024; }
            }

            val = GetParameterValue(logLine, "bitrate=");
            idx = val.IndexOf("kbit/s");
            if (idx > 0)
            {
                val = val.Slice(0,idx);
                if (float.TryParse(val, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var br)) { bitRate = (int)Math.Ceiling(br * 1000); }
            }

            return (framerate, transcodingPosition, bytesTranscoded, bitRate);
        }

        private static ReadOnlySpan<char> GetParameterValue(ReadOnlySpan<char> logLine, ReadOnlySpan<char> parameterKey)
        {
            var idx = logLine.IndexOf(parameterKey, StringComparison.OrdinalIgnoreCase);
            if(idx < 0)
                return null;

            var rest = logLine.Slice(idx + parameterKey.Length).TrimStart();
            idx = rest.IndexOf(' ');
            if(idx < 0)
                return null;

            return rest.Slice(0,idx);
        }
    }
}
