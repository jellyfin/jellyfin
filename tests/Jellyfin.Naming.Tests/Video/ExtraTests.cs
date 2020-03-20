using Emby.Naming.Common;
using Emby.Naming.Video;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Naming.Tests.Video
{
    public class ExtraTests : BaseVideoTest
    {
        // Requirements
        // movie-deleted = ExtraType deletedscene

        // All of the above rules should be configurable through the options objects (ideally, even the ExtraTypes)

        [Fact]
        public void TestKodiExtras()
        {
            var videoOptions = new NamingOptions();

            Test("trailer.mp4", ExtraType.Trailer, videoOptions);
            Test("300-trailer.mp4", ExtraType.Trailer, videoOptions);

            Test("theme.mp3", ExtraType.ThemeSong, videoOptions);
        }

        [Fact]
        public void TestExpandedExtras()
        {
            var videoOptions = new NamingOptions();

            Test("trailer.mp4", ExtraType.Trailer, videoOptions);
            Test("trailer.mp3", null, videoOptions);
            Test("300-trailer.mp4", ExtraType.Trailer, videoOptions);

            Test("theme.mp3", ExtraType.ThemeSong, videoOptions);
            Test("theme.mkv", null, videoOptions);

            Test("300-scene.mp4", ExtraType.Scene, videoOptions);
            Test("300-scene2.mp4", ExtraType.Scene, videoOptions);
            Test("300-clip.mp4", ExtraType.Clip, videoOptions);

            Test("300-deleted.mp4", ExtraType.DeletedScene, videoOptions);
            Test("300-deletedscene.mp4", ExtraType.DeletedScene, videoOptions);
            Test("300-interview.mp4", ExtraType.Interview, videoOptions);
            Test("300-behindthescenes.mp4", ExtraType.BehindTheScenes, videoOptions);
        }

        [Fact]
        public void TestSample()
        {
            var videoOptions = new NamingOptions();

            Test("300-sample.mp4", ExtraType.Sample, videoOptions);
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
