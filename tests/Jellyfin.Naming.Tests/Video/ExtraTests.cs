using Emby.Naming.Common;
using Emby.Naming.Video;
using MediaBrowser.Model.Entities;
using Xunit;

using MediaType = Emby.Naming.Common.MediaType;

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
            Test("trailer.mp4", ExtraType.Trailer);
            Test("300-trailer.mp4", ExtraType.Trailer);
            Test("300.trailer.mp4", ExtraType.Trailer);
            Test("300_trailer.mp4", ExtraType.Trailer);
            Test("300 - trailer.mp4", ExtraType.Trailer);

            Test("theme.mp3", ExtraType.ThemeSong);
        }

        [Fact]
        public void TestExpandedExtras()
        {
            Test("trailer.mp4", ExtraType.Trailer);
            Test("trailer.mp3", null);
            Test("300-trailer.mp4", ExtraType.Trailer);
            Test("stuff trailerthings.mkv", null);

            Test("theme.mp3", ExtraType.ThemeSong);
            Test("theme.mkv", null);

            Test("300-scene.mp4", ExtraType.Scene);
            Test("300-scene2.mp4", ExtraType.Scene);
            Test("300-clip.mp4", ExtraType.Clip);

            Test("300-deleted.mp4", ExtraType.DeletedScene);
            Test("300-deletedscene.mp4", ExtraType.DeletedScene);
            Test("300-interview.mp4", ExtraType.Interview);
            Test("300-behindthescenes.mp4", ExtraType.BehindTheScenes);
            Test("300-featurette.mp4", ExtraType.Featurette);
            Test("300-short.mp4", ExtraType.Short);
            Test("300-extra.mp4", ExtraType.Unknown);
            Test("300-other.mp4", ExtraType.Unknown);
        }

        [Theory]
        [InlineData(ExtraType.ThemeSong, "theme-music")]
        public void TestDirectoriesAudioExtras(ExtraType type, string dirName)
        {
            Test(dirName + "/300.mp3", type);
            Test("300/" + dirName + "/something.mp3", type);
            Test("/data/something/Movies/300/" + dirName + "/whoknows.mp3", type);
        }

        [Theory]
        [InlineData(ExtraType.BehindTheScenes, "behind the scenes")]
        [InlineData(ExtraType.DeletedScene, "deleted scenes")]
        [InlineData(ExtraType.Interview, "interviews")]
        [InlineData(ExtraType.Scene, "scenes")]
        [InlineData(ExtraType.Sample, "samples")]
        [InlineData(ExtraType.Short, "shorts")]
        [InlineData(ExtraType.Trailer, "trailers")]
        [InlineData(ExtraType.Featurette, "featurettes")]
        [InlineData(ExtraType.Clip, "clips")]
        [InlineData(ExtraType.ThemeVideo, "backdrops")]
        [InlineData(ExtraType.Unknown, "extra")]
        [InlineData(ExtraType.Unknown, "extras")]
        [InlineData(ExtraType.Unknown, "other")]
        public void TestDirectoriesVideoExtras(ExtraType type, string dirName)
        {
            Test(dirName + "/300.mp4", type);
            Test("300/" + dirName + "/something.mkv", type);
            Test("/data/something/Movies/300/" + dirName + "/whoknows.mp4", type);
        }

        [Theory]
        [InlineData("gibberish")]
        [InlineData("not a scene")]
        [InlineData("The Big Short")]
        public void TestNonExtraDirectories(string dirName)
        {
            Test(dirName + "/300.mp4", null);
            Test("300/" + dirName + "/something.mkv", null);
            Test("/data/something/Movies/300/" + dirName + "/whoknows.mp4", null);
            Test("/data/something/Movies/" + dirName + "/" + dirName + ".mp4", null);
        }

        [Theory]
        [InlineData(ExtraType.ThemeSong, "theme-music")]
        public void TestTopLevelDirectoriesWithAudioExtraNames(ExtraType typicalType, string dirName)
        {
            string libraryRoot = "/data/something/" + dirName;
            TestWithLibraryRoot(libraryRoot + "/300.mp3", libraryRoot, null);
            TestWithLibraryRoot(libraryRoot + "/300/" + dirName + "/something.mp3", libraryRoot, typicalType);
        }

        [Theory]
        [InlineData(ExtraType.Trailer, "trailers")]
        [InlineData(ExtraType.ThemeVideo, "backdrops")]
        [InlineData(ExtraType.BehindTheScenes, "behind the scenes")]
        [InlineData(ExtraType.DeletedScene, "deleted scenes")]
        [InlineData(ExtraType.Interview, "interviews")]
        [InlineData(ExtraType.Scene, "scenes")]
        [InlineData(ExtraType.Sample, "samples")]
        [InlineData(ExtraType.Short, "shorts")]
        [InlineData(ExtraType.Featurette, "featurettes")]
        [InlineData(ExtraType.Unknown, "extras")]
        [InlineData(ExtraType.Unknown, "extra")]
        [InlineData(ExtraType.Unknown, "other")]
        [InlineData(ExtraType.Clip, "clips")]
        public void TestTopLevelDirectoriesWithVideoExtraNames(ExtraType typicalType, string dirName)
        {
            string libraryRoot = "/data/something/" + dirName;
            TestWithLibraryRoot(libraryRoot + "/300.mp4", libraryRoot, null);
            TestWithLibraryRoot(libraryRoot + "/300/" + dirName + "/something.mkv", libraryRoot, typicalType);
        }

        [Fact]
        public void TestSample()
        {
            Test("sample.mp4", ExtraType.Sample);
            Test("300-sample.mp4", ExtraType.Sample);
            Test("300.sample.mp4", ExtraType.Sample);
            Test("300_sample.mp4", ExtraType.Sample);
            Test("300 - sample.mp4", ExtraType.Sample);
        }

        [Fact]
        public void TestSuffixPartOfTitle()
        {
            Test("I Live In A Trailer.mp4", null);
            Test("The DNA Sample.mp4", null);
        }

        private void Test(string input, ExtraType? expectedType)
        {
            var extraType = ExtraRuleResolver.GetExtraInfo(input, _videoOptions).ExtraType;

            Assert.Equal(expectedType, extraType);
        }

        private void TestWithLibraryRoot(string input, string libraryRoot, ExtraType? expectedType)
        {
            var extraType = ExtraRuleResolver.GetExtraInfo(input, _videoOptions, libraryRoot).ExtraType;
            Assert.Equal(expectedType, extraType);
        }

        [Fact]
        public void TestExtraInfo_InvalidRuleType()
        {
            var rule = new ExtraRule(ExtraType.Unknown, ExtraRuleType.Regex, @"([eE]x(tra)?\.\w+)", MediaType.Video);
            var options = new NamingOptions { VideoExtraRules = new[] { rule } };
            var res = ExtraRuleResolver.GetExtraInfo("extra.mp4", options);

            Assert.Equal(rule, res.Rule);
        }
    }
}
