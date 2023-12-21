using Emby.Naming.AudioBook;
using Emby.Naming.Common;
using Xunit;

namespace Jellyfin.Naming.Tests.AudioBook
{
    public class AudioBookResolverTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        public static TheoryData<AudioBookFileInfo> Resolve_ValidFileNameTestData()
        {
            var data = new TheoryData<AudioBookFileInfo>();

            data.Add(
                new AudioBookFileInfo(
                    "/server/AudioBooks/Larry Potter/Larry Potter.mp3",
                    "mp3"));

            data.Add(
                new AudioBookFileInfo(
                    "/server/AudioBooks/Berry Potter/Chapter 1 .ogg",
                    "ogg",
                    chapterNumber: 1));

            data.Add(
                new AudioBookFileInfo(
                    "/server/AudioBooks/Nerry Potter/Part 3 - Chapter 2.mp3",
                    "mp3",
                    chapterNumber: 2,
                    partNumber: 3));

            return data;
        }

        [Theory]
        [MemberData(nameof(Resolve_ValidFileNameTestData))]
        public void Resolve_ValidFileName_Success(AudioBookFileInfo expectedResult)
        {
            var result = new AudioBookResolver(_namingOptions).Resolve(expectedResult.Path);

            Assert.NotNull(result);
            Assert.Equal(result!.Path, expectedResult.Path);
            Assert.Equal(result!.Container, expectedResult.Container);
            Assert.Equal(result!.ChapterNumber, expectedResult.ChapterNumber);
            Assert.Equal(result!.PartNumber, expectedResult.PartNumber);
        }

        [Fact]
        public void Resolve_InvalidExtension()
        {
            var result = new AudioBookResolver(_namingOptions).Resolve("/server/AudioBooks/Larry Potter/Larry Potter.mp9");

            Assert.Null(result);
        }

        [Fact]
        public void Resolve_EmptyFileName()
        {
            var result = new AudioBookResolver(_namingOptions).Resolve(string.Empty);

            Assert.Null(result);
        }
    }
}
