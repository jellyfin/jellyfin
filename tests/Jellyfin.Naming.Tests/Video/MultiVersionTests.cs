using System.Linq;
using Emby.Naming.Common;
using Emby.Naming.Video;
using MediaBrowser.Model.IO;
using Xunit;

namespace Jellyfin.Naming.Tests.Video
{
    public class MultiVersionTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        // FIXME
        // [Fact]
        private void TestMultiEdition1()
        {
            var files = new[]
            {
                @"/movies/X-Men Days of Future Past/X-Men Days of Future Past - 1080p.mkv",
                @"/movies/X-Men Days of Future Past/X-Men Days of Future Past-trailer.mp4",
                @"/movies/X-Men Days of Future Past/X-Men Days of Future Past - [hsbs].mkv",
                @"/movies/X-Men Days of Future Past/X-Men Days of Future Past [hsbs].mkv"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
            Assert.Single(result[0].Extras);
        }

        // FIXME
        // [Fact]
        private void TestMultiEdition2()
        {
            var files = new[]
            {
                @"/movies/X-Men Days of Future Past/X-Men Days of Future Past - apple.mkv",
                @"/movies/X-Men Days of Future Past/X-Men Days of Future Past-trailer.mp4",
                @"/movies/X-Men Days of Future Past/X-Men Days of Future Past - banana.mkv",
                @"/movies/X-Men Days of Future Past/X-Men Days of Future Past [banana].mp4"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
            Assert.Single(result[0].Extras);
            Assert.Equal(2, result[0].AlternateVersions.Count);
        }

        [Fact]
        public void TestMultiEdition3()
        {
            var files = new[]
            {
                @"/movies/The Phantom of the Opera (1925)/The Phantom of the Opera (1925) - 1925 version.mkv",
                @"/movies/The Phantom of the Opera (1925)/The Phantom of the Opera (1925) - 1929 version.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
            Assert.Single(result[0].AlternateVersions);
        }

        // FIXME
        // [Fact]
        private void TestLetterFolders()
        {
            var files = new[]
            {
                @"/movies/M/Movie 1.mkv",
                @"/movies/M/Movie 2.mkv",
                @"/movies/M/Movie 3.mkv",
                @"/movies/M/Movie 4.mkv",
                @"/movies/M/Movie 5.mkv",
                @"/movies/M/Movie 6.mkv",
                @"/movies/M/Movie 7.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Equal(7, result.Count);
            Assert.Empty(result[0].Extras);
            Assert.Empty(result[0].AlternateVersions);
        }

        // FIXME
        // [Fact]
        private void TestMultiVersionLimit()
        {
            var files = new[]
            {
                @"/movies/Movie/Movie.mkv",
                @"/movies/Movie/Movie-2.mkv",
                @"/movies/Movie/Movie-3.mkv",
                @"/movies/Movie/Movie-4.mkv",
                @"/movies/Movie/Movie-5.mkv",
                @"/movies/Movie/Movie-6.mkv",
                @"/movies/Movie/Movie-7.mkv",
                @"/movies/Movie/Movie-8.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
            Assert.Empty(result[0].Extras);
            Assert.Equal(7, result[0].AlternateVersions.Count);
        }

        // FIXME
        // [Fact]
        private void TestMultiVersionLimit2()
        {
            var files = new[]
            {
                @"/movies/Mo/Movie 1.mkv",
                @"/movies/Mo/Movie 2.mkv",
                @"/movies/Mo/Movie 3.mkv",
                @"/movies/Mo/Movie 4.mkv",
                @"/movies/Mo/Movie 5.mkv",
                @"/movies/Mo/Movie 6.mkv",
                @"/movies/Mo/Movie 7.mkv",
                @"/movies/Mo/Movie 8.mkv",
                @"/movies/Mo/Movie 9.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Equal(9, result.Count);
            Assert.Empty(result[0].Extras);
            Assert.Empty(result[0].AlternateVersions);
        }

        // FIXME
        // [Fact]
        private void TestMultiVersion3()
        {
            var files = new[]
            {
                @"/movies/Movie/Movie 1.mkv",
                @"/movies/Movie/Movie 2.mkv",
                @"/movies/Movie/Movie 3.mkv",
                @"/movies/Movie/Movie 4.mkv",
                @"/movies/Movie/Movie 5.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Equal(5, result.Count);
            Assert.Empty(result[0].Extras);
            Assert.Empty(result[0].AlternateVersions);
        }

        // FIXME
        // [Fact]
        private void TestMultiVersion4()
        {
            // Test for false positive

            var files = new[]
            {
                @"/movies/Iron Man/Iron Man.mkv",
                @"/movies/Iron Man/Iron Man (2008).mkv",
                @"/movies/Iron Man/Iron Man (2009).mkv",
                @"/movies/Iron Man/Iron Man (2010).mkv",
                @"/movies/Iron Man/Iron Man (2011).mkv"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Equal(5, result.Count);
            Assert.Empty(result[0].Extras);
            Assert.Empty(result[0].AlternateVersions);
        }

        // FIXME
        // [Fact]
        private void TestMultiVersion5()
        {
            var files = new[]
            {
                @"/movies/Iron Man/Iron Man.mkv",
                @"/movies/Iron Man/Iron Man-720p.mkv",
                @"/movies/Iron Man/Iron Man-test.mkv",
                @"/movies/Iron Man/Iron Man-bluray.mkv",
                @"/movies/Iron Man/Iron Man-3d.mkv",
                @"/movies/Iron Man/Iron Man-3d-hsbs.mkv",
                @"/movies/Iron Man/Iron Man-3d.hsbs.mkv",
                @"/movies/Iron Man/Iron Man[test].mkv",
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
            Assert.Empty(result[0].Extras);
            Assert.Equal(7, result[0].AlternateVersions.Count);
            Assert.False(result[0].AlternateVersions[2].Is3D);
            Assert.True(result[0].AlternateVersions[3].Is3D);
            Assert.True(result[0].AlternateVersions[4].Is3D);
        }

        // FIXME
        // [Fact]
        private void TestMultiVersion6()
        {
            var files = new[]
            {
                @"/movies/Iron Man/Iron Man.mkv",
                @"/movies/Iron Man/Iron Man - 720p.mkv",
                @"/movies/Iron Man/Iron Man - test.mkv",
                @"/movies/Iron Man/Iron Man - bluray.mkv",
                @"/movies/Iron Man/Iron Man - 3d.mkv",
                @"/movies/Iron Man/Iron Man - 3d-hsbs.mkv",
                @"/movies/Iron Man/Iron Man - 3d.hsbs.mkv",
                @"/movies/Iron Man/Iron Man [test].mkv"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
            Assert.Empty(result[0].Extras);
            Assert.Equal(7, result[0].AlternateVersions.Count);
            Assert.False(result[0].AlternateVersions[3].Is3D);
            Assert.True(result[0].AlternateVersions[4].Is3D);
            Assert.True(result[0].AlternateVersions[5].Is3D);
        }

        // FIXME
        // [Fact]
        private void TestMultiVersion7()
        {
            var files = new[]
            {
                @"/movies/Iron Man/Iron Man - B (2006).mkv",
                @"/movies/Iron Man/Iron Man - C (2007).mkv"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Equal(2, result.Count);
        }

        // FIXME
        // [Fact]
        private void TestMultiVersion8()
        {
            // This is not actually supported yet

            var files = new[]
            {
                @"/movies/Iron Man/Iron Man.mkv",
                @"/movies/Iron Man/Iron Man_720p.mkv",
                @"/movies/Iron Man/Iron Man_test.mkv",
                @"/movies/Iron Man/Iron Man_bluray.mkv",
                @"/movies/Iron Man/Iron Man_3d.mkv",
                @"/movies/Iron Man/Iron Man_3d-hsbs.mkv",
                @"/movies/Iron Man/Iron Man_3d.hsbs.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
            Assert.Empty(result[0].Extras);
            Assert.Equal(6, result[0].AlternateVersions.Count);
            Assert.False(result[0].AlternateVersions[2].Is3D);
            Assert.True(result[0].AlternateVersions[3].Is3D);
            Assert.True(result[0].AlternateVersions[4].Is3D);
        }

        // FIXME
        // [Fact]
        private void TestMultiVersion9()
        {
            // Test for false positive

            var files = new[]
            {
                @"/movies/Iron Man/Iron Man (2007).mkv",
                @"/movies/Iron Man/Iron Man (2008).mkv",
                @"/movies/Iron Man/Iron Man (2009).mkv",
                @"/movies/Iron Man/Iron Man (2010).mkv",
                @"/movies/Iron Man/Iron Man (2011).mkv"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Equal(5, result.Count);
            Assert.Empty(result[0].Extras);
            Assert.Empty(result[0].AlternateVersions);
        }

        // FIXME
        // [Fact]
        private void TestMultiVersion10()
        {
            var files = new[]
            {
                @"/movies/Blade Runner (1982)/Blade Runner (1982) [Final Cut] [1080p HEVC AAC].mkv",
                @"/movies/Blade Runner (1982)/Blade Runner (1982) [EE by ADM] [480p HEVC AAC,AAC,AAC].mkv"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
            Assert.Empty(result[0].Extras);
            Assert.Single(result[0].AlternateVersions);
        }

        // FIXME
        // [Fact]
        private void TestMultiVersion11()
        {
            // Currently not supported but we should probably handle this.

            var files = new[]
            {
                @"/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) [1080p] Blu-ray.x264.DTS.mkv",
                @"/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) [2160p] Blu-ray.x265.AAC.mkv"
            };

            var resolver = GetResolver();

            var result = resolver.Resolve(files.Select(i => new FileSystemMetadata
            {
                IsDirectory = false,
                FullName = i
            }).ToList()).ToList();

            Assert.Single(result);
            Assert.Empty(result[0].Extras);
            Assert.Single(result[0].AlternateVersions);
        }

        private VideoListResolver GetResolver()
        {
            return new VideoListResolver(_namingOptions);
        }
    }
}
