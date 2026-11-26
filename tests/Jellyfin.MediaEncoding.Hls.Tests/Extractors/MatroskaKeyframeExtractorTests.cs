using System;
using System.Collections.Generic;
using Jellyfin.MediaEncoding.Hls.Extractors;
using Jellyfin.MediaEncoding.Keyframes;
using Xunit;

namespace Jellyfin.MediaEncoding.Hls.Tests.Extractors
{
    public class MatroskaKeyframeExtractorTests
    {
        [Fact]
        public void HasPlausibleKeyframeInterval_DensePerFrameCues_ReturnsFalse()
        {
            // Simulate 23.976 fps per-frame cues (~41.7 ms apart) without CueReference
            var ticks = new List<long>(20);
            for (var i = 0; i < 20; i++)
            {
                ticks.Add(TimeSpan.FromMilliseconds(i * 41.7).Ticks);
            }

            var data = new KeyframeData(TimeSpan.FromSeconds(10).Ticks, ticks);

            var result = MatroskaKeyframeExtractor.HasPlausibleKeyframeInterval(data, out var averageIntervalTicks);

            Assert.False(result);
            Assert.True(averageIntervalTicks < TimeSpan.FromMilliseconds(250).Ticks);
        }

        [Fact]
        public void HasPlausibleKeyframeInterval_NormalKeyframes_ReturnsTrue()
        {
            // Simulate normal keyframes 5 seconds apart
            var ticks = new List<long>(15);
            for (var i = 0; i < 15; i++)
            {
                ticks.Add(TimeSpan.FromSeconds(i * 5).Ticks);
            }

            var data = new KeyframeData(TimeSpan.FromSeconds(75).Ticks, ticks);

            var result = MatroskaKeyframeExtractor.HasPlausibleKeyframeInterval(data, out _);

            Assert.True(result);
        }

        [Fact]
        public void HasPlausibleKeyframeInterval_FewCues_ReturnsTrue()
        {
            var ticks = new long[] { 0, TimeSpan.FromMilliseconds(42).Ticks, TimeSpan.FromMilliseconds(84).Ticks };
            var data = new KeyframeData(TimeSpan.FromSeconds(1).Ticks, ticks);

            var result = MatroskaKeyframeExtractor.HasPlausibleKeyframeInterval(data, out _);

            Assert.True(result);
        }
    }
}
