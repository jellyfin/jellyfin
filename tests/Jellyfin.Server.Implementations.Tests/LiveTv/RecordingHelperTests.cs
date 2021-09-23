using System;
using System.Collections.Generic;
using System.Globalization;
using Emby.Server.Implementations.LiveTv.EmbyTV;
using MediaBrowser.Controller.LiveTv;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.LiveTv
{
    public static class RecordingHelperTests
    {
        public static IEnumerable<object[]> GetRecordingName_Success_TestData()
        {
            DateTime testLocalStartDate = new DateTime(2020, 4, 20, 21, 6, 0, DateTimeKind.Local);
            DateTime testOriginalAirDate = new DateTime(2018, 12, 6);

            yield return new object[]
            {
                $"The Incredibles {GetDateTimeString(testLocalStartDate)}",
                new TimerInfo
                {
                    Name = "The Incredibles",
                    StartDate = testLocalStartDate,
                    IsMovie = true
                }
            };

            yield return new object[]
            {
                "The Incredibles (2004)",
                new TimerInfo
                {
                    Name = "The Incredibles",
                    IsMovie = true,
                    ProductionYear = 2004
                }
            };

            yield return new object[]
            {
                $"The Big Bang Theory {GetDateTimeString(testLocalStartDate)}",
                new TimerInfo
                {
                    Name = "The Big Bang Theory",
                    StartDate = testLocalStartDate,
                    IsProgramSeries = true,
                }
            };

            yield return new object[]
            {
                "The Big Bang Theory S12E10",
                new TimerInfo
                {
                    Name = "The Big Bang Theory",
                    IsProgramSeries = true,
                    SeasonNumber = 12,
                    EpisodeNumber = 10
                }
            };

            yield return new object[]
            {
                "The Big Bang Theory S12E10 The VCR Illumination",
                new TimerInfo
                {
                    Name = "The Big Bang Theory",
                    IsProgramSeries = true,
                    SeasonNumber = 12,
                    EpisodeNumber = 10,
                    EpisodeTitle = "The VCR Illumination"
                }
            };

            yield return new object[]
            {
                $"The Big Bang Theory {GetDateString(testOriginalAirDate)}",
                new TimerInfo
                {
                    Name = "The Big Bang Theory",
                    IsProgramSeries = true,
                    OriginalAirDate = testOriginalAirDate
                }
            };

            yield return new object[]
            {
                $"The Big Bang Theory {GetDateString(testOriginalAirDate)} - The VCR Illumination",
                new TimerInfo
                {
                    Name = "The Big Bang Theory",
                    IsProgramSeries = true,
                    OriginalAirDate = testOriginalAirDate,
                    EpisodeTitle = "The VCR Illumination"
                }
            };

            yield return new object[]
            {
                $"The Big Bang Theory {GetDateString(testOriginalAirDate)} - The VCR Illumination",
                new TimerInfo
                {
                    Name = "The Big Bang Theory",
                    StartDate = testLocalStartDate,
                    IsProgramSeries = true,
                    OriginalAirDate = testOriginalAirDate,
                    EpisodeTitle = "The VCR Illumination"
                }
            };
        }

        [Theory]
        [MemberData(nameof(GetRecordingName_Success_TestData))]
        public static void GetRecordingName_Success(string expected, TimerInfo timerInfo)
        {
            Assert.Equal(expected, RecordingHelper.GetRecordingName(timerInfo));
        }

        private static string GetDateTimeString(DateTime date)
        {
            return date.ToLocalTime().ToString("yyyy_MM_dd_HH_mm_ss", CultureInfo.InvariantCulture);
        }

        private static string GetDateString(DateTime date)
        {
            return date.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}
