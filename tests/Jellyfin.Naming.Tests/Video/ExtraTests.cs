using Emby.Naming.Common;
using Emby.Naming.Video;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Naming.Tests.Video
{
    public class ExtraTests
    {
        private readonly NamingOptions _videoOptions = new NamingOptions();

        // Requirements
        // movie-deleted = ExtraType deletedscene

        // All of the above rules should be configurable through the options objects (ideally, even the ExtraTypes)

        [Fact]
        public void TestKodiExtras()
        {
            Test("trailer.mp4", ExtraType.Trailer, _videoOptions);
            Test("300-trailer.mp4", ExtraType.Trailer, _videoOptions);

            Test("theme.mp3", ExtraType.ThemeSong, _videoOptions);
        }

        [Fact]
        public void TestExpandedExtras()
        {
            Test("trailer.mp4", ExtraType.Trailer, _videoOptions);
            Test("trailer.mp3", null, _videoOptions);
            Test("300-trailer.mp4", ExtraType.Trailer, _videoOptions);

            Test("theme.mp3", ExtraType.ThemeSong, _videoOptions);
            Test("theme.mkv", null, _videoOptions);

            Test("300-scene.mp4", ExtraType.Scene, _videoOptions);
            Test("300-scene2.mp4", ExtraType.Scene, _videoOptions);
            Test("300-clip.mp4", ExtraType.Clip, _videoOptions);

            Test("300-deleted.mp4", ExtraType.DeletedScene, _videoOptions);
            Test("300-deletedscene.mp4", ExtraType.DeletedScene, _videoOptions);
            Test("300-interview.mp4", ExtraType.Interview, _videoOptions);
            Test("300-behindthescenes.mp4", ExtraType.BehindTheScenes, _videoOptions);
        }

        [Theory]
        [InlineData(ExtraType.BehindTheScenes, "behind the scenes" )]
        [InlineData(ExtraType.DeletedScene, "deleted scenes" )]
        [InlineData(ExtraType.Interview, "interviews" )]
        [InlineData(ExtraType.Scene, "scenes" )]
        [InlineData(ExtraType.Sample, "samples" )]
        [InlineData(ExtraType.Clip, "shorts" )]
        [InlineData(ExtraType.Clip, "featurettes" )]
        [InlineData(ExtraType.Unknown, "extras" )]
        public void TestDirectories(ExtraType type, string dirName)
        {
            Test(dirName + "/300.mp4", type, _videoOptions);
            Test("300/" + dirName + "/something.mkv", type, _videoOptions);
            Test("/data/something/Movies/300/" + dirName + "/whoknows.mp4", type, _videoOptions);
        }

        [Theory]
        [InlineData("gibberish")]
        [InlineData("not a scene")]
        [InlineData("The Big Short")]
        public void TestNonExtraDirectories(string dirName)
        {
            Test(dirName + "/300.mp4", null, _videoOptions);
            Test("300/" + dirName + "/something.mkv", null, _videoOptions);
            Test("/data/something/Movies/300/" + dirName + "/whoknows.mp4", null, _videoOptions);
            Test("/data/something/Movies/" + dirName + "/" + dirName + ".mp4", null, _videoOptions);
        }

        [Fact]
        public void TestSample()
        {
            Test("300-sample.mp4", ExtraType.Sample, _videoOptions);
        }

        private void Test(string input, ExtraType? expectedType, NamingOptions videoOptions)
        {
            var parser = GetExtraTypeParser(videoOptions);

            var extraType = parser.GetExtraInfo(input).ExtraType;

            if (expectedType == null)
            {
                Assert.Null(extraType);
            }
            else
            {
                Assert.Equal(expectedType, extraType);
            }
        }

        private ExtraResolver GetExtraTypeParser(NamingOptions videoOptions)
        {
            return new ExtraResolver(videoOptions);
        }
    }
}
