using System;
using Xunit;

namespace Jellyfin.Extensions.Tests
{
    public class DateTimeExtensionsTests
    {
        [Theory]
        [InlineData(DateTimeKind.Unspecified, 0)] // As a provider parses "2000-05-18"
        [InlineData(DateTimeKind.Local, 0)]
        [InlineData(DateTimeKind.Local, 23)]
        [InlineData(DateTimeKind.Utc, 0)]
        [InlineData(DateTimeKind.Utc, 23)]
        public void ToUtcDate_AnyKindOrTime_MidnightUtcOnTheSameCalendarDate(DateTimeKind kind, int hour)
        {
            var result = new DateTime(2000, 5, 18, hour, 30, 0, kind).ToUtcDate();

            Assert.Equal(DateTimeKind.Utc, result.Kind);
            Assert.Equal(new DateTime(2000, 5, 18, 0, 0, 0, DateTimeKind.Utc), result);
        }
    }
}
