using System;
using System.Linq;
using Emby.Naming.Common;
using Emby.Naming.Video;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Naming.Tests.Video
{
    public class VideoListResolverTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        [Fact]
        public void TestStackAndExtras()
        {
            // No stacking here because there is no part/disc/etc
            var files = new[]
            {
                "Harry Potter and the Deathly Hallows-trailer.mkv",
                "Harry Potter and the Deathly Hallows.trailer.mkv",
                "Harry Potter and the Deathly Hallows part1.mkv",
                "Harry Potter and the Deathly Hallows part2.mkv",
                "Harry Potter and the Deathly Hallows part3.mkv",
                "Harry Potter and the Deathly Hallows part4.mkv",
                "Batman-deleted.mkv",
                "Batman-sample.mkv",
                "Batman-trailer.mkv",
                "Batman part1.mkv",
                "Batman part2.mkv",
                "Batman part3.mkv",
                "Avengers.mkv",
                "Avengers-trailer.mkv",

                // Despite having a keyword in the name that will return an ExtraType, there's no original video to match it to
                // So this is just a standalone video
                "trailer.mkv",

                // Same as above
                "WillyWonka-trailer.mkv"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            Assert.Equal(11, result.Count);
            var batman = result.FirstOrDefault(x => string.Equals(x.Name, "Batman", StringComparison.Ordinal));
            Assert.NotNull(batman);
            Assert.Equal(3, batman!.Files.Count);

            var harry = result.FirstOrDefault(x => string.Equals(x.Name, "Harry Potter and the Deathly Hallows", StringComparison.Ordinal));
            Assert.NotNull(harry);
            Assert.Equal(4, harry!.Files.Count);

            Assert.False(result[2].ExtraType.HasValue);

            Assert.Equal(ExtraType.Trailer, result[3].ExtraType);
            Assert.Equal(ExtraType.Trailer, result[4].ExtraType);
            Assert.Equal(ExtraType.DeletedScene, result[5].ExtraType);
            Assert.Equal(ExtraType.Sample, result[6].ExtraType);
            Assert.Equal(ExtraType.Trailer, result[7].ExtraType);
            Assert.Equal(ExtraType.Trailer, result[8].ExtraType);
            Assert.Equal(ExtraType.Trailer, result[9].ExtraType);
            Assert.Equal(ExtraType.Trailer, result[10].ExtraType);
        }

        [Fact]
        public void TestWithMetadata()
        {
            var files = new[]
            {
                "300.mkv",
                "300.nfo"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void TestWithExtra()
        {
            var files = new[]
            {
                "300.mkv",
                "300 trailer.mkv"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            Assert.Equal(2, result.Count);
            Assert.False(result[0].ExtraType.HasValue);
            Assert.Equal(ExtraType.Trailer, result[1].ExtraType);
        }

        [Fact]
        public void TestVariationWithFolderName()
        {
            var files = new[]
            {
                "X-Men Days of Future Past - 1080p.mkv",
                "X-Men Days of Future Past-trailer.mp4"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            Assert.Equal(2, result.Count);
            Assert.False(result[0].ExtraType.HasValue);
            Assert.Equal(ExtraType.Trailer, result[1].ExtraType);
        }

        [Fact]
        public void TestTrailer2()
        {
            var files = new[]
            {
                "X-Men Days of Future Past - 1080p.mkv",
                "X-Men Days of Future Past-trailer.mp4",
                "X-Men Days of Future Past-trailer2.mp4"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            Assert.Equal(3, result.Count);
            Assert.False(result[0].ExtraType.HasValue);
            Assert.Equal(ExtraType.Trailer, result[1].ExtraType);
            Assert.Equal(ExtraType.Trailer, result[2].ExtraType);
        }

        [Fact]
        public void Resolve_SameNameAndYear_ReturnsSingleItem()
        {
            var files = new[]
            {
                "Looper (2012)-trailer.mkv",
                "Looper 2012-trailer.mkv",
                "Looper.2012.bluray.720p.x264.mkv"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            Assert.Equal(3, result.Count);
            Assert.False(result[0].ExtraType.HasValue);
            Assert.Equal(ExtraType.Trailer, result[1].ExtraType);
            Assert.Equal(ExtraType.Trailer, result[2].ExtraType);
        }

        [Fact]
        public void Resolve_TrailerMatchesFolderName_ReturnsSingleItem()
        {
            var files = new[]
            {
                "/movies/Looper (2012)/Looper (2012)-trailer.mkv",
                "/movies/Looper (2012)/Looper.bluray.720p.x264.mkv"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            Assert.Equal(2, result.Count);
            Assert.False(result[0].ExtraType.HasValue);
            Assert.Equal(ExtraType.Trailer, result[1].ExtraType);
        }

        [Fact]
        public void TestSeparateFiles()
        {
            // These should be considered separate, unrelated videos
            var files = new[]
            {
                "My video 1.mkv",
                "My video 2.mkv",
                "My video 3.mkv",
                "My video 4.mkv",
                "My video 5.mkv"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            Assert.Equal(5, result.Count);
        }

        [Fact]
        public void TestMultiDisc()
        {
            var files = new[]
            {
                "M:/Movies (DVD)/Movies (Musical)/Sound of Music (1965)/Sound of Music Disc 1",
                "M:/Movies (DVD)/Movies (Musical)/Sound of Music (1965)/Sound of Music Disc 2"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, true, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void TestPoundSign()
        {
            // These should be considered separate, unrelated videos
            var files = new[]
            {
                "My movie #1.mp4",
                "My movie #2.mp4"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, true, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void TestStackedWithTrailer()
        {
            var files = new[]
            {
                "No (2012) part1.mp4",
                "No (2012) part2.mp4",
                "No (2012) part1-trailer.mp4",
                "No (2012)-trailer.mp4"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            Assert.Equal(3, result.Count);
            Assert.False(result[0].ExtraType.HasValue);
            Assert.Equal(ExtraType.Trailer, result[1].ExtraType);
            Assert.Equal(ExtraType.Trailer, result[2].ExtraType);
        }

        [Fact]
        public void TestExtrasByFolderName()
        {
            var files = new[]
            {
                "/Movies/Top Gun (1984)/movie.mp4",
                "/Movies/Top Gun (1984)/Top Gun (1984)-trailer.mp4",
                "/Movies/Top Gun (1984)/Top Gun (1984)-trailer2.mp4",
                "/Movies/trailer.mp4"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            Assert.Equal(4, result.Count);
            Assert.False(result[0].ExtraType.HasValue);
            Assert.Equal(ExtraType.Trailer, result[1].ExtraType);
            Assert.Equal(ExtraType.Trailer, result[2].ExtraType);
            Assert.Equal(ExtraType.Trailer, result[3].ExtraType);
        }

        [Fact]
        public void TestDoubleTags()
        {
            var files = new[]
            {
                "/MCFAMILY-PC/Private3$/Heterosexual/Breast In Class 2 Counterfeit Racks (2011)/Breast In Class 2 Counterfeit Racks (2011) Disc 1 cd1.avi",
                "/MCFAMILY-PC/Private3$/Heterosexual/Breast In Class 2 Counterfeit Racks (2011)/Breast In Class 2 Counterfeit Racks (2011) Disc 1 cd2.avi",
                "/MCFAMILY-PC/Private3$/Heterosexual/Breast In Class 2 Counterfeit Racks (2011)/Breast In Class 2 Disc 2 cd1.avi",
                "/MCFAMILY-PC/Private3$/Heterosexual/Breast In Class 2 Counterfeit Racks (2011)/Breast In Class 2 Disc 2 cd2.avi"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void TestArgumentOutOfRangeException()
        {
            var files = new[]
            {
                "/nas-markrobbo78/Videos/INDEX HTPC/Movies/Watched/3 - ACTION/Argo (2012)/movie.mkv"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void TestColony()
        {
            var files = new[]
            {
                "The Colony.mkv"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void TestFourSisters()
        {
            var files = new[]
            {
                "Four Sisters and a Wedding - A.avi",
                "Four Sisters and a Wedding - B.avi"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            // The result should contain two individual movies
            // Version grouping should not work here, because the files are not in a directory with the name 'Four Sisters and a Wedding'
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void TestFourRooms()
        {
            var files = new[]
            {
                "Four Rooms - A.avi",
                "Four Rooms - A.mp4"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void TestMovieTrailer()
        {
            var files = new[]
            {
                "/Server/Despicable Me/Despicable Me (2010).mkv",
                "/Server/Despicable Me/trailer.mkv"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            Assert.Equal(2, result.Count);
            Assert.False(result[0].ExtraType.HasValue);
            Assert.Equal(ExtraType.Trailer, result[1].ExtraType);
        }

        [Fact]
        public void Resolve_TrailerInTrailersFolder_ReturnsCorrectExtraType()
        {
            var files = new[]
            {
                "/Server/Despicable Me/Despicable Me (2010).mkv",
                "/Server/Despicable Me/trailers/some title.mkv"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            Assert.Equal(2, result.Count);
            Assert.False(result[0].ExtraType.HasValue);
            Assert.Equal(ExtraType.Trailer, result[1].ExtraType);
        }

        [Fact]
        public void TestSubfolders()
        {
            var files = new[]
            {
                "/Movies/Despicable Me/Despicable Me.mkv",
                "/Movies/Despicable Me/trailers/trailer.mkv"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions).ToList();

            Assert.Equal(2, result.Count);
            Assert.False(result[0].ExtraType.HasValue);
            Assert.Equal(ExtraType.Trailer, result[1].ExtraType);
        }

        [Fact]
        public void TestDirectoryStack()
        {
            var stack = new FileStack(string.Empty, false, Array.Empty<string>());
            Assert.False(stack.ContainsFile("XX", true));
        }
    }
}
