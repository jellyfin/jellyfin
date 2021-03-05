using System;
using Emby.Server.Implementations.Library;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library
{
    public class PathExtensionsTests
    {
        [Theory]
        [InlineData("Superman: Red Son [imdbid=tt10985510]", "imdbid", "tt10985510")]
        [InlineData("Superman: Red Son - tt10985510", "imdbid", "tt10985510")]
        [InlineData("Superman: Red Son", "imdbid", null)]
        [InlineData("Superman: Red Son", "something", null)]
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
        [InlineData("C:/Users/jeff/myfile.mkv", "C:/Users/jeff", "/home/jeff", true, "/home/jeff/myfile.mkv")]
        [InlineData("C:/Users/jeff/myfile.mkv", "C:/Users/jeff/", "/home/jeff", true, "/home/jeff/myfile.mkv")]
        [InlineData("/home/jeff/music/jeff's band/consistently inconsistent.mp3", "/home/jeff/music/not jeff's band", "/home/not jeff", false, null)]
        [InlineData("/home/jeff/music/jeff's band/consistently inconsistent.mp3", "/home/jeff/music/jeff's band", "/home/not jeff", true, "/home/not jeff/consistently inconsistent.mp3")]
        [InlineData("C:\\Users\\jeff\\myfile.mkv", "C:\\Users/jeff", "/home/jeff", true, "/home/jeff/myfile.mkv")]
        public void TryReplaceSubPath_ValidArgs_Correct(string path, string subPath, string newSubPath, bool succeeded, string? expectedResult)
        {
            var status = PathExtensions.TryReplaceSubPath(path, subPath, newSubPath, out var result);
            Assert.Equal(succeeded, status);
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("", "", "")]
        [InlineData("/my/path", "", "")]
        [InlineData("", "/another/path", "")]
        [InlineData("", "", "/new/subpath")]
        public void TryReplaceSubPath_EmptyString_ThrowsArgumentNullException(string path, string subPath, string newSubPath)
        {
            Assert.Throws<ArgumentNullException>(() => PathExtensions.TryReplaceSubPath(path, subPath, newSubPath, out _));
        }
    }
}
