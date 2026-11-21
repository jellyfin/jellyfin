using Emby.Naming.Video;
using Xunit;

namespace Jellyfin.Naming.Tests.Video
{
    public class CleanDateTimeResultTests
    {
        [Fact]
        public void Equals_SameNameAndYear_ReturnsTrue()
        {
            var left = new CleanDateTimeResult("Arrival", 2016);
            var right = new CleanDateTimeResult("Arrival", 2016);

            Assert.Equal(left, right);
            Assert.True(left == right);
            Assert.False(left != right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }

        [Fact]
        public void Equals_DifferentName_ReturnsFalse()
        {
            var left = new CleanDateTimeResult("Arrival", 2016);
            var right = new CleanDateTimeResult("Dune", 2016);

            Assert.NotEqual(left, right);
            Assert.False(left == right);
            Assert.True(left != right);
        }

        [Fact]
        public void Equals_DifferentYear_ReturnsFalse()
        {
            var left = new CleanDateTimeResult("Arrival", 2016);
            var right = new CleanDateTimeResult("Arrival", 2017);

            Assert.NotEqual(left, right);
        }

        [Fact]
        public void Equals_NullYearAndMissingYear_ReturnsTrue()
        {
            var left = new CleanDateTimeResult("American.Psycho.mkv");
            var right = new CleanDateTimeResult("American.Psycho.mkv", null);

            Assert.Equal(left, right);
            Assert.True(left == right);
        }

        [Fact]
        public void Equals_ObjectOfDifferentType_ReturnsFalse()
        {
            var result = new CleanDateTimeResult("Arrival", 2016);

            Assert.False(result.Equals("Arrival"));
            Assert.False(result.Equals(null));
        }
    }
}
