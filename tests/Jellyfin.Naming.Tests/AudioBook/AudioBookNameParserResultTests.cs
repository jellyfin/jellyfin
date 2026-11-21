using Emby.Naming.AudioBook;
using Xunit;

namespace Jellyfin.Naming.Tests.AudioBook
{
    public class AudioBookNameParserResultTests
    {
        [Fact]
        public void Equals_SameNameAndYear_ReturnsTrue()
        {
            var left = new AudioBookNameParserResult { Name = "Dune", Year = 1965 };
            var right = new AudioBookNameParserResult { Name = "Dune", Year = 1965 };

            Assert.Equal(left, right);
            Assert.True(left == right);
            Assert.False(left != right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }

        [Fact]
        public void Equals_DifferentName_ReturnsFalse()
        {
            var left = new AudioBookNameParserResult { Name = "Dune", Year = 1965 };
            var right = new AudioBookNameParserResult { Name = "Foundation", Year = 1965 };

            Assert.NotEqual(left, right);
            Assert.False(left == right);
            Assert.True(left != right);
        }

        [Fact]
        public void Equals_DifferentYear_ReturnsFalse()
        {
            var left = new AudioBookNameParserResult { Name = "Dune", Year = 1965 };
            var right = new AudioBookNameParserResult { Name = "Dune", Year = 2021 };

            Assert.NotEqual(left, right);
        }

        [Fact]
        public void Equals_BothDefault_ReturnsTrue()
        {
            AudioBookNameParserResult left = default;
            AudioBookNameParserResult right = default;

            Assert.Equal(left, right);
            Assert.True(left == right);
        }

        [Fact]
        public void Equals_ObjectOfDifferentType_ReturnsFalse()
        {
            var result = new AudioBookNameParserResult { Name = "Dune", Year = 1965 };

            Assert.False(result.Equals("Dune"));
            Assert.False(result.Equals(null));
        }
    }
}
