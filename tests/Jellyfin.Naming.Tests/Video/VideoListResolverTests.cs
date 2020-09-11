using System.Linq;
using Emby.Naming.Common;
using Emby.Naming.Video;
using MediaBrowser.Model.IO;
using Xunit;

namespace Jellyfin.Naming.Tests.Video
{
    public class VideoListResolverTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        // FIXME
        // [Fact]
        private void TestStackAndExtras()
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

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Equal(5, result.Count);

            Assert.Equal(3, result[1].Files.Count);
            Assert.Equal(3, result[1].Extras.Count);
            Assert.Equal("Batman", result[1].Name);

            Assert.Equal(4, result[2].Files.Count);
            Assert.Equal(2, result[2].Extras.Count);
            Assert.Equal("Harry Potter and the Deathly Hallows", result[2].Name);
        }

        [Fact]
        public void TestWithMetadata()
        {
            var files = new[]
            {
                "300.mkv",
                "300.nfo"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

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

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void TestVariationWithFolderName()
        {
            var files = new[]
            {
                "X-Men Days of Future Past - 1080p.mkv",
                "X-Men Days of Future Past-trailer.mp4"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
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

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void TestDifferentNames()
        {
            var files = new[]
            {
                "Looper (2012)-trailer.mkv",
                "Looper.2012.bluray.720p.x264.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
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

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Equal(5, result.Count);
        }

        [Fact]
        public void TestMultiDisc()
        {
            var files = new[]
            {
                @"M:/Movies (DVD)/Movies (Musical)/Sound of Music (1965)/Sound of Music Disc 1",
                @"M:/Movies (DVD)/Movies (Musical)/Sound of Music (1965)/Sound of Music Disc 2"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = true,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void TestPoundSign()
        {
            // These should be considered separate, unrelated videos
            var files = new[]
            {
                @"My movie #1.mp4",
                @"My movie #2.mp4"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = true,
                FullName = i
            }).ToList()).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void TestStackedWithTrailer()
        {
            var files = new[]
            {
                @"No (2012) part1.mp4",
                @"No (2012) part2.mp4",
                @"No (2012) part1-trailer.mp4"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void TestStackedWithTrailer2()
        {
            var files = new[]
            {
                @"No (2012) part1.mp4",
                @"No (2012) part2.mp4",
                @"No (2012)-trailer.mp4"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void TestExtrasByFolderName()
        {
            var files = new[]
            {
                @"/Movies/Top Gun (1984)/movie.mp4",
                @"/Movies/Top Gun (1984)/Top Gun (1984)-trailer.mp4",
                @"/Movies/Top Gun (1984)/Top Gun (1984)-trailer2.mp4",
                @"trailer.mp4"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void TestDoubleTags()
        {
            var files = new[]
            {
                @"/MCFAMILY-PC/Private3$/Heterosexual/Breast In Class 2 Counterfeit Racks (2011)/Breast In Class 2 Counterfeit Racks (2011) Disc 1 cd1.avi",
                @"/MCFAMILY-PC/Private3$/Heterosexual/Breast In Class 2 Counterfeit Racks (2011)/Breast In Class 2 Counterfeit Racks (2011) Disc 1 cd2.avi",
                @"/MCFAMILY-PC/Private3$/Heterosexual/Breast In Class 2 Counterfeit Racks (2011)/Breast In Class 2 Disc 2 cd1.avi",
                @"/MCFAMILY-PC/Private3$/Heterosexual/Breast In Class 2 Counterfeit Racks (2011)/Breast In Class 2 Disc 2 cd2.avi"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void TestArgumentOutOfRangeException()
        {
            var files = new[]
            {
                @"/nas-markrobbo78/Videos/INDEX HTPC/Movies/Watched/3 - ACTION/Argo (2012)/movie.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void TestColony()
        {
            var files = new[]
            {
                @"The Colony.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void TestFourSisters()
        {
            var files = new[]
            {
                @"Four Sisters and a Wedding - A.avi",
                @"Four Sisters and a Wedding - B.avi"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void TestMovieTrailer()
        {
            var files = new[]
            {
                @"/Server/Despicable Me/Despicable Me (2010).mkv",
                @"/Server/Despicable Me/movie-trailer.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void TestTrailerFalsePositives()
        {
            var files = new[]
            {
                @"/Server/Despicable Me/Skyscraper (2018) - Big Game Spot.mkv",
                @"/Server/Despicable Me/Skyscraper (2018) - Trailer.mkv",
                @"/Server/Despicable Me/Baywatch (2017) - Big Game Spot.mkv",
                @"/Server/Despicable Me/Baywatch (2017) - Trailer.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Equal(4, result.Count);
        }

        [Fact]
        public void TestSubfolders()
        {
            var files = new[]
            {
                @"/Movies/Despicable Me/Despicable Me.mkv",
                @"/Movies/Despicable Me/trailers/trailer.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
        }

        private VideoListResolver GetResolver()
        {
            return new VideoListResolver(_namingOptions);
        }
    }
}
