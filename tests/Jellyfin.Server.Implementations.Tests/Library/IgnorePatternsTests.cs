using Emby.Server.Implementations.Library;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library
{
    public class IgnorePatternsTests
    {
        [Theory]
        [InlineData("/media/small.jpg", true)]
        [InlineData("/media/albumart.jpg", true)]
        [InlineData("/media/movie.sample.mp4", true)]
        [InlineData("/media/movie/sample.mp4", true)]
        [InlineData("/media/movie/sample/movie.mp4", true)]
        [InlineData("/foo/sample/bar/baz.mkv", false)]
        [InlineData("/media/movies/the sample/the sample.mkv", false)]
        [InlineData("/media/movies/sampler.mkv", false)]
        [InlineData("/media/movies/#Recycle/test.txt", true)]
        [InlineData("/media/movies/#recycle/", true)]
        [InlineData("/media/movies/#recycle", true)]
        [InlineData("thumbs.db", true)]
        [InlineData(@"C:\media\movies\movie.avi", false)]
        [InlineData("/media/.hiddendir/file.mp4", false)]
        [InlineData("/media/dir/.hiddenfile.mp4", true)]
        [InlineData("/media/dir/._macjunk.mp4", true)]
        [InlineData("/volume1/video/Series/@eaDir", true)]
        [InlineData("/volume1/video/Series/@eaDir/file.txt", true)]
        [InlineData("/directory/@Recycle", true)]
        [InlineData("/directory/@Recycle/file.mp3", true)]
        [InlineData("/media/movies/.@__thumb", true)]
        [InlineData("/media/movies/.@__thumb/foo-bar-thumbnail.png", true)]
        [InlineData("/media/music/Foo B.A.R./epic.flac", false)]
        [InlineData("/media/music/Foo B.A.R", false)]
        [InlineData("/media/music/Foo B.A.R.", false)]
        [InlineData("/movies/.zfs/snapshot/AutoM-2023-09", true)]
        public void PathIgnored(string path, bool expected)
        {
            Assert.Equal(expected, IgnorePatterns.ShouldIgnore(path));
        }
    }
}
