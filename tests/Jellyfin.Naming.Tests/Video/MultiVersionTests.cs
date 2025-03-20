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

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.movies).ToList();

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

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.movies).ToList();

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

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.movies).ToList();

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

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.movies).ToList();

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

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.movies).ToList();

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

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.movies).ToList();

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

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.movies).ToList();

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

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.movies).ToList();

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

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.movies).ToList();

            Assert.Single(result);
            Assert.Equal("/movies/Iron Man/Iron Man.mkv", result[0].Files[0].Path);
            Assert.Equal(6, result[0].AlternateVersions.Count);
            Assert.Equal("/movies/Iron Man/Iron Man-720p.mkv", result[0].AlternateVersions[0].Path);
            Assert.Equal("/movies/Iron Man/Iron Man-3d.mkv", result[0].AlternateVersions[1].Path);
            Assert.Equal("/movies/Iron Man/Iron Man-3d-hsbs.mkv", result[0].AlternateVersions[2].Path);
            Assert.Equal("/movies/Iron Man/Iron Man-bluray.mkv", result[0].AlternateVersions[3].Path);
            Assert.Equal("/movies/Iron Man/Iron Man-test.mkv", result[0].AlternateVersions[4].Path);
            Assert.Equal("/movies/Iron Man/Iron Man[test].mkv", result[0].AlternateVersions[5].Path);
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

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.movies).ToList();

            Assert.Single(result);
            Assert.Equal("/movies/Iron Man/Iron Man.mkv", result[0].Files[0].Path);
            Assert.Equal(6, result[0].AlternateVersions.Count);
            Assert.Equal("/movies/Iron Man/Iron Man - 720p.mkv", result[0].AlternateVersions[0].Path);
            Assert.Equal("/movies/Iron Man/Iron Man - 3d.mkv", result[0].AlternateVersions[1].Path);
            Assert.Equal("/movies/Iron Man/Iron Man - 3d-hsbs.mkv", result[0].AlternateVersions[2].Path);
            Assert.Equal("/movies/Iron Man/Iron Man - bluray.mkv", result[0].AlternateVersions[3].Path);
            Assert.Equal("/movies/Iron Man/Iron Man - test.mkv", result[0].AlternateVersions[4].Path);
            Assert.Equal("/movies/Iron Man/Iron Man [test].mkv", result[0].AlternateVersions[5].Path);
        }

        [Fact]
        public void TestMultiVersion7()
        {
            var files = new[]
            {
                "/movies/Iron Man/Iron Man - B (2006).mkv",
                "/movies/Iron Man/Iron Man - C (2007).mkv"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.movies).ToList();

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

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.movies).ToList();

            Assert.Equal(7, result.Count);
            Assert.Empty(result[0].AlternateVersions);
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

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.movies).ToList();

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

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.movies);

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

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.movies);

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

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.movies);

            Assert.Single(result);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016).mkv", result[0].Files[0].Path);
            Assert.Equal(5, result[0].AlternateVersions.Count);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 2160p.mkv", result[0].AlternateVersions[0].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 1080p.mkv", result[0].AlternateVersions[1].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 720p.mkv", result[0].AlternateVersions[2].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - Directors Cut.mkv", result[0].AlternateVersions[3].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - Theatrical Release.mkv", result[0].AlternateVersions[4].Path);
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

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.movies).ToList();

            Assert.Single(result);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016).mkv", result[0].Files[0].Path);
            Assert.Equal(11, result[0].AlternateVersions.Count);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 2160p.mkv", result[0].AlternateVersions[0].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 2160p Remux.mkv", result[0].AlternateVersions[1].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 1080p.mkv", result[0].AlternateVersions[2].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 1080p Directors Cut.mkv", result[0].AlternateVersions[3].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 1080p High Bitrate.mkv", result[0].AlternateVersions[4].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 1080p Remux.mkv", result[0].AlternateVersions[5].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 1080p Theatrical Release.mkv", result[0].AlternateVersions[6].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 720p.mkv", result[0].AlternateVersions[7].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - 720p Directors Cut.mkv", result[0].AlternateVersions[8].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - Directors Cut.mkv", result[0].AlternateVersions[9].Path);
            Assert.Equal("/movies/X-Men Apocalypse (2016)/X-Men Apocalypse (2016) - Theatrical Release.mkv", result[0].AlternateVersions[10].Path);
        }

        [Fact]
        public void Resolve_GivenFolderNameWithBracketsAndHyphens_GroupsBasedOnFolderName()
        {
            var files = new[]
            {
                "/movies/John Wick - Kapitel 3 (2019) [imdbid=tt6146586]/John Wick - Kapitel 3 (2019) [imdbid=tt6146586] - Version 1.mkv",
                "/movies/John Wick - Kapitel 3 (2019) [imdbid=tt6146586]/John Wick - Kapitel 3 (2019) [imdbid=tt6146586] - Version 2.mkv"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.movies);

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

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.movies);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public void TestEmptyList()
        {
            var result = VideoListResolver.Resolve(new List<VideoFileInfo>(), _namingOptions, null).ToList();

            Assert.Empty(result);
        }

        [Fact]
        public void TestMultiVersionEpisodeDontCollapse()
        {
            // Test for false positive

            var files = new[]
            {
                @"/TV/Dexter/Dexter - S01E01 - One.mkv",
                @"/TV/Dexter/Dexter - S01E02 - Two.mkv",
                @"/TV/Dexter/Dexter - S01E03 - Three.mkv",
                @"/TV/Dexter/Dexter - S01E04 - Four.mkv",
                @"/TV/Dexter/Dexter - S01E05 - Five.mkv",
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions, false)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.tvshows);

            Assert.Equal(5, result.Count);
            Assert.Empty(result[0].AlternateVersions);
        }

        [Fact]
        public void TestMultiVersionEpisodeDontCollapse2()
        {
            // Test for false positive

            var files = new[]
            {
                @"/TV/Dexter/Dexter - S01E01 One.mkv",
                @"/TV/Dexter/Dexter - S01E02 Two.mkv",
                @"/TV/Dexter/Dexter - S01E03 Three.mkv",
                @"/TV/Dexter/Dexter - S01E04 Four.mkv",
                @"/TV/Dexter/Dexter - S01E05 Five.mkv",
                @"/TV/Star Trek- Picard/Season 3/Star Trek - Picard 3x01 [WEBDL-720p Proper x264][EAC3 5.1] - Part One - The Next Generation.mkv",
                @"/TV/Star Trek- Picard/Season 3/Star Trek - Picard 3x02 [WEBDL-720p Proper x264][EAC3 5.1] - Part Two - Disengage.mkv",
                @"/TV/Star Trek- Picard/Season 3/Star Trek - Picard 3x03 [WEBDL-720p x264][EAC3 5.1] - Part Three - Seventeen Seconds.mkv",
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions, false)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.tvshows);

            Assert.Equal(8, result.Count);
            Assert.Empty(result[0].AlternateVersions);
        }

        [Fact]
        public void TestMultiVersionEpisode()
        {
            var files = new[]
            {
                @"/TV/Dexter/Dexter - S01E01/Dexter - S01E01 - One.mkv",
                @"/TV/Dexter/Dexter - S01E01/Dexter - S01E01 - Two.mkv",
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions, false)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.tvshows);

            Assert.Single(result);
            Assert.Single(result[0].AlternateVersions);
        }

        [Fact]
        public void TestMultiVersionEpisodeMixedSeriesFolder()
        {
            var files = new[]
            {
                @"/TV/Dexter/Dexter - S01E01.mkv",
                @"/TV/Dexter/Dexter - S01E01 - Unaired.mkv",
                @"/TV/Dexter/Dexter - S01E02 - Two.mkv",
                @"/TV/Dexter/Dexter - S01E03 - Three.mkv",
                @"/TV/Dexter/Dexter S02E01 - Ia.mkv",
                @"/TV/Dexter/Dexter S02E01 - I.mkv",
                @"/TV/Dexter/Dexter - S02E02.mkv",
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions, false)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.tvshows);

            Assert.Equal(5, result.Count);
            Assert.Single(result[0].AlternateVersions);
            Assert.Empty(result[1].AlternateVersions);
            Assert.Empty(result[2].AlternateVersions);
            Assert.Empty(result[3].AlternateVersions);

            var s02e01 = result.FirstOrDefault(x => string.Equals(x.Name, "Dexter S02E01", StringComparison.Ordinal));
            Assert.NotNull(s02e01);
            Assert.Single(s02e01!.AlternateVersions);
        }

        [Fact]
        public void TestMultiVersionEpisodeMixedSeasonFolder()
        {
            var files = new[]
            {
                @"/TV/Dexter/Season 2/Dexter - S02E01 - Ia.mkv",
                @"/TV/Dexter/Season 2/Dexter - S02E01 - I.mkv",
                @"/TV/Dexter/Season 2/Dexter - S02E02.mkv",
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions, false)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.tvshows);

            Assert.Equal(2, result.Count);
            Assert.Single(result[0].AlternateVersions);
            Assert.Empty(result[1].AlternateVersions);
        }

        [Fact]
        public void TestMultiVersionEpisodeMixedSeasonFolderWithYear()
        {
            var files = new[]
            {
                @"/TV/Name (2020)/Season 1/Name (2020) - S01E01 - [ORIGINAL].mkv",
                @"/TV/Name (2020)/Season 1/Name (2020) - S01E01 - [VERSION].mkv",
                @"/TV/Name (2020)/Season 1/Name (2020) - S01E02 - [ORIGINAL].mkv",
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions, false)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.tvshows);

            Assert.Equal(2, result.Count);
            Assert.Single(result[0].AlternateVersions);
            Assert.Empty(result[1].AlternateVersions);
        }

        [Fact]
        public void TestMultiVersionEpisodeMixedSeasonFolderWithYearAndDirtyNames()
        {
            var files = new[]
            {
                @"/TV/Name (2020)/Season 1/Name (2020) - S01E01 [BluRay-480p x264][AC3 2.0] - [ORIGINAL].mkv",
                @"/TV/Name (2020)/Season 1/Name (2020) - S01E01 [BluRay-1080p x264][AC3 5.1] - [Remaster].mkv",
                @"/TV/Name (2020)/Season 1/Name (2020) - S01E02 - [ORIGINAL].mkv",
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions, false)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.tvshows);

            Assert.Equal(2, result.Count);
            Assert.Single(result[0].AlternateVersions);
            Assert.Empty(result[1].AlternateVersions);
        }

        [Fact]
        public void TestMultiVersionEpisodeABCD()
        {
            var files = new[]
            {
                @"/TV/SeriesName/SeriesName- S01E01 - EpisodeTitle - [VersionA].mkv",
                @"/TV/SeriesName/SeriesName- S01E01 - EpisodeTitle - [VersionB].mkv",
                @"/TV/SeriesName/SeriesName- S01E01 - EpisodeTitle - VersionC.mkv",
                @"/TV/SeriesName/SeriesName- S01E01 - EpisodeTitle - VersionD.mkv"
            };

            var result = VideoListResolver.Resolve(
                files.Select(i => VideoResolver.Resolve(i, false, _namingOptions, false)).OfType<VideoFileInfo>().ToList(),
                _namingOptions,
                CollectionType.tvshows);

            Assert.Single(result);
            Assert.Equal(3, result[0].AlternateVersions.Count);
        }
    }
}
