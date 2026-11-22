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
        public void Equals_SameValues_True()
        {
            var info1 = new AudioBookFileInfo("/books/a.mp3", "mp3", 1, 2);
            var info2 = new AudioBookFileInfo("/books/a.mp3", "mp3", 1, 2);

            Assert.True(info1.Equals(info2));
            Assert.True(info1.Equals((object)info2));
            Assert.True(info1 == info2);
            Assert.False(info1 != info2);
            Assert.Equal(info1.GetHashCode(), info2.GetHashCode());
        }

        [Theory]
        [InlineData("/books/b.mp3", 1, 2)]
        [InlineData("/books/a.mp3", 2, 2)]
        [InlineData("/books/a.mp3", 1, 3)]
        [InlineData("/books/a.mp3", null, 2)]
        [InlineData("/books/a.mp3", 1, null)]
        public void Equals_DifferentValues_False(string path, int? partNumber, int? chapterNumber)
        {
            var info1 = new AudioBookFileInfo("/books/a.mp3", "mp3", 1, 2);
            var info2 = new AudioBookFileInfo(path, "mp3", partNumber, chapterNumber);

            Assert.False(info1.Equals(info2));
            Assert.False(info1 == info2);
            Assert.True(info1 != info2);
        }

        [Fact]
        public void Equals_Null_False()
        {
            var info = new AudioBookFileInfo(string.Empty, string.Empty);

            Assert.False(info.Equals(null));
            Assert.False(info.Equals((object?)null));
            Assert.False(info == null);
            Assert.True(info != null);
        }

        [Fact]
        public void EqualityOperator_BothNull_True()
        {
            AudioBookFileInfo? info1 = null;
            AudioBookFileInfo? info2 = null;

            Assert.True(info1 == info2);
            Assert.False(info1 != info2);
        }

        [Theory]
        [InlineData("/books/a.mp3", 1, 1, "/books/a.mp3", 1, 2)]
        [InlineData("/books/a.mp3", 1, 1, "/books/a.mp3", 2, 1)]
        [InlineData("/books/a.mp3", 1, 1, "/books/b.mp3", 1, 1)]
        [InlineData("/books/z.mp3", 9, 1, "/books/a.mp3", 1, 2)]
        public void ComparisonOperators_MatchCompareTo(
            string lowerPath,
            int? lowerPart,
            int? lowerChapter,
            string higherPath,
            int? higherPart,
            int? higherChapter)
        {
            var lower = new AudioBookFileInfo(lowerPath, "mp3", lowerPart, lowerChapter);
            var higher = new AudioBookFileInfo(higherPath, "mp3", higherPart, higherChapter);

            Assert.True(lower.CompareTo(higher) < 0);
            Assert.True(lower < higher);
            Assert.True(lower <= higher);
            Assert.False(lower > higher);
            Assert.False(lower >= higher);
            Assert.True(higher > lower);
            Assert.True(higher >= lower);
            Assert.False(higher < lower);
            Assert.False(higher <= lower);
        }

        [Fact]
        public void ComparisonOperators_EqualValues()
        {
            var info1 = new AudioBookFileInfo("/books/a.mp3", "mp3", 1, 1);
            var info2 = new AudioBookFileInfo("/books/a.mp3", "mp3", 1, 1);

            Assert.False(info1 < info2);
            Assert.True(info1 <= info2);
            Assert.False(info1 > info2);
            Assert.True(info1 >= info2);
        }

        [Fact]
        public void ComparisonOperators_NullSortsFirst()
        {
            var info = new AudioBookFileInfo(string.Empty, string.Empty);
            AudioBookFileInfo? nullInfo = null;

            Assert.True(nullInfo < info);
            Assert.True(nullInfo <= info);
            Assert.False(nullInfo > info);
            Assert.False(nullInfo >= info);
            Assert.True(info > nullInfo);
            Assert.True(info >= nullInfo);
            Assert.False(info < nullInfo);
            Assert.False(info <= nullInfo);
        }
    }
}
