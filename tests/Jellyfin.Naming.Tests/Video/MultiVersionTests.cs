using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Naming.Common;
using Emby.Naming.Video;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Naming.Tests.Video
{
    public class MultiVersionTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();
        private readonly VideoListResolver _videoListResolver;

        public MultiVersionTests()
        {
            _videoListResolver = new VideoListResolver(_namingOptions);
        }

        [Fact]
        public void TestMultiEdition1()
        {
            var files = new[]
            {
                "/movies/X-Men Days of Future Past/X-Men Days of Future Past - 1080p.mkv",
                "/movies/X-Men Days of Future Past/X-Men Days of Future Past-trailer.mp4",
                "/movies/X-Men Days of Future Past/X-Men Days of Future Past - [hsbs].mkv",
                "/movies/X-Men Days of Future Past/X-Men Days of Future Past [hsbs].mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result, v => v.ExtraType is null);
            Assert.Single(result, v => v.ExtraType is not null);
        }

        [Fact]
        public void TestMultiEdition2()
        {
            var files = new[]
            {
                "/movies/X-Men Days of Future Past/X-Men Days of Future Past - apple.mkv",
                "/movies/X-Men Days of Future Past/X-Men Days of Future Past-trailer.mp4",
                "/movies/X-Men Days of Future Past/X-Men Days of Future Past - banana.mkv",
                "/movies/X-Men Days of Future Past/X-Men Days of Future Past [banana].mp4"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result, v => v.ExtraType is null);
            Assert.Single(result, v => v.ExtraType is not null);
            Assert.Equal(2, result[0].AlternateVersions.Count);
        }

        [Fact]
        public void TestMultiEdition3()
        {
            var files = new[]
            {
                "/movies/The Phantom of the Opera (1925)/The Phantom of the Opera (1925) - 1925 version.mkv",
                "/movies/The Phantom of the Opera (1925)/The Phantom of the Opera (1925) - 1929 version.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result);
            Assert.Single(result[0].AlternateVersions);
        }

        [Fact]
        public void TestLetterFolders()
        {
            var files = new[]
            {
                "/movies/M/Movie 1.mkv",
                "/movies/M/Movie 2.mkv",
                "/movies/M/Movie 3.mkv",
                "/movies/M/Movie 4.mkv",
                "/movies/M/Movie 5.mkv",
                "/movies/M/Movie 6.mkv",
                "/movies/M/Movie 7.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Equal(7, result.Count);
            Assert.Empty(result[0].AlternateVersions);
        }

        [Fact]
        public void TestMultiVersionLimit()
        {
            var files = new[]
            {
                "/movies/Movie/Movie.mkv",
                "/movies/Movie/Movie-2.mkv",
                "/movies/Movie/Movie-3.mkv",
                "/movies/Movie/Movie-4.mkv",
                "/movies/Movie/Movie-5.mkv",
                "/movies/Movie/Movie-6.mkv",
                "/movies/Movie/Movie-7.mkv",
                "/movies/Movie/Movie-8.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result);
            Assert.Equal(7, result[0].AlternateVersions.Count);
        }

        [Fact]
        public void TestMultiVersionLimit2()
        {
            var files = new[]
            {
                "/movies/Mo/Movie 1.mkv",
                "/movies/Mo/Movie 2.mkv",
                "/movies/Mo/Movie 3.mkv",
                "/movies/Mo/Movie 4.mkv",
                "/movies/Mo/Movie 5.mkv",
                "/movies/Mo/Movie 6.mkv",
                "/movies/Mo/Movie 7.mkv",
                "/movies/Mo/Movie 8.mkv",
                "/movies/Mo/Movie 9.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Equal(9, result.Count);
            Assert.Empty(result[0].AlternateVersions);
        }

        [Fact]
        public void TestMultiVersion3()
        {
            var files = new[]
            {
                "/movies/Movie/Movie 1.mkv",
                "/movies/Movie/Movie 2.mkv",
                "/movies/Movie/Movie 3.mkv",
                "/movies/Movie/Movie 4.mkv",
                "/movies/Movie/Movie 5.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Equal(5, result.Count);
            Assert.Empty(result[0].AlternateVersions);
        }

        [Fact]
        public void TestMultiVersion4()
        {
            // Test for false positive

            var files = new[]
            {
                "/movies/Iron Man/Iron Man.mkv",
                "/movies/Iron Man/Iron Man (2008).mkv",
                "/movies/Iron Man/Iron Man (2009).mkv",
                "/movies/Iron Man/Iron Man (2010).mkv",
                "/movies/Iron Man/Iron Man (2011).mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Equal(5, result.Count);
            Assert.Empty(result[0].AlternateVersions);
        }

        [Fact]
        public void TestMultiVersion5()
        {
            var files = new[]
            {
                "/movies/Iron Man/Iron Man.mkv",
                "/movies/Iron Man/Iron Man-720p.mkv",
                "/movies/Iron Man/Iron Man-test.mkv",
                "/movies/Iron Man/Iron Man-bluray.mkv",
                "/movies/Iron Man/Iron Man-3d.mkv",
                "/movies/Iron Man/Iron Man-3d-hsbs.mkv",
                "/movies/Iron Man/Iron Man[test].mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result);
            Assert.Equal("/movies/Iron Man/Iron Man.mkv", result[0].Files[0].Path);
            Assert.Equal(6, result[0].AlternateVersions.Count);
            Assert.Equal("/movies/Iron Man/Iron Man-720p.mkv", result[0].AlternateVersions[0].Files[0].Path);
            Assert.Equal("/movies/Iron Man/Iron Man-3d.mkv", result[0].AlternateVersions[1].Files[0].Path);
            Assert.Equal("/movies/Iron Man/Iron Man-3d-hsbs.mkv", result[0].AlternateVersions[2].Files[0].Path);
            Assert.Equal("/movies/Iron Man/Iron Man-bluray.mkv", result[0].AlternateVersions[3].Files[0].Path);
            Assert.Equal("/movies/Iron Man/Iron Man-test.mkv", result[0].AlternateVersions[4].Files[0].Path);
            Assert.Equal("/movies/Iron Man/Iron Man[test].mkv", result[0].AlternateVersions[5].Files[0].Path);
        }

        [Fact]
        public void TestMultiVersion6()
        {
            var files = new[]
            {
                "/movies/Iron Man/Iron Man.mkv",
                "/movies/Iron Man/Iron Man - 720p.mkv",
                "/movies/Iron Man/Iron Man - test.mkv",
                "/movies/Iron Man/Iron Man - bluray.mkv",
                "/movies/Iron Man/Iron Man - 3d.mkv",
                "/movies/Iron Man/Iron Man - 3d-hsbs.mkv",
                "/movies/Iron Man/Iron Man [test].mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result);
            Assert.Equal("/movies/Iron Man/Iron Man.mkv", result[0].Files[0].Path);
            Assert.Equal(6, result[0].AlternateVersions.Count);
            Assert.Equal("/movies/Iron Man/Iron Man - 720p.mkv", result[0].AlternateVersions[0].Files[0].Path);
            Assert.Equal("/movies/Iron Man/Iron Man - 3d.mkv", result[0].AlternateVersions[1].Files[0].Path);
            Assert.Equal("/movies/Iron Man/Iron Man - 3d-hsbs.mkv", result[0].AlternateVersions[2].Files[0].Path);
            Assert.Equal("/movies/Iron Man/Iron Man - bluray.mkv", result[0].AlternateVersions[3].Files[0].Path);
            Assert.Equal("/movies/Iron Man/Iron Man - test.mkv", result[0].AlternateVersions[4].Files[0].Path);
            Assert.Equal("/movies/Iron Man/Iron Man [test].mkv", result[0].AlternateVersions[5].Files[0].Path);
        }

        [Fact]
        public void TestMultiVersion7()
        {
            var files = new[]
            {
                "/movies/Iron Man/Iron Man - B (2006).mkv",
                "/movies/Iron Man/Iron Man - C (2007).mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void TestMultiVersion8()
        {
            var files = new[]
            {
                "/movies/Iron Man/Iron Man.mkv",
                "/movies/Iron Man/Iron Man_720p.mkv",
                "/movies/Iron Man/Iron Man_test.mkv",
                "/movies/Iron Man/Iron Man_bluray.mkv",
                "/movies/Iron Man/Iron Man_3d.mkv",
                "/movies/Iron Man/Iron Man_3d-hsbs.mkv",
                "/movies/Iron Man/Iron Man_3d.hsbs.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result);
            Assert.Equal(6, result[0].AlternateVersions.Count);

            // Verify 3D recognition is preserved on alternate versions
            var hsbs = result[0].AlternateVersions.First(v => v.Files[0].Path.Contains("3d-hsbs", StringComparison.Ordinal));
            Assert.True(hsbs.Files[0].Is3D);
            Assert.Equal("hsbs", hsbs.Files[0].Format3D);
        }

        [Fact]
        public void TestMultiVersion9()
        {
            // Test for false positive

            var files = new[]
            {
                "/movies/Iron Man/Iron Man (2007).mkv",
                "/movies/Iron Man/Iron Man (2008).mkv",
                "/movies/Iron Man/Iron Man (2009).mkv",
                "/movies/Iron Man/Iron Man (2010).mkv",
                "/movies/Iron Man/Iron Man (2011).mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Equal(5, result.Count);
            Assert.Empty(result[0].AlternateVersions);
        }

        [Fact]
        public void TestMultiVersion10()
        {
            var files = new[]
            {
                "/movies/Blade Runner (1982)/Blade Runner (1982) [Final Cut] [1080p HEVC AAC].mkv",
                "/movies/Blade Runner (1982)/Blade Runner (1982) [EE by ADM] [480p HEVC AAC,AAC,AAC].mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result);
            Assert.Single(result[0].AlternateVersions);
        }

        [Fact]
        public void TestMultiVersion11()
        {
            var files = new[]
            {
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) [1080p] Blu-ray.x264.DTS.mkv",
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) [2160p] Blu-ray.x265.AAC.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result);
            Assert.Single(result[0].AlternateVersions);
        }

        [Fact]
        public void TestMultiVersion12()
        {
            var files = new[]
            {
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - Theatrical Release.mkv",
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - Directors Cut.mkv",
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 1080p.mkv",
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 2160p.mkv",
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 720p.mkv",
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016).mkv",
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016).mkv", result[0].Files[0].Path);
            Assert.Equal(5, result[0].AlternateVersions.Count);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 2160p.mkv", result[0].AlternateVersions[0].Files[0].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 1080p.mkv", result[0].AlternateVersions[1].Files[0].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 720p.mkv", result[0].AlternateVersions[2].Files[0].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - Directors Cut.mkv", result[0].AlternateVersions[3].Files[0].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - Theatrical Release.mkv", result[0].AlternateVersions[4].Files[0].Path);
        }

        [Fact]
        public void TestMultiVersion13()
        {
            var files = new[]
            {
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - Theatrical Release.mkv",
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - Directors Cut.mkv",
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 1080p.mkv",
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 2160p.mkv",
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 1080p Directors Cut.mkv",
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 2160p Remux.mkv",
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 1080p Theatrical Release.mkv",
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 720p.mkv",
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 1080p Remux.mkv",
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 720p Directors Cut.mkv",
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 1080p High Bitrate.mkv",
                "/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016).mkv",
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016).mkv", result[0].Files[0].Path);
            Assert.Equal(11, result[0].AlternateVersions.Count);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 2160p.mkv", result[0].AlternateVersions[0].Files[0].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 2160p Remux.mkv", result[0].AlternateVersions[1].Files[0].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 1080p.mkv", result[0].AlternateVersions[2].Files[0].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 1080p Directors Cut.mkv", result[0].AlternateVersions[3].Files[0].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 1080p High Bitrate.mkv", result[0].AlternateVersions[4].Files[0].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 1080p Remux.mkv", result[0].AlternateVersions[5].Files[0].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 1080p Theatrical Release.mkv", result[0].AlternateVersions[6].Files[0].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 720p.mkv", result[0].AlternateVersions[7].Files[0].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 720p Directors Cut.mkv", result[0].AlternateVersions[8].Files[0].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - Directors Cut.mkv", result[0].AlternateVersions[9].Files[0].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - Theatrical Release.mkv", result[0].AlternateVersions[10].Files[0].Path);
        }

        [Fact]
        public void Resolve_GivenFolderNameWithBracketsAndHyphens_GroupsBasedOnFolderName()
        {
            var files = new[]
            {
                "/movies/John Wick - Kapitel 3 (2019) [imdbid=tt6146586]/John Wick - Kapitel 3 (2019) [imdbid=tt6146586] - Version 1.mkv",
                "/movies/John Wick - Kapitel 3 (2019) [imdbid=tt6146586]/John Wick - Kapitel 3 (2019) [imdbid=tt6146586] - Version 2.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result);
            Assert.Single(result[0].AlternateVersions);
        }

        [Fact]
        public void Resolve_GivenUnclosedBrackets_DoesNotGroup()
        {
            var files = new[]
            {
                "/movies/John Wick - Chapter 3 (2019)/John Wick - Chapter 3 (2019) [Version 1].mkv",
                "/movies/John Wick - Chapter 3 (2019)/John Wick - Chapter 3 (2019) [Version 2.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void TestEmptyList()
        {
            var result = _videoListResolver.Resolve(new List<VideoFileInfo>()).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void Resolve_GivenUnderscoreSeparator_GroupsVersions()
        {
            var files = new[]
            {
                "/movies/Movie (2020)/Movie (2020)_4K.mkv",
                "/movies/Movie (2020)/Movie (2020)_1080p.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result);
            Assert.Single(result[0].AlternateVersions);
        }

        [Fact]
        public void Resolve_GivenDotSeparator_GroupsVersions()
        {
            var files = new[]
            {
                "/movies/Movie (2020)/Movie (2020).UHD.mkv",
                "/movies/Movie (2020)/Movie (2020).1080p.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result);
            Assert.Single(result[0].AlternateVersions);
        }

        // Episode multi-version tests

        [Fact]
        public void TestMultiVersionEpisodeInOwnFolder()
        {
            // Two versions of S01E01 in their own subfolder should merge
            var files = new[]
            {
                "/TV/Dexter/Dexter - S01E01/Dexter - S01E01 - 1080p.mkv",
                "/TV/Dexter/Dexter - S01E01/Dexter - S01E01 - 720p.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Single(result);
            Assert.Single(result[0].AlternateVersions);
            // 1080p should be primary (higher resolution)
            Assert.Contains("1080p", result[0].Files[0].Path, StringComparison.Ordinal);
            Assert.Contains("720p", result[0].AlternateVersions[0].Files[0].Path, StringComparison.Ordinal);
        }

        [Fact]
        public void TestMultiVersionEpisodeMixedSeasonFolder()
        {
            // Multiple episodes in season folder, some with versions
            var files = new[]
            {
                "/TV/Dexter/Season 1/Dexter - S01E01 - 1080p.mkv",
                "/TV/Dexter/Season 1/Dexter - S01E01 - 720p.mkv",
                "/TV/Dexter/Season 1/Dexter - S01E02.mkv",
                "/TV/Dexter/Season 1/Dexter - S01E03 - 1080p.mkv",
                "/TV/Dexter/Season 1/Dexter - S01E03 - 720p.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Equal(3, result.Count);

            // S01E01 - should have one alternate version
            var e01 = result.FirstOrDefault(r => r.Files[0].Path.Contains("S01E01", StringComparison.Ordinal));
            Assert.NotNull(e01);
            Assert.Single(e01!.AlternateVersions);
            Assert.Contains("1080p", e01.Files[0].Path, StringComparison.Ordinal);

            // S01E02 - standalone, no alternates
            var e02 = result.FirstOrDefault(r => r.Files[0].Path.Contains("S01E02", StringComparison.Ordinal));
            Assert.NotNull(e02);
            Assert.Empty(e02!.AlternateVersions);

            // S01E03 - should have one alternate version
            var e03 = result.FirstOrDefault(r => r.Files[0].Path.Contains("S01E03", StringComparison.Ordinal));
            Assert.NotNull(e03);
            Assert.Single(e03!.AlternateVersions);
        }

        [Fact]
        public void TestMultiVersionEpisodeDontCollapse()
        {
            // Different episodes should NOT collapse into versions
            var files = new[]
            {
                "/TV/Dexter/Season 1/Dexter - S01E01.mkv",
                "/TV/Dexter/Season 1/Dexter - S01E02.mkv",
                "/TV/Dexter/Season 1/Dexter - S01E03.mkv",
                "/TV/Dexter/Season 1/Dexter - S01E04.mkv",
                "/TV/Dexter/Season 1/Dexter - S01E05.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Equal(5, result.Count);
            Assert.All(result, r => Assert.Empty(r.AlternateVersions));
        }

        [Fact]
        public void TestMultiVersionEpisodeWithVersionSuffix()
        {
            // Episodes with named versions (like Aired/Uncensored)
            var files = new[]
            {
                "/TV/Show/Season 1/Show - S01E01 - Aired.mkv",
                "/TV/Show/Season 1/Show - S01E01 - Uncensored.mkv",
                "/TV/Show/Season 1/Show - S01E02 - Aired.mkv",
                "/TV/Show/Season 1/Show - S01E02 - Uncensored.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Single(r.AlternateVersions));
        }

        [Fact]
        public void TestMultiVersionEpisodeFourVersions()
        {
            // Four versions of the same episode
            var files = new[]
            {
                "/TV/Show/Season 1/Show - S01E01 - VersionA.mkv",
                "/TV/Show/Season 1/Show - S01E01 - VersionB.mkv",
                "/TV/Show/Season 1/Show - S01E01 - VersionC.mkv",
                "/TV/Show/Season 1/Show - S01E01 - VersionD.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Single(result);
            Assert.Equal(3, result[0].AlternateVersions.Count);
        }

        [Fact]
        public void TestMultiVersionEpisodeWithResolutions()
        {
            // Resolution sorting should work for episodes too
            var files = new[]
            {
                "/TV/Show/Season 1/Show - S01E01 - 720p.mkv",
                "/TV/Show/Season 1/Show - S01E01 - 2160p.mkv",
                "/TV/Show/Season 1/Show - S01E01 - 1080p.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Single(result);
            Assert.Equal(2, result[0].AlternateVersions.Count);
            // Primary should be 2160p (highest resolution)
            Assert.Contains("2160p", result[0].Files[0].Path, StringComparison.Ordinal);
            // Next should be 1080p, then 720p
            Assert.Contains("1080p", result[0].AlternateVersions[0].Files[0].Path, StringComparison.Ordinal);
            Assert.Contains("720p", result[0].AlternateVersions[1].Files[0].Path, StringComparison.Ordinal);
        }

        [Fact]
        public void TestMultiVersionEpisodeDifferentSeasons()
        {
            // Same episode number but different seasons should NOT group
            var files = new[]
            {
                "/TV/Show/Show - S01E01.mkv",
                "/TV/Show/Show - S02E01.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Empty(r.AlternateVersions));
        }

        [Fact]
        public void TestMultiVersionEpisodeDisabledByDefault()
        {
            // Without collectionType: CollectionType.tvshows, episodes should NOT group
            var files = new[]
            {
                "/TV/Show/Season 1/Show - S01E01 - 1080p.mkv",
                "/TV/Show/Season 1/Show - S01E01 - 720p.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            // Without the tvshows collection type, these fall through the movie path
            // (folder-name eligibility fails) and are treated as separate items.
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void TestMultiVersionEpisodeSameNumberDifferentTitle()
        {
            // Two files parse to the same S01E01 but carry distinct episode titles.
            // Current behavior: they are grouped as alternate versions because
            // grouping keys only on season + episode number, not on episode title.
            // This documents the trade-off: users with mis-numbered episodes will
            // see one of the files collapsed into AlternateVersions of the other.
            var files = new[]
            {
                "/TV/Show/Season 1/Show - S01E01 - Pilot.mkv",
                "/TV/Show/Season 1/Show - S01E01 - Completely Different Title.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Single(result);
            Assert.Single(result[0].AlternateVersions);
        }

        [Fact]
        public void TestMultiVersionEpisodeWithTitle()
        {
            // Episodes with an episode title AND a version suffix should group
            var files = new[]
            {
                "/TV/Show/Show - S01E01/Show - S01E01 - Episode Title - 1080p.mkv",
                "/TV/Show/Show - S01E01/Show - S01E01 - Episode Title - 720p.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Single(result);
            Assert.Single(result[0].AlternateVersions);
            Assert.Contains("1080p", result[0].Files[0].Path, StringComparison.Ordinal);
            Assert.Contains("720p", result[0].AlternateVersions[0].Files[0].Path, StringComparison.Ordinal);
        }

        [Fact]
        public void TestMultiVersionEpisodeWithTitleMixedFolder()
        {
            // Multiple different episodes with titles and resolution variants in a season folder
            var files = new[]
            {
                "/TV/Show/Season 1/Show - S01E01 - Pilot - 1080p.mkv",
                "/TV/Show/Season 1/Show - S01E01 - Pilot - 720p.mkv",
                "/TV/Show/Season 1/Show - S01E02 - Second Episode - 1080p.mkv",
                "/TV/Show/Season 1/Show - S01E02 - Second Episode - 720p.mkv",
                "/TV/Show/Season 1/Show - S01E03 - Third Episode.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Equal(3, result.Count);

            var e01 = result.FirstOrDefault(r => r.Files[0].Path.Contains("S01E01", StringComparison.Ordinal));
            Assert.NotNull(e01);
            Assert.Single(e01!.AlternateVersions);

            var e02 = result.FirstOrDefault(r => r.Files[0].Path.Contains("S01E02", StringComparison.Ordinal));
            Assert.NotNull(e02);
            Assert.Single(e02!.AlternateVersions);

            var e03 = result.FirstOrDefault(r => r.Files[0].Path.Contains("S01E03", StringComparison.Ordinal));
            Assert.NotNull(e03);
            Assert.Empty(e03!.AlternateVersions);
        }

        [Fact]
        public void TestMultiVersionEpisodeInSeasonSubfolder()
        {
            // Two versions of S01E01 in their own subfolder under a season folder
            var files = new[]
            {
                "/TV/Show/Season 1/Show - S01E01/Show - S01E01 - 1080p.mkv",
                "/TV/Show/Season 1/Show - S01E01/Show - S01E01 - 720p.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Single(result);
            Assert.Single(result[0].AlternateVersions);
            Assert.Contains("1080p", result[0].Files[0].Path, StringComparison.Ordinal);
            Assert.Contains("720p", result[0].AlternateVersions[0].Files[0].Path, StringComparison.Ordinal);
        }

        [Fact]
        public void TestMultiVersionEpisodeWithTitleAndVersionSuffix()
        {
            // Episodes with episode title AND a named version suffix
            var files = new[]
            {
                "/TV/Show/Season 1/Show - S01E01 - Pilot - Aired.mkv",
                "/TV/Show/Season 1/Show - S01E01 - Pilot - Uncensored.mkv",
                "/TV/Show/Season 1/Show - S01E02 - The Getaway - Aired.mkv",
                "/TV/Show/Season 1/Show - S01E02 - The Getaway - Uncensored.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Single(r.AlternateVersions));
        }

        [Fact]
        public void TestMultiVersionEpisodeWithAdditionalPartsCd()
        {
            // Stacked episode (cd1/cd2) with higher resolution alongside a single-file lower-res version
            var files = new[]
            {
                "/TV/Show/Season 1/Show - S01E01 - 1080p cd1.mkv",
                "/TV/Show/Season 1/Show - S01E01 - 1080p cd2.mkv",
                "/TV/Show/Season 1/Show - S01E01 - 720p.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Single(result);
            Assert.Equal(2, result[0].Files.Count);
            Assert.Single(result[0].AlternateVersions);
            Assert.Contains("720p", result[0].AlternateVersions[0].Files[0].Path, StringComparison.Ordinal);
        }

        [Fact]
        public void TestMultiVersionEpisodeWithAdditionalPartsDashPart()
        {
            // Stacked episode using "- part1" / "- part2" separator
            var files = new[]
            {
                "/TV/Show/Season 1/Show - S01E01 - 1080p - part1.mkv",
                "/TV/Show/Season 1/Show - S01E01 - 1080p - part2.mkv",
                "/TV/Show/Season 1/Show - S01E01 - 720p.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Single(result);
            Assert.Equal(2, result[0].Files.Count);
            Assert.Single(result[0].AlternateVersions);
            Assert.Contains("720p", result[0].AlternateVersions[0].Files[0].Path, StringComparison.Ordinal);
        }

        [Fact]
        public void TestMultiVersionEpisodeWithAdditionalPartsPt()
        {
            // Stacked episode using "pt1" / "pt2" short form
            var files = new[]
            {
                "/TV/Show/Season 1/Show - S01E01 - 1080p.pt1.mkv",
                "/TV/Show/Season 1/Show - S01E01 - 1080p.pt2.mkv",
                "/TV/Show/Season 1/Show - S01E01 - 720p.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Single(result);
            Assert.Equal(2, result[0].Files.Count);
            Assert.Single(result[0].AlternateVersions);
            Assert.Contains("720p", result[0].AlternateVersions[0].Files[0].Path, StringComparison.Ordinal);
        }

        [Fact]
        public void TestMultiVersionEpisodeWithAdditionalPartsAndTitle()
        {
            // Stacked episode with episode title in filename
            var files = new[]
            {
                "/TV/Show/Season 1/Show - S01E01 - Pilot - 1080p part1.mkv",
                "/TV/Show/Season 1/Show - S01E01 - Pilot - 1080p part2.mkv",
                "/TV/Show/Season 1/Show - S01E01 - Pilot - 720p.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Single(result);
            // Primary should be the stacked 1080p version with 2 files
            Assert.Equal(2, result[0].Files.Count);
            Assert.Single(result[0].AlternateVersions);
            Assert.Contains("720p", result[0].AlternateVersions[0].Files[0].Path, StringComparison.Ordinal);
        }

        [Fact]
        public void TestMultiVersionEpisodeWithAdditionalPartsAndTitleDashSeparator()
        {
            // Stacked episode with episode title using "- part1" separator
            var files = new[]
            {
                "/TV/Show/Season 1/Show - S01E01 - Pilot - 1080p - part1.mkv",
                "/TV/Show/Season 1/Show - S01E01 - Pilot - 1080p - part2.mkv",
                "/TV/Show/Season 1/Show - S01E01 - Pilot - 720p.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Single(result);
            // Primary should be the stacked 1080p version with 2 files
            Assert.Equal(2, result[0].Files.Count);
            Assert.Single(result[0].AlternateVersions);
            Assert.Contains("720p", result[0].AlternateVersions[0].Files[0].Path, StringComparison.Ordinal);
        }

        [Fact]
        public void TestMultiVersionEpisodeWithAdditionalPartsAndMultipleEpisodes()
        {
            // Stacked episode alongside single-file version, plus a different episode
            var files = new[]
            {
                "/TV/Show/Season 1/Show - S01E01 - 1080p cd1.mkv",
                "/TV/Show/Season 1/Show - S01E01 - 1080p cd2.mkv",
                "/TV/Show/Season 1/Show - S01E01 - 720p.mkv",
                "/TV/Show/Season 1/Show - S01E02 - Other.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Equal(2, result.Count);

            // S01E01: stacked (cd1+cd2) primary with 720p alternate
            var e01 = result.FirstOrDefault(r => r.Files[0].Path.Contains("S01E01", StringComparison.Ordinal));
            Assert.NotNull(e01);
            Assert.Equal(2, e01!.Files.Count);
            Assert.Single(e01.AlternateVersions);

            // S01E02: standalone
            var e02 = result.FirstOrDefault(r => r.Files[0].Path.Contains("S01E02", StringComparison.Ordinal));
            Assert.NotNull(e02);
            Assert.Empty(e02!.AlternateVersions);
        }

        [Fact]
        public void TestMultiVersionEpisodePartStackAlongsideSingleFileResolutions()
        {
            // A part-stacked episode (3 parts, no resolution suffix) alongside single-file 720p and 1080p versions.
            // The multi-part stack is preferred as primary.
            var files = new[]
            {
                "/TV/Show/Season 1/S01E01 - 720p.mkv",
                "/TV/Show/Season 1/S01E01 - 1080p.mkv",
                "/TV/Show/Season 1/S01E01 - Part 1.mkv",
                "/TV/Show/Season 1/S01E01 - Part 2.mkv",
                "/TV/Show/Season 1/S01E01 - Part 3.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Single(result);
            Assert.Equal(3, result[0].Files.Count);
            Assert.All(result[0].Files, f => Assert.Contains("Part", f.Path, StringComparison.Ordinal));
            Assert.Equal(2, result[0].AlternateVersions.Count);
            Assert.Contains(result[0].AlternateVersions, f => f.Files[0].Path.Contains("1080p", StringComparison.Ordinal));
            Assert.Contains(result[0].AlternateVersions, f => f.Files[0].Path.Contains("720p", StringComparison.Ordinal));
        }

        [Fact]
        public void TestMultiVersionEpisodeTwoPartStacks()
        {
            // Two part-suffixed stacks of the same episode at different resolutions.
            // The 1080p stack is primary, the 720p stack is preserved as a multi-file alternate.
            var files = new[]
            {
                "/TV/Show/Season 1/Show - S01E01 - 1080p - part1.mkv",
                "/TV/Show/Season 1/Show - S01E01 - 1080p - part2.mkv",
                "/TV/Show/Season 1/Show - S01E01 - 720p - part1.mkv",
                "/TV/Show/Season 1/Show - S01E01 - 720p - part2.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Single(result);
            Assert.Equal(2, result[0].Files.Count);
            Assert.Contains("1080p", result[0].Files[0].Path, StringComparison.Ordinal);

            Assert.Single(result[0].AlternateVersions);
            var alt = result[0].AlternateVersions[0];
            Assert.Equal(2, alt.Files.Count);
            Assert.All(alt.Files, f => Assert.Contains("720p", f.Path, StringComparison.Ordinal));
        }

        [Fact]
        public void TestMultiVersionEpisodePartStackWithTrailer()
        {
            // A part-stacked multi-version episode alongside a trailer must not pull the trailer into the version group
            var files = new[]
            {
                "/TV/Show/Season 1/Show - S01E01 - 1080p part1.mkv",
                "/TV/Show/Season 1/Show - S01E01 - 1080p part2.mkv",
                "/TV/Show/Season 1/Show - S01E01 - 720p.mkv",
                "/TV/Show/Season 1/Show - S01E01-trailer.mp4"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Equal(2, result.Count);

            var episode = result.FirstOrDefault(r => r.ExtraType is null);
            Assert.NotNull(episode);
            Assert.Equal(2, episode!.Files.Count);
            Assert.Single(episode.AlternateVersions);
            Assert.Contains("720p", episode.AlternateVersions[0].Files[0].Path, StringComparison.Ordinal);

            var trailer = result.FirstOrDefault(r => r.ExtraType is not null);
            Assert.NotNull(trailer);
            Assert.Equal(ExtraType.Trailer, trailer!.ExtraType);
        }

        [Fact]
        public void TestMovieStackingWithPartNaming()
        {
            // Movie stacking with "part1"/"part2" naming
            var files = new[]
            {
                "/movies/Movie/Movie part1.mkv",
                "/movies/Movie/Movie part2.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result);
            Assert.Equal(2, result[0].Files.Count);
        }

        [Fact]
        public void TestMovieStackingWithDashPartNaming()
        {
            // Movie stacking with "- part1" / "- part2" dash separator
            var files = new[]
            {
                "/movies/Movie/Movie - part1.mkv",
                "/movies/Movie/Movie - part2.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result);
            Assert.Equal(2, result[0].Files.Count);
        }

        [Fact]
        public void TestMovieStackingWithPtNaming()
        {
            // Movie stacking with "pt1"/"pt2" short form
            var files = new[]
            {
                "/movies/Movie/Movie.pt1.mkv",
                "/movies/Movie/Movie.pt2.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result);
            Assert.Equal(2, result[0].Files.Count);
        }

        [Fact]
        public void TestMovieStackingWithHyphenNoSpaces()
        {
            // Movie stacking with hyphen directly adjacent to "part" (no spaces)
            var files = new[]
            {
                "/movies/Movie/Movie-part1.mkv",
                "/movies/Movie/Movie-part2.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result);
            Assert.Equal(2, result[0].Files.Count);
        }

        [Fact]
        public void TestMovieStackingWithHyphenNoSpacesAndVersion()
        {
            // Movie stacking with hyphen-no-space separators plus a version alternate
            var files = new[]
            {
                "/movies/Movie/Movie-1080p-part1.mkv",
                "/movies/Movie/Movie-1080p-part2.mkv",
                "/movies/Movie/Movie-720p.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result);
            // Stacked 1080p (2 files) should be primary, 720p is alternate
            Assert.Equal(2, result[0].Files.Count);
            Assert.Single(result[0].AlternateVersions);
        }

        [Fact]
        public void TestMovieMultiVersionWithStackedAlternate()
        {
            // Movie folder where the folder-named file is the primary (single file via primaryOverride)
            // and an alternate version is itself a stack. The stacked alternate must keep all its files.
            var files = new[]
            {
                "/movies/Inception (2010)/Inception (2010).mkv",
                "/movies/Inception (2010)/Inception (2010) - 4k part1.mkv",
                "/movies/Inception (2010)/Inception (2010) - 4k part2.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList()).ToList();

            Assert.Single(result);
            Assert.Single(result[0].Files);
            Assert.Equal("/movies/Inception (2010)/Inception (2010).mkv", result[0].Files[0].Path);

            Assert.Single(result[0].AlternateVersions);
            var stackedAlternate = result[0].AlternateVersions[0];
            Assert.Equal(2, stackedAlternate.Files.Count);
            Assert.All(stackedAlternate.Files, f => Assert.Contains("4k part", f.Path, StringComparison.Ordinal));
        }

        [Fact]
        public void TestEpisodeStackingWithHyphenNoSpaces()
        {
            // Episode stacking with hyphen-no-space separators plus version alternate
            var files = new[]
            {
                "/TV/Show/Season 1/Show - S01E01-1080p-cd1.mkv",
                "/TV/Show/Season 1/Show - S01E01-1080p-cd2.mkv",
                "/TV/Show/Season 1/Show - S01E01-720p.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Single(result);
            // Stacked 1080p (2 files) should be primary, 720p is alternate
            Assert.Equal(2, result[0].Files.Count);
            Assert.Single(result[0].AlternateVersions);
        }

        [Fact]
        public void TestEpisodeStackingWithHyphenNoSpacesAndTitle()
        {
            // Episode stacking with title and hyphen-no-space separators
            var files = new[]
            {
                "/TV/Show/Season 1/Show - S01E01 - Pilot-1080p-part1.mkv",
                "/TV/Show/Season 1/Show - S01E01 - Pilot-1080p-part2.mkv",
                "/TV/Show/Season 1/Show - S01E01 - Pilot-720p.mkv"
            };

            var result = _videoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                collectionType: CollectionType.tvshows).ToList();

            Assert.Single(result);
            // Stacked 1080p (2 files) should be primary, 720p is alternate
            Assert.Equal(2, result[0].Files.Count);
            Assert.Single(result[0].AlternateVersions);
        }
    }
}
