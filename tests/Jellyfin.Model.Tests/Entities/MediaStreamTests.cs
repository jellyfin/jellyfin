using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Model.Tests.Entities
{
    public class MediaStreamTests
    {
        public static IEnumerable<object[]> Get_DisplayTitle_TestData()
        {
            return new List<object[]>
            {
                new object[]
                {
                    new MediaStream
                    {
                        Type = MediaStreamType.Subtitle,
                        Title = "English",
                        Language = string.Empty,
                        IsForced = false,
                        IsDefault = false,
                        Codec = "ASS"
                    },
                    "English - Und - ASS"
                },
                new object[]
                {
                    new MediaStream
                    {
                        Type = MediaStreamType.Subtitle,
                        Title = "English",
                        Language = string.Empty,
                        IsForced = false,
                        IsDefault = false,
                        Codec = string.Empty
                    },
                    "English - Und"
                },
                new object[]
                {
                    new MediaStream
                    {
                        Type = MediaStreamType.Subtitle,
                        Title = "English",
                        Language = "EN",
                        IsForced = false,
                        IsDefault = false,
                        Codec = string.Empty
                    },
                    "English"
                },
                new object[]
                {
                    new MediaStream
                    {
                        Type = MediaStreamType.Subtitle,
                        Title = "English",
                        Language = "EN",
                        IsForced = true,
                        IsDefault = true,
                        Codec = "SRT"
                    },
                    "English - Default - Forced - SRT"
                },
                new object[]
                {
                    new MediaStream
                    {
                        Type = MediaStreamType.Subtitle,
                        Title = null,
                        Language = null,
                        IsForced = false,
                        IsDefault = false,
                        Codec = null
                    },
                    "Und"
                }
            };
        }

        [Theory]
        [MemberData(nameof(Get_DisplayTitle_TestData))]
        public void Get_DisplayTitle_should_return_valid_title(MediaStream mediaStream, string expected)
        {
            Assert.Equal(expected, mediaStream.DisplayTitle);
        }
    }
}
