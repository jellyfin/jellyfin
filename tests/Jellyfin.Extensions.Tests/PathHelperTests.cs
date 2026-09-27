using System.IO;
using Jellyfin.Extensions;
using Xunit;

namespace Jellyfin.Extensions.Tests
{
    public static class PathHelperTests
    {
        [Theory]
        [InlineData("file.txt", "file.txt")]
        [InlineData("sub/file.txt", "file.txt")]
        [InlineData("../../etc/passwd", "passwd")]
        public static void GetSafeLeafFileName_ReducesToLeaf(string input, string expected)
        {
            Assert.Equal(expected, PathHelper.GetSafeLeafFileName(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(".")]
        [InlineData("..")]
        public static void GetSafeLeafFileName_RejectsUnusableLeaf(string? input)
        {
            Assert.Null(PathHelper.GetSafeLeafFileName(input));
        }

        [Fact]
        public static void IsContainedIn_ChildPath_ReturnsTrue()
        {
            var root = Path.Combine(Path.GetTempPath(), "root");
            var child = Path.Combine(root, "sub", "file.txt");
            Assert.True(PathHelper.IsContainedIn(root, child));
        }

        [Fact]
        public static void IsContainedIn_RootItself_ReturnsTrue()
        {
            var root = Path.Combine(Path.GetTempPath(), "root");
            Assert.True(PathHelper.IsContainedIn(root, root));
        }

        [Fact]
        public static void IsContainedIn_TraversalEscape_ReturnsFalse()
        {
            var root = Path.Combine(Path.GetTempPath(), "root");
            var escape = Path.Combine(root, "..", "..", "etc", "passwd");
            Assert.False(PathHelper.IsContainedIn(root, escape));
        }

        [Fact]
        public static void IsContainedIn_SiblingPrefixCollision_ReturnsFalse()
        {
            // "/var/data" must not be accepted as a parent of "/var/dataset".
            var root = Path.Combine(Path.GetTempPath(), "data");
            var sibling = Path.Combine(Path.GetTempPath(), "dataset", "file.txt");
            Assert.False(PathHelper.IsContainedIn(root, sibling));
        }
    }
}
