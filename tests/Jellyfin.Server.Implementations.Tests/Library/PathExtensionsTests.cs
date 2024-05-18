using System;
using System.IO;
using Emby.Server.Implementations.Library;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library
{
    public class PathExtensionsTests
    {
        [Theory]
        [InlineData("Superman: Red Son [imdbid=tt10985510]", "imdbid", "tt10985510")]
        [InlineData("Superman: Red Son [imdbid-tt10985510]", "imdbid", "tt10985510")]
        [InlineData("Superman: Red Son - tt10985510", "imdbid", "tt10985510")]
        [InlineData("Superman: Red Son", "imdbid", null)]
        [InlineData("Superman: Red Son", "something", null)]
        [InlineData("Superman: Red Son [imdbid1=tt11111111][imdbid=tt10985510]", "imdbid", "tt10985510")]
        [InlineData("Superman: Red Son [imdbid1-tt11111111][imdbid=tt10985510]", "imdbid", "tt10985510")]
        [InlineData("Superman: Red Son [tmdbid=618355][imdbid=tt10985510]", "imdbid", "tt10985510")]
        [InlineData("Superman: Red Son [tmdbid-618355][imdbid-tt10985510]", "imdbid", "tt10985510")]
        [InlineData("Superman: Red Son [tmdbid-618355][imdbid-tt10985510]", "tmdbid", "618355")]
        [InlineData("Superman: Red Son [providera-id=1]", "providera-id", "1")]
        [InlineData("Superman: Red Son [providerb-id=2]", "providerb-id", "2")]
        [InlineData("Superman: Red Son [providera id=4]", "providera id", "4")]
        [InlineData("Superman: Red Son [providerb id=5]", "providerb id", "5")]
        [InlineData("Superman: Red Son [tmdbid=3]", "tmdbid", "3")]
        [InlineData("Superman: Red Son [tvdbid-6]", "tvdbid", "6")]
        [InlineData("[tmdbid=618355]", "tmdbid", "618355")]
        [InlineData("[tmdbid-618355]", "tmdbid", "618355")]
        [InlineData("tmdbid=111111][tmdbid=618355]", "tmdbid", "618355")]
        [InlineData("[tmdbid=618355]tmdbid=111111]", "tmdbid", "618355")]
        [InlineData("tmdbid=618355]", "tmdbid", null)]
        [InlineData("[tmdbid=618355", "tmdbid", null)]
        [InlineData("tmdbid=618355", "tmdbid", null)]
        [InlineData("tmdbid=", "tmdbid", null)]
        [InlineData("tmdbid", "tmdbid", null)]
        [InlineData("[tmdbid=][imdbid=tt10985510]", "tmdbid", null)]
        [InlineData("[tmdbid-][imdbid-tt10985510]", "tmdbid", null)]
        [InlineData("Superman: Red Son [tmdbid-618355][tmdbid=1234567]", "tmdbid", "618355")]
        public void GetAttributeValue_ValidArgs_Correct(string input, string attribute, string? expectedResult)
        {
            Assert.Equal(expectedResult, PathExtensions.GetAttributeValue(input, attribute));
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("Superman: Red Son [imdbid=tt10985510]", "")]
        [InlineData("", "imdbid")]
        public void GetAttributeValue_EmptyString_ThrowsArgumentException(string input, string attribute)
        {
            Assert.Throws<ArgumentException>(() => PathExtensions.GetAttributeValue(input, attribute));
        }

        [Theory]
        [InlineData("C:/Users/jeff/myfile.mkv", "C:/Users/jeff", "/home/jeff", "/home/jeff/myfile.mkv")]
        [InlineData("C:/Users/jeff/myfile.mkv", "C:/Users/jeff/", "/home/jeff", "/home/jeff/myfile.mkv")]
        [InlineData("/home/jeff/music/jeff's band/consistently inconsistent.mp3", "/home/jeff/music/jeff's band", "/home/not jeff", "/home/not jeff/consistently inconsistent.mp3")]
        [InlineData(@"C:\Users\jeff\myfile.mkv", "C:\\Users/jeff", "/home/jeff", "/home/jeff/myfile.mkv")]
        [InlineData(@"C:\Users\jeff\myfile.mkv", "C:\\Users/jeff", "/home/jeff/", "/home/jeff/myfile.mkv")]
        [InlineData(@"C:\Users\jeff\myfile.mkv", "C:\\Users/jeff/", "/home/jeff/", "/home/jeff/myfile.mkv")]
        [InlineData(@"C:\Users\jeff\myfile.mkv", "C:\\Users/jeff/", "/", "/myfile.mkv")]
        [InlineData("/o", "/o", "/s", "/s")] // regression test for #5977
        public void TryReplaceSubPath_ValidArgs_Correct(string path, string subPath, string newSubPath, string? expectedResult)
        {
            Assert.True(PathExtensions.TryReplaceSubPath(path, subPath, newSubPath, out var result));
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData(null, null, null)]
        [InlineData(null, "/my/path", "/another/path")]
        [InlineData("/my/path", null, "/another/path")]
        [InlineData("/my/path", "/another/path", null)]
        [InlineData("", "", "")]
        [InlineData("/my/path", "", "")]
        [InlineData("", "/another/path", "")]
        [InlineData("", "", "/new/subpath")]
        [InlineData("/home/jeff/music/jeff's band/consistently inconsistent.mp3", "/home/jeff/music/not jeff's band", "/home/not jeff")]
        public void TryReplaceSubPath_InvalidInput_ReturnsFalseAndNull(string? path, string? subPath, string? newSubPath)
        {
            Assert.False(PathExtensions.TryReplaceSubPath(path, subPath, newSubPath, out var result));
            Assert.Null(result);
        }

        [Theory]
        [InlineData(null, '/', null)]
        [InlineData(null, '\\', null)]
        [InlineData("/home/jeff/myfile.mkv", '\\', @"\home\jeff\myfile.mkv")]
        [InlineData(@"C:\Users\Jeff\myfile.mkv", '/', "C:/Users/Jeff/myfile.mkv")]
        [InlineData(@"\home/jeff\myfile.mkv", '\\', @"\home\jeff\myfile.mkv")]
        [InlineData(@"\home/jeff\myfile.mkv", '/', "/home/jeff/myfile.mkv")]
        [InlineData("", '/', "")]
        public void NormalizePath_SpecifyingSeparator_Normalizes(string? path, char separator, string? expectedPath)
        {
            Assert.Equal(expectedPath, path.NormalizePath(separator));
        }

        [Theory]
        [InlineData("/home/jeff/myfile.mkv")]
        [InlineData(@"C:\Users\Jeff\myfile.mkv")]
        [InlineData(@"\home/jeff\myfile.mkv")]
        public void NormalizePath_NoArgs_UsesDirectorySeparatorChar(string path)
        {
            var separator = Path.DirectorySeparatorChar;

            Assert.Equal(path.Replace('\\', separator).Replace('/', separator), path.NormalizePath());
        }

        [Theory]
        [InlineData("/home/jeff/myfile.mkv", '/')]
        [InlineData(@"C:\Users\Jeff\myfile.mkv", '\\')]
        [InlineData(@"\home/jeff\myfile.mkv", '/')]
        public void NormalizePath_OutVar_Correct(string path, char expectedSeparator)
        {
            var result = path.NormalizePath(out var separator);

            Assert.Equal(expectedSeparator, separator);
            Assert.Equal(path.Replace('\\', separator).Replace('/', separator), result);
        }

        [Fact]
        public void NormalizePath_SpecifyInvalidSeparator_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => string.Empty.NormalizePath('a'));
        }
    }
}
