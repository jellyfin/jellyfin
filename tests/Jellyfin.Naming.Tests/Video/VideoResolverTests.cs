using System.Collections.Generic;
using System.Linq;
using Emby.Naming.Common;
using Emby.Naming.Video;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Naming.Tests.Video
{
    public class VideoResolverTests
    {
        private static NamingOptions _namingOptions = new NamingOptions();

        public static IEnumerable<object[]> ResolveFile_ValidFileNameTestData()
        {
            yield return new object[]
            {
                new VideoFileInfo(
                    path: @"/server/Movies/7 Psychos.mkv/7 Psychos.mkv",
                    container: "mkv",
                    name: "7 Psychos")
            };
            yield return new object[]
            {
                new VideoFileInfo(
                    path: @"/server/Movies/3 days to kill (2005)/3 days to kill (2005).mkv",
                    container: "mkv",
                    name: "3 days to kill",
                    year: 2005)
            };
            yield return new object[]
            {
                new VideoFileInfo(
                    path: @"/server/Movies/American Psycho/American.Psycho.mkv",
                    container: "mkv",
                    name: "American.Psycho")
            };
            yield return new object[]
            {
                new VideoFileInfo(
                    path: @"/server/Movies/brave (2007)/brave (2006).3d.sbs.mkv",
                    container: "mkv",
                    name: "brave",
                    year: 2006,
                    is3D: true,
                    format3D: "sbs")
            };
            yield return new object[]
            {
                new VideoFileInfo(
                    path: @"/server/Movies/300 (2007)/300 (2006).3d1.sbas.mkv",
                    container: "mkv",
                    name: "300",
                    year: 2006)
            };
            yield return new object[]
            {
                new VideoFileInfo(
                    path: @"/server/Movies/300 (2007)/300 (2006).3d.sbs.mkv",
                    container: "mkv",
                    name: "300",
                    year: 2006,
                    is3D: true,
                    format3D: "sbs")
            };
            yield return new object[]
            {
                new VideoFileInfo(
                    path: @"/server/Movies/brave (2007)/brave (2006)-trailer.bluray.disc",
                    container: "disc",
                    name: "brave",
                    year: 2006,
                    isStub: true,
                    stubType: "bluray")
            };
            yield return new object[]
            {
                new VideoFileInfo(
                    path: @"/server/Movies/300 (2007)/300 (2006)-trailer.bluray.disc",
                    container: "disc",
                    name: "300",
                    year: 2006,
                    isStub: true,
                    stubType: "bluray")
            };
            yield return new object[]
            {
                new VideoFileInfo(
                    path: @"/server/Movies/Brave (2007)/Brave (2006).bluray.disc",
                    container: "disc",
                    name: "Brave",
                    year: 2006,
                    isStub: true,
                    stubType: "bluray")
            };
            yield return new object[]
            {
                new VideoFileInfo(
                    path: @"/server/Movies/300 (2007)/300 (2006).bluray.disc",
                    container: "disc",
                    name: "300",
                    year: 2006,
                    isStub: true,
                    stubType: "bluray")
            };
            yield return new object[]
            {
                new VideoFileInfo(
                    path: @"/server/Movies/300 (2007)/300 (2006)-trailer.mkv",
                    container: "mkv",
                    name: "300",
                    year: 2006,
                    extraType: ExtraType.Trailer)
            };
            yield return new object[]
            {
                new VideoFileInfo(
                    path: @"/server/Movies/Brave (2007)/Brave (2006)-trailer.mkv",
                    container: "mkv",
                    name: "Brave",
                    year: 2006,
                    extraType: ExtraType.Trailer)
            };
            yield return new object[]
            {
                new VideoFileInfo(
                    path: @"/server/Movies/300 (2007)/300 (2006).mkv",
                    container: "mkv",
                    name: "300",
                    year: 2006)
            };
            yield return new object[]
            {
                new VideoFileInfo(
                    path: @"/server/Movies/Bad Boys (1995)/Bad Boys (1995).mkv",
                    container: "mkv",
                    name: "Bad Boys",
                    year: 1995)
            };
            yield return new object[]
            {
                new VideoFileInfo(
                    path: @"/server/Movies/Brave (2007)/Brave (2006).mkv",
                    container: "mkv",
                    name: "Brave",
                    year: 2006)
            };
            yield return new object[]
            {
                new VideoFileInfo(
                    path: @"/server/Movies/Rain Man 1988 REMASTERED 1080p BluRay x264 AAC - JEFF/Rain Man 1988 REMASTERED 1080p BluRay x264 AAC - JEFF.mp4",
                    container: "mp4",
                    name: "Rain Man",
                    year: 1988)
            };
        }

        [Theory]
        [MemberData(nameof(ResolveFile_ValidFileNameTestData))]
        public void ResolveFile_ValidFileName_Success(VideoFileInfo expectedResult)
        {
            var result = VideoResolver.ResolveFile(expectedResult.Path, _namingOptions);

            Assert.NotNull(result);
            Assert.Equal(result!.Path, expectedResult.Path);
            Assert.Equal(result.Container, expectedResult.Container);
            Assert.Equal(result.Name, expectedResult.Name);
            Assert.Equal(result.Year, expectedResult.Year);
            Assert.Equal(result.ExtraType, expectedResult.ExtraType);
            Assert.Equal(result.Format3D, expectedResult.Format3D);
            Assert.Equal(result.Is3D, expectedResult.Is3D);
            Assert.Equal(result.IsStub, expectedResult.IsStub);
            Assert.Equal(result.StubType, expectedResult.StubType);
            Assert.Equal(result.IsDirectory, expectedResult.IsDirectory);
            Assert.Equal(result.FileNameWithoutExtension.ToString(), expectedResult.FileNameWithoutExtension.ToString());
            Assert.Equal(result.ToString(), expectedResult.ToString());
        }

        [Fact]
        public void ResolveFile_EmptyPath()
        {
            var result = VideoResolver.ResolveFile(string.Empty, _namingOptions);

            Assert.Null(result);
        }

        [Fact]
        public void ResolveDirectoryTest()
        {
            var paths = new[]
            {
                @"/Server/Iron Man",
                @"Batman",
                string.Empty
            };

            var results = paths.Select(path => VideoResolver.ResolveDirectory(path, _namingOptions)).ToList();

            Assert.Equal(3, results.Count);
            Assert.NotNull(results[0]);
            Assert.NotNull(results[1]);
            Assert.Null(results[2]);
            foreach (var result in results)
            {
                Assert.Null(result?.Container);
            }
        }
    }
}
