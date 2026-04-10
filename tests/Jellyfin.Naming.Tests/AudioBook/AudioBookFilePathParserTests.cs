using Emby.Naming.AudioBook;
using Emby.Naming.Common;
using Xunit;

namespace Jellyfin.Naming.Tests.AudioBook
{
    public class AudioBookFilePathParserTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        [Theory]
        [InlineData("/Book/Part 1/Chapter 01.mp3", 1, 1)]
        [InlineData("/Book/Part 2/Chapter 03.mp3", 2, 3)]
        [InlineData("/Book/Part 2/Chapter 05.mp3", 2, 5)]
        [InlineData("/Book/part 1/ch01.mp3", 1, 1)]
        [InlineData("/Book/Part 01/Chapter 1.mp3", 1, 1)]
        public void Parse_FileInPartSubfolder_ExtractsPartFromFolder(string path, int expectedPart, int expectedChapter)
        {
            var result = new AudioBookFilePathParser(_namingOptions).Parse(path);
            Assert.Equal(expectedPart, result.PartNumber);
            Assert.Equal(expectedChapter, result.ChapterNumber);
        }

        [Theory]
        [InlineData("/Book/Part 99/Part 1 Chapter 3.mp3", 1, 3)]
        public void Parse_FileWithPartInName_FilenamePartTakesPrecedenceOverFolder(string path, int expectedPart, int expectedChapter)
        {
            var result = new AudioBookFilePathParser(_namingOptions).Parse(path);
            Assert.Equal(expectedPart, result.PartNumber);
            Assert.Equal(expectedChapter, result.ChapterNumber);
        }

        [Theory]
        [InlineData("/Book/Chapter 01.mp3", null, 1)]
        [InlineData("/Book/extras/bonus.mp3", null, null)]
        public void Parse_FileNotInPartSubfolder_NoPartNumber(string path, int? expectedPart, int? expectedChapter)
        {
            var result = new AudioBookFilePathParser(_namingOptions).Parse(path);
            Assert.Equal(expectedPart, result.PartNumber);
            Assert.Equal(expectedChapter, result.ChapterNumber);
        }
    }
}
