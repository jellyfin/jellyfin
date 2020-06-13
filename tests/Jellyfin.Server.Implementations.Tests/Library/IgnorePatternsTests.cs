using Emby.Server.Implementations.Library;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library
{
    public class IgnorePatternsTests
    {
        [Theory]
        [InlineData("/media/small.jpg", true)]
        [InlineData("/media/movies/#Recycle/test.txt", true)]
        [InlineData("/media/movies/#recycle/", true)]
        [InlineData("thumbs.db", true)]
        [InlineData(@"C:\media\movies\movie.avi", false)]
        [InlineData("/media/.hiddendir/file.mp4", true)]
        [InlineData("/media/dir/.hiddenfile.mp4", true)]
        public void PathIgnored(string path, bool expected)
        {
            Assert.Equal(expected, IgnorePatterns.ShouldIgnore(path));
        }
    }
}
