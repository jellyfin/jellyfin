using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Controller.Tests.MediaEncoding
{
    public class JobLoggerTests
    {
        [Theory]
        [InlineData("frame=  480 fps= 48 q=28.0 size=    2048kB time=00:00:20.00 bitrate= 838.9kbits/s speed=2.01x", 2.01)]
        [InlineData("frame=   24 fps= 12 q=28.0 size=     256kB time=00:00:01.00 bitrate=2097.2kbits/s speed=   1x", 1.0)]
        [InlineData("size=     512kB time=00:00:30.00 bitrate= 139.8kbits/s speed=0.512x", 0.512)]
        public async Task StartStreamingLog_ProgressLine_RecordsFfmpegSpeed(string line, double expected)
        {
            var state = await ParseAsync(line);

            Assert.Equal(expected, state.TranscodingSpeed);
        }

        [Fact]
        public async Task StartStreamingLog_SpeedNotAvailable_LeavesSpeedUnset()
        {
            var state = await ParseAsync("frame=    0 fps=0.0 q=0.0 size=       0kB time=00:00:00.00 bitrate=N/A speed=N/A");

            Assert.Null(state.TranscodingSpeed);
        }

        private static async Task<EncodingJobInfo> ParseAsync(string line)
        {
            var state = new EncodingJobInfo(TranscodingJobType.Hls)
            {
                BaseRequest = new BaseEncodingJobOptions(),
                RunTimeTicks = 6_000_000_000,
                Progress = new Progress<double>()
            };

            using var reader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(line)));
            await new JobLogger(NullLogger.Instance).StartStreamingLog(state, reader, new MemoryStream());

            return state;
        }
    }
}
