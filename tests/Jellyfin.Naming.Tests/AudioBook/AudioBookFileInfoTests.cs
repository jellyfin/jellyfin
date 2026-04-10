using Emby.Naming.AudioBook;
using Xunit;

namespace Jellyfin.Naming.Tests.AudioBook
{
    public class AudioBookFileInfoTests
    {
        [Fact]
        public void CompareTo_Same_Success()
        {
            var info = new AudioBookFileInfo(string.Empty, string.Empty);
            Assert.Equal(0, info.CompareTo(info));
        }

        [Fact]
        public void CompareTo_Null_Success()
        {
            var info = new AudioBookFileInfo(string.Empty, string.Empty);
            Assert.Equal(1, info.CompareTo(null));
        }

        [Fact]
        public void CompareTo_Empty_Success()
        {
            var info1 = new AudioBookFileInfo(string.Empty, string.Empty);
            var info2 = new AudioBookFileInfo(string.Empty, string.Empty);
            Assert.Equal(0, info1.CompareTo(info2));
        }

        [Fact]
        public void CompareTo_DifferentParts_SortsByPart()
        {
            // Part is the outer grouping (like disc), chapter is inner (like track).
            // Part 2 Chapter 1 must sort after Part 1 Chapter 99.
            var part1ch99 = new AudioBookFileInfo("a", "mp3", partNumber: 1, chapterNumber: 99);
            var part2ch1 = new AudioBookFileInfo("b", "mp3", partNumber: 2, chapterNumber: 1);
            Assert.True(part1ch99.CompareTo(part2ch1) < 0);
            Assert.True(part2ch1.CompareTo(part1ch99) > 0);
        }

        [Fact]
        public void CompareTo_SamePart_SortsByChapter()
        {
            var part1ch1 = new AudioBookFileInfo("a", "mp3", partNumber: 1, chapterNumber: 1);
            var part1ch2 = new AudioBookFileInfo("b", "mp3", partNumber: 1, chapterNumber: 2);
            Assert.True(part1ch1.CompareTo(part1ch2) < 0);
        }

        [Fact]
        public void CompareTo_NullPartBeforeNumberedPart_Success()
        {
            var noPart = new AudioBookFileInfo("a", "mp3", partNumber: null, chapterNumber: 1);
            var part1 = new AudioBookFileInfo("b", "mp3", partNumber: 1, chapterNumber: 1);
            Assert.True(noPart.CompareTo(part1) < 0);
        }
    }
}
