using System;
using AutoFixture;
using AutoFixture.AutoMoq;
using MediaBrowser.MediaEncoding.Subtitles;
using MediaBrowser.Model.MediaInfo;
using Xunit;

namespace Jellyfin.MediaEncoding.Subtitles.Tests
{
    public class FilterEventsTests
    {
        private readonly SubtitleEncoder _encoder;

        public FilterEventsTests()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
            _encoder = fixture.Create<SubtitleEncoder>();
        }

        [Fact]
        public void FilterEvents_SubtitleSpanningSegmentBoundary_IsRetained()
        {
            // Subtitle starts at 5s, ends at 15s.
            // Segment requested from 10s to 20s.
            // The subtitle is still on screen at 10s and should NOT be dropped.
            var track = new SubtitleTrackInfo
            {
                TrackEvents = new[]
                {
                    new SubtitleTrackEvent("1", "Still on screen")
                    {
                        StartPositionTicks = TimeSpan.FromSeconds(5).Ticks,
                        EndPositionTicks = TimeSpan.FromSeconds(15).Ticks
                    },
                    new SubtitleTrackEvent("2", "Next subtitle")
                    {
                        StartPositionTicks = TimeSpan.FromSeconds(12).Ticks,
                        EndPositionTicks = TimeSpan.FromSeconds(17).Ticks
                    }
                }
            };

            _encoder.FilterEvents(
                track,
                startPositionTicks: TimeSpan.FromSeconds(10).Ticks,
                endTimeTicks: TimeSpan.FromSeconds(20).Ticks,
                preserveTimestamps: true);

            Assert.Equal(2, track.TrackEvents.Count);
            Assert.Equal("1", track.TrackEvents[0].Id);
            Assert.Equal("2", track.TrackEvents[1].Id);
        }

        [Fact]
        public void FilterEvents_SubtitleFullyBeforeSegment_IsDropped()
        {
            // Subtitle starts at 2s, ends at 5s.
            // Segment requested from 10s.
            // The subtitle ended before the segment — should be dropped.
            var track = new SubtitleTrackInfo
            {
                TrackEvents = new[]
                {
                    new SubtitleTrackEvent("1", "Already gone")
                    {
                        StartPositionTicks = TimeSpan.FromSeconds(2).Ticks,
                        EndPositionTicks = TimeSpan.FromSeconds(5).Ticks
                    },
                    new SubtitleTrackEvent("2", "Visible")
                    {
                        StartPositionTicks = TimeSpan.FromSeconds(12).Ticks,
                        EndPositionTicks = TimeSpan.FromSeconds(17).Ticks
                    }
                }
            };

            _encoder.FilterEvents(
                track,
                startPositionTicks: TimeSpan.FromSeconds(10).Ticks,
                endTimeTicks: TimeSpan.FromSeconds(20).Ticks,
                preserveTimestamps: true);

            Assert.Single(track.TrackEvents);
            Assert.Equal("2", track.TrackEvents[0].Id);
        }

        [Fact]
        public void FilterEvents_SubtitleAfterSegment_IsDropped()
        {
            // Segment is 10s-20s, subtitle starts at 25s.
            var track = new SubtitleTrackInfo
            {
                TrackEvents = new[]
                {
                    new SubtitleTrackEvent("1", "In range")
                    {
                        StartPositionTicks = TimeSpan.FromSeconds(12).Ticks,
                        EndPositionTicks = TimeSpan.FromSeconds(15).Ticks
                    },
                    new SubtitleTrackEvent("2", "After segment")
                    {
                        StartPositionTicks = TimeSpan.FromSeconds(25).Ticks,
                        EndPositionTicks = TimeSpan.FromSeconds(30).Ticks
                    }
                }
            };

            _encoder.FilterEvents(
                track,
                startPositionTicks: TimeSpan.FromSeconds(10).Ticks,
                endTimeTicks: TimeSpan.FromSeconds(20).Ticks,
                preserveTimestamps: true);

            Assert.Single(track.TrackEvents);
            Assert.Equal("1", track.TrackEvents[0].Id);
        }

        [Fact]
        public void FilterEvents_PreserveTimestampsFalse_AdjustsTimestamps()
        {
            var track = new SubtitleTrackInfo
            {
                TrackEvents = new[]
                {
                    new SubtitleTrackEvent("1", "Subtitle")
                    {
                        StartPositionTicks = TimeSpan.FromSeconds(15).Ticks,
                        EndPositionTicks = TimeSpan.FromSeconds(20).Ticks
                    }
                }
            };

            _encoder.FilterEvents(
                track,
                startPositionTicks: TimeSpan.FromSeconds(10).Ticks,
                endTimeTicks: TimeSpan.FromSeconds(30).Ticks,
                preserveTimestamps: false);

            Assert.Single(track.TrackEvents);
            // Timestamps should be shifted back by 10s
            Assert.Equal(TimeSpan.FromSeconds(5).Ticks, track.TrackEvents[0].StartPositionTicks);
            Assert.Equal(TimeSpan.FromSeconds(10).Ticks, track.TrackEvents[0].EndPositionTicks);
        }

        [Fact]
        public void FilterEvents_PreserveTimestampsTrue_KeepsOriginalTimestamps()
        {
            var startTicks = TimeSpan.FromSeconds(15).Ticks;
            var endTicks = TimeSpan.FromSeconds(20).Ticks;

            var track = new SubtitleTrackInfo
            {
                TrackEvents = new[]
                {
                    new SubtitleTrackEvent("1", "Subtitle")
                    {
                        StartPositionTicks = startTicks,
                        EndPositionTicks = endTicks
                    }
                }
            };

            _encoder.FilterEvents(
                track,
                startPositionTicks: TimeSpan.FromSeconds(10).Ticks,
                endTimeTicks: TimeSpan.FromSeconds(30).Ticks,
                preserveTimestamps: true);

            Assert.Single(track.TrackEvents);
            Assert.Equal(startTicks, track.TrackEvents[0].StartPositionTicks);
            Assert.Equal(endTicks, track.TrackEvents[0].EndPositionTicks);
        }

        [Fact]
        public void FilterEvents_SubtitleEndingExactlyAtSegmentStart_IsRetained()
        {
            // Subtitle ends exactly when the segment begins.
            // EndPositionTicks == startPositionTicks means (end - start) == 0, not < 0,
            // so SkipWhile stops and the subtitle is retained.
            var track = new SubtitleTrackInfo
            {
                TrackEvents = new[]
                {
                    new SubtitleTrackEvent("1", "Boundary subtitle")
                    {
                        StartPositionTicks = TimeSpan.FromSeconds(5).Ticks,
                        EndPositionTicks = TimeSpan.FromSeconds(10).Ticks
                    },
                    new SubtitleTrackEvent("2", "In range")
                    {
                        StartPositionTicks = TimeSpan.FromSeconds(12).Ticks,
                        EndPositionTicks = TimeSpan.FromSeconds(15).Ticks
                    }
                }
            };

            _encoder.FilterEvents(
                track,
                startPositionTicks: TimeSpan.FromSeconds(10).Ticks,
                endTimeTicks: TimeSpan.FromSeconds(20).Ticks,
                preserveTimestamps: true);

            Assert.Equal(2, track.TrackEvents.Count);
            Assert.Equal("1", track.TrackEvents[0].Id);
        }

        [Fact]
        public void FilterEvents_SpanningBoundaryWithTimestampAdjustment_DoesNotProduceNegativeTimestamps()
        {
            // Subtitle starts at 5s, ends at 15s.
            // Segment requested from 10s to 20s, preserveTimestamps = false.
            // The subtitle spans the boundary and is retained, but shifting
            // StartPositionTicks by -10s would produce -5s (negative).
            var track = new SubtitleTrackInfo
            {
                TrackEvents = new[]
                {
                    new SubtitleTrackEvent("1", "Spans boundary")
                    {
                        StartPositionTicks = TimeSpan.FromSeconds(5).Ticks,
                        EndPositionTicks = TimeSpan.FromSeconds(15).Ticks
                    },
                    new SubtitleTrackEvent("2", "Fully in range")
                    {
                        StartPositionTicks = TimeSpan.FromSeconds(12).Ticks,
                        EndPositionTicks = TimeSpan.FromSeconds(17).Ticks
                    }
                }
            };

            _encoder.FilterEvents(
                track,
                startPositionTicks: TimeSpan.FromSeconds(10).Ticks,
                endTimeTicks: TimeSpan.FromSeconds(20).Ticks,
                preserveTimestamps: false);

            Assert.Equal(2, track.TrackEvents.Count);
            // Subtitle 1: start should be clamped to 0, not -5s
            Assert.True(track.TrackEvents[0].StartPositionTicks >= 0, "StartPositionTicks must not be negative");
            Assert.Equal(TimeSpan.FromSeconds(5).Ticks, track.TrackEvents[0].EndPositionTicks);
            // Subtitle 2: normal shift (12s - 10s = 2s, 17s - 10s = 7s)
            Assert.Equal(TimeSpan.FromSeconds(2).Ticks, track.TrackEvents[1].StartPositionTicks);
            Assert.Equal(TimeSpan.FromSeconds(7).Ticks, track.TrackEvents[1].EndPositionTicks);
        }

        [Fact]
        public void FilterEvents_NoEndTimeTicks_ReturnsAllFromStartPosition()
        {
            var track = new SubtitleTrackInfo
            {
                TrackEvents = new[]
                {
                    new SubtitleTrackEvent("1", "Before")
                    {
                        StartPositionTicks = TimeSpan.FromSeconds(2).Ticks,
                        EndPositionTicks = TimeSpan.FromSeconds(4).Ticks
                    },
                    new SubtitleTrackEvent("2", "After")
                    {
                        StartPositionTicks = TimeSpan.FromSeconds(12).Ticks,
                        EndPositionTicks = TimeSpan.FromSeconds(15).Ticks
                    },
                    new SubtitleTrackEvent("3", "Much later")
                    {
                        StartPositionTicks = TimeSpan.FromSeconds(500).Ticks,
                        EndPositionTicks = TimeSpan.FromSeconds(505).Ticks
                    }
                }
            };

            _encoder.FilterEvents(
                track,
                startPositionTicks: TimeSpan.FromSeconds(10).Ticks,
                endTimeTicks: 0,
                preserveTimestamps: true);

            Assert.Equal(2, track.TrackEvents.Count);
            Assert.Equal("2", track.TrackEvents[0].Id);
            Assert.Equal("3", track.TrackEvents[1].Id);
        }
    }
}
