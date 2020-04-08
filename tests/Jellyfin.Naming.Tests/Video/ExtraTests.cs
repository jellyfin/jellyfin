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
        public void TestDirectories()
        {
            var videoOptions = new NamingOptions();

            (ExtraType Type, string dirName)[] extraDirectoryNameTests =
            {
                (ExtraType.BehindTheScenes, "behind the scenes" ),
                (ExtraType.DeletedScene, "deleted scenes" ),
                (ExtraType.Interview, "interviews" ),
                (ExtraType.Scene, "scenes" ),
                (ExtraType.Sample, "samples" ),
                (ExtraType.Clip, "shorts" ),
                (ExtraType.Clip, "featurettes" ),
                (ExtraType.Unknown, "extras" ),
            };

            foreach ((ExtraType type, string dirName) in extraDirectoryNameTests)
            {
                Test(dirName + "/300.mp4", type, videoOptions);
                Test("300/" + dirName + "/something.mkv", type, videoOptions);
                Test("/data/something/Movies/300/" + dirName + "/whoknows.mp4", type, videoOptions);
            }

            //Test the null condition
            string[] nonExtraDirectoryNames = 
            {
               "gibberish",
               "not a scene",
            };
            foreach (string dirName in nonExtraDirectoryNames)
            {
                Test(dirName + "/300.mp4", null, videoOptions);
                Test("300/" + dirName + "/something.mkv", null, videoOptions);
                Test("/data/something/Movies/300/" + dirName + "/whoknows.mp4", null, videoOptions);
            }
            Test("/data/something/Movies/not a scene/not a scene.mp4", null, videoOptions);
            Test("/data/something/Movies/The Big Short/The Big Short.mp4", null, videoOptions);
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
