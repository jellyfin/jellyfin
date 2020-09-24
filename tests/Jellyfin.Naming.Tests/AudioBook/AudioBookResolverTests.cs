using System.Collections.Generic;
using Emby.Naming.AudioBook;
using Emby.Naming.Common;
using Xunit;

namespace Jellyfin.Naming.Tests.AudioBook
{
    public class AudioBookResolverTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        public static IEnumerable<object[]> GetResolveFileTestData()
        {
            yield return new object[]
            {
                new AudioBookFileInfo()
                {
                    Path = @"/server/AudioBooks/Larry Potter/Larry Potter.mp3",
                    Container = "mp3",
                }
            };
            yield return new object[]
            {
                new AudioBookFileInfo()
                {
                    Path = @"/server/AudioBooks/Berry Potter/Chapter 1 .ogg",
                    Container = "ogg",
                    ChapterNumber = 1
                }
            };
            yield return new object[]
            {
                new AudioBookFileInfo()
                {
                    Path = @"/server/AudioBooks/Nerry Potter/Part 3 - Chapter 2.mp3",
                    Container = "mp3",
                    ChapterNumber = 2,
                    PartNumber = 3
                }
            };
        }

        [Theory]
        [MemberData(nameof(GetResolveFileTestData))]
        public void ResolveFile_ValidFileName_Success(AudioBookFileInfo expectedResult)
        {
            var result = new AudioBookResolver(_namingOptions).Resolve(expectedResult.Path);

            Assert.NotNull(result);
            Assert.Equal(result.Path, expectedResult.Path);
            Assert.Equal(result.Container, expectedResult.Container);
            Assert.Equal(result.ChapterNumber, expectedResult.ChapterNumber);
            Assert.Equal(result.PartNumber, expectedResult.PartNumber);
            Assert.Equal(result.IsDirectory, expectedResult.IsDirectory);
        }
    }
}
