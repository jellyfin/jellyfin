using System;
using Emby.Server.Implementations.Library;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library
{
    public class PathExtensionsTests
    {
        [Theory]
        [InlineData("C:/Users/jeff/myfile.mkv", "C:/Users/jeff", "/home/jeff", "/home/jeff/myfile.mkv")]
        [InlineData("C:/Users/jeff/myfile.mkv", "C:/Users/jeff/", "/home/jeff", "/home/jeff/myfile.mkv")]
        [InlineData("/home/jeff/music/jeff's band/consistently inconsistent.mp3", "/home/jeff/music/jeff's band", "/home/not jeff", "/home/not jeff/consistently inconsistent.mp3")]
        [InlineData("C:\\Users\\jeff\\myfile.mkv", "C:\\Users/jeff", "/home/jeff", "/home/jeff/myfile.mkv")]
        [InlineData("C:\\Users\\jeff\\myfile.mkv", "C:\\Users/jeff", "/home/jeff/", "/home/jeff/myfile.mkv")]
        [InlineData("C:\\Users\\jeff\\myfile.mkv", "C:\\Users/jeff/", "/home/jeff/", "/home/jeff/myfile.mkv")]
        [InlineData("C:\\Users\\jeff\\myfile.mkv", "C:\\Users/jeff/", "/", "/myfile.mkv")]
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
    }
}
