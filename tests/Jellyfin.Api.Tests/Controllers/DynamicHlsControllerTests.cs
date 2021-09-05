using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.StreamingDtos;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers
{
    public class DynamicHlsControllerTests
    {
        [Theory]
        [MemberData(nameof(GetSegmentLengths_Success_TestData))]
        public void GetSegmentLengths_Success(long runtimeTicks, int segmentlength, double[] expected)
        {
            var res = DynamicHlsController.GetSegmentLengthsInternal(runtimeTicks, segmentlength);
            Assert.Equal(expected.Length, res.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], res[i]);
            }
        }

        public static IEnumerable<object[]> GetSegmentLengths_Success_TestData()
        {
            yield return new object[] { 0, 6, Array.Empty<double>() };
            yield return new object[]
            {
                TimeSpan.FromSeconds(3).Ticks,
                6,
                new double[] { 3 }
            };
            yield return new object[]
            {
                TimeSpan.FromSeconds(6).Ticks,
                6,
                new double[] { 6 }
            };
            yield return new object[]
            {
                TimeSpan.FromSeconds(3.3333333).Ticks,
                6,
                new double[] { 3.3333333 }
            };
            yield return new object[]
            {
                TimeSpan.FromSeconds(9.3333333).Ticks,
                6,
                new double[] { 6, 3.3333333 }
            };
        }
    }
}
