using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests
{
    public sealed class TranscodingThrottlerTests : IDisposable
    {
        private readonly Mock<IConfigurationManager> _configManager;
        private readonly Mock<IMediaEncoder> _mediaEncoder;
        private readonly TranscodingJob _job;
        private readonly TranscodingThrottler _throttler;

        public TranscodingThrottlerTests()
        {
            _configManager = new Mock<IConfigurationManager>();
            _mediaEncoder = new Mock<IMediaEncoder>();
            _mediaEncoder.Setup(m => m.IsPkeyPauseSupported).Returns(true);

            _job = new TranscodingJob(new NullLogger<TranscodingJob>())
            {
                Path = "/fake/path.ts",
                HasExited = false
            };

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "echo",
                    RedirectStandardInput = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            _job.Process = process;

            _throttler = new TranscodingThrottler(
                _job,
                new NullLogger<TranscodingThrottler>(),
                _configManager.Object,
                new Mock<IFileSystem>().Object,
                _mediaEncoder.Object);
        }

        public void Dispose()
        {
            _throttler.Dispose();
            _job.Dispose();
        }

        private void SetupThrottlingEnabled(int thresholdSeconds = 60)
        {
            _configManager
                .Setup(c => c.GetConfiguration("encoding"))
                .Returns(new EncodingOptions
                {
                    EnableThrottling = true,
                    ThrottleDelaySeconds = thresholdSeconds
                });
        }

        [Fact]
        public void DoesNotPause_WhenClientIsWithinThreshold()
        {
            SetupThrottlingEnabled(60);
            var transcodingPosition = TimeSpan.FromMinutes(5).Ticks;
            _job.TranscodingPositionTicks = transcodingPosition;
            _job.DownloadPositionTicks = transcodingPosition - TimeSpan.FromSeconds(30).Ticks;

            _throttler.Start();
            Thread.Sleep(6000);

            Assert.False(_job.HasExited);
        }

        [Fact]
        public void Pauses_WhenClientIsBeyondThreshold()
        {
            SetupThrottlingEnabled(60);
            var transcodingPosition = TimeSpan.FromMinutes(5).Ticks;
            _job.TranscodingPositionTicks = transcodingPosition;
            _job.DownloadPositionTicks = transcodingPosition - TimeSpan.FromMinutes(2).Ticks;

            _throttler.Start();
            Thread.Sleep(6000);

            Assert.False(_job.HasExited);
        }

        [Fact]
        public async Task OverlappingTimerCallbacks_DoNotCauseErrors()
        {
            SetupThrottlingEnabled(60);
            _job.TranscodingPositionTicks = TimeSpan.FromMinutes(5).Ticks;
            _job.DownloadPositionTicks = 0;

            _throttler.Start();
            await Task.Delay(12000);

            Assert.False(_job.HasExited);
        }

        [Fact]
        public async Task Unpauses_WhenDownloadPositionMovesWithinThreshold()
        {
            SetupThrottlingEnabled(60);
            var transcodingPosition = TimeSpan.FromMinutes(5).Ticks;
            _job.TranscodingPositionTicks = transcodingPosition;
            _job.DownloadPositionTicks = transcodingPosition - TimeSpan.FromMinutes(3).Ticks;

            _throttler.Start();
            await Task.Delay(6000);

            _job.DownloadPositionTicks = transcodingPosition - TimeSpan.FromSeconds(10).Ticks;
            await Task.Delay(6000);

            Assert.False(_job.HasExited);
        }
    }
}
