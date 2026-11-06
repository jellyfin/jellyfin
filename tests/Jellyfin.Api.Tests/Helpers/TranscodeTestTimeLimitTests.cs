using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.TranscodeTestDtos;
using Jellyfin.Extensions.Json;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    /// <summary>
    /// What bounds a stress test: its time limit, a dashboard that stopped checking on it, its bitrate and the space
    /// its segments take, and how its results are sent.
    /// </summary>
    public class TranscodeTestTimeLimitTests
    {
        private const double Threshold = 21.6;

        [Theory]
        [InlineData(1, 900)]
        [InlineData(16, 900)]
        [InlineData(20, 940)]
        [InlineData(64, 3008)]
        public void TimeLimitSeconds_IsFifteenMinutesOrEnoughForEveryStream(int maxStreams, double expected)
            => Assert.Equal(expected, TranscodeTestRules.TimeLimitSeconds(maxStreams));

        [Theory]
        [InlineData(3, 8, TranscodeTestEndReason.LimitReached)]
        [InlineData(0, 8, TranscodeTestEndReason.LimitReached)]
        [InlineData(8, 8, TranscodeTestEndReason.MaxStreams)]
        public void TimeLimitEndReason_KeepsTheKeptUpCount(int maxStable, int maxStreams, TranscodeTestEndReason expected)
            => Assert.Equal(expected, TranscodeTestRules.TimeLimitEndReason(maxStable, maxStreams));

        [Fact]
        public void Tick_StreamsThatKeepDippingBriefly_EndAtTheTimeLimit()
        {
            // Stream 2 dips for a second every three: never long enough to end the test, always too recent to add one.
            static TranscodeTestStreamReading[] Readings(double now)
                => [new(1, 30, now), new(2, now % 3 < 1 ? 20 : 30, now)];

            var unlimited = new TranscodeTestStressController(Threshold, 12, 5, 30, 8, 0, 3);
            var limited = new TranscodeTestStressController(Threshold, 12, 5, 30, 8, 0, 3, timeLimitSeconds: 900);
            TranscodeTestStressDecision? last = null;
            for (var now = 0.5; now < 1000; now += 0.5)
            {
                Assert.NotEqual(TranscodeTestStressPhase.Finished, unlimited.Tick(now, Readings(now)).Phase);
                last = limited.Tick(now, Readings(now));
                if (last.Phase == TranscodeTestStressPhase.Finished)
                {
                    Assert.Equal(900, now);
                    break;
                }
            }

            Assert.Equal(TranscodeTestStressAction.TimeLimit, last?.Action);
        }

        [Theory]
        [InlineData(0, 599, false)]
        [InlineData(0, 600, true)]
        [InlineData(400, 999, false)]
        public void IsAbandoned_AfterTenMinutesWithoutAStatusRequest(double lastCheckedAt, double now, bool expected)
            => Assert.Equal(expected, TranscodeTestRules.IsAbandoned(now, lastCheckedAt));

        [Fact]
        public void DescribeResult_SaysWhenTheTimeLimitEndedTheTest()
        {
            var text = TranscodeTestRules.DescribeResult(new TranscodeTestStressResult { MaxStableStreams = 3, EndReason = TranscodeTestEndReason.LimitReached, TimeLimitReached = true });

            Assert.StartsWith("Result: 3 stream(s) kept up with real time.", text, StringComparison.Ordinal);
            Assert.Contains("time limit", text, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(2_000_000, true)]
        [InlineData(20_000_000, true)]
        [InlineData(TranscodeTestRequestDto.MaxVideoBitrate, true)]
        [InlineData(TranscodeTestRequestDto.MaxVideoBitrate + 1, false)]
        [InlineData(200_000_000, false)]
        [InlineData(TranscodeTestRequestDto.MinVideoBitrate - 1, false)]
        public void VideoBitrate_IsCapped(int bitrate, bool valid)
        {
            var request = new TranscodeTestRequestDto { VideoBitrate = bitrate };
            var results = new List<ValidationResult>();

            Assert.Equal(valid, Validator.TryValidateObject(request, new ValidationContext(request), results, true));
        }

        [Fact]
        public void NeededSpace_CoversTheSegmentsEveryStreamKeeps()
        {
            // 16 streams of 8 Mbps video and 192 kbps audio, 5 segments of 6 s each, plus a tenth for muxing.
            Assert.Equal(540_672_000, TranscodeTestRules.NeededSpace(16, 8_000_000, 6));
        }

        [Fact]
        public void Results_SendTheStatusAsEnumNames()
        {
            var result = new TranscodeTestStressResult
            {
                Streams = [new TranscodeTestStreamResult { Id = 1, Status = TranscodeTestStreamStatus.Failed }]
            };

            var json = JsonSerializer.Serialize(result, JsonDefaults.Options);

            Assert.Contains("\"Status\":\"Failed\"", json, StringComparison.Ordinal);
            Assert.Contains("\"TimeLimitReached\":false", json, StringComparison.Ordinal);
        }
    }
}
