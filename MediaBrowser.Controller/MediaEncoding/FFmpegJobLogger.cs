using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.MediaEncoding
{
    public class FFmpegJobLogger
    {
        private readonly ILogger _logger;

        public FFmpegJobLogger(ILogger logger)
        {
            _logger = logger;
        }

        public async Task StartStreamingLog(Stream source, Stream target, Action<TimeSpan?, float?, long?, int?> progressCallback)
        {
            try
            {
                using (target)
                using (var reader = new StreamReader(source))
                {
                    while (!reader.EndOfStream && reader.BaseStream.CanRead)
                    {
                        var line = await reader.ReadLineAsync().ConfigureAwait(false);

                        if(line.StartsWith("frame="))
                        {
                            var (framerate, transcodingPosition, bytesTranscoded, bitRate) = ParseLogLine(line);
                            if (framerate.HasValue || transcodingPosition.HasValue)
                            {
                                progressCallback(transcodingPosition, framerate, bytesTranscoded, bitRate);
                            }
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

        internal static (float? framerate, TimeSpan? transcodingPosition, long? bytesTranscoded, int? bitRate) ParseLogLine(ReadOnlySpan<char> logLine)
        {
            float? framerate = null;
            TimeSpan? transcodingPosition = null;
            long? bytesTranscoded = null;
            int? bitRate = null;

            // Parameter search have to be in same order as they occur in FFmpeg progress log
            var val = GetParameterValue(ref logLine, "fps=");
            if (float.TryParse(val, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var f)) { framerate = f; }

            val = GetParameterValue(ref logLine, "time=");
            if (TimeSpan.TryParse(val, NumberFormatInfo.InvariantInfo, out var ts)) { transcodingPosition = ts;}

            val = GetParameterValue(ref logLine, "size=");
            var idx = val.IndexOf("kb");
            if (idx > 0)
            {
                val = val.Slice(0,idx);
                if (long.TryParse(val, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var sz)) { bytesTranscoded = sz * 1024; }
            }

            val = GetParameterValue(ref logLine, "bitrate=");
            idx = val.IndexOf("kbit/s");
            if (idx > 0)
            {
                val = val.Slice(0,idx);
                if (float.TryParse(val, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out var br)) { bitRate = (int)Math.Ceiling(br * 1000); }
            }

            return (framerate, transcodingPosition, bytesTranscoded, bitRate);
        }

        private static ReadOnlySpan<char> GetParameterValue(ref ReadOnlySpan<char> logLine, ReadOnlySpan<char> parameterKey)
        {
            var idx = logLine.IndexOf(parameterKey, StringComparison.OrdinalIgnoreCase);
            if(idx < 0)
            {
                return null;
            }

            var rest = logLine.Slice(idx + parameterKey.Length).TrimStart();
            idx = rest.IndexOf(' ');
            if(idx < 0)
            {
                return null;
            }

            logLine = rest.Slice(idx+1);

            return rest.Slice(0,idx);
        }
    }
}
