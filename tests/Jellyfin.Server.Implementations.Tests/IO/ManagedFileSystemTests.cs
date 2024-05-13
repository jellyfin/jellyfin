using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Server.Implementations.IO;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.IO
{
    public class ManagedFileSystemTests
    {
        private readonly IFixture _fixture;
        private readonly ManagedFileSystem _sut;

        public ManagedFileSystemTests()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
            _sut = _fixture.Create<ManagedFileSystem>();
        }

        [SkippableTheory]
        [InlineData("/Volumes/Library/Sample/Music/Playlists/", "../Beethoven/Misc/Moonlight Sonata.mp3", "/Volumes/Library/Sample/Music/Beethoven/Misc/Moonlight Sonata.mp3")]
        [InlineData("/Volumes/Library/Sample/Music/Playlists/", "../../Beethoven/Misc/Moonlight Sonata.mp3", "/Volumes/Library/Sample/Beethoven/Misc/Moonlight Sonata.mp3")]
        [InlineData("/Volumes/Library/Sample/Music/Playlists/", "Beethoven/Misc/Moonlight Sonata.mp3", "/Volumes/Library/Sample/Music/Playlists/Beethoven/Misc/Moonlight Sonata.mp3")]
        [InlineData("/Volumes/Library/Sample/Music/Playlists/", "/mnt/Beethoven/Misc/Moonlight Sonata.mp3", "/mnt/Beethoven/Misc/Moonlight Sonata.mp3")]
        public void MakeAbsolutePathCorrectlyHandlesRelativeFilePathsOnUnixLike(
            string folderPath,
            string filePath,
            string expectedAbsolutePath)
        {
            Skip.If(OperatingSystem.IsWindows());

            var generatedPath = _sut.MakeAbsolutePath(folderPath, filePath);
            Assert.Equal(expectedAbsolutePath, generatedPath);
        }

        [SkippableTheory]
        [InlineData(@"C:\\Volumes\Library\Sample\Music\Playlists\", @"..\Beethoven\Misc\Moonlight Sonata.mp3", @"C:\Volumes\Library\Sample\Music\Beethoven\Misc\Moonlight Sonata.mp3")]
        [InlineData(@"C:\\Volumes\Library\Sample\Music\Playlists\", @"..\..\Beethoven\Misc\Moonlight Sonata.mp3", @"C:\Volumes\Library\Sample\Beethoven\Misc\Moonlight Sonata.mp3")]
        [InlineData(@"C:\\Volumes\Library\Sample\Music\Playlists\", @"Beethoven\Misc\Moonlight Sonata.mp3", @"C:\Volumes\Library\Sample\Music\Playlists\Beethoven\Misc\Moonlight Sonata.mp3")]
        [InlineData(@"C:\\Volumes\Library\Sample\Music\Playlists\", @"D:\\Beethoven\Misc\Moonlight Sonata.mp3", @"D:\\Beethoven\Misc\Moonlight Sonata.mp3")]
        public void MakeAbsolutePathCorrectlyHandlesRelativeFilePathsOnWindows(
            string folderPath,
            string filePath,
            string expectedAbsolutePath)
        {
            Skip.If(!OperatingSystem.IsWindows());

            var generatedPath = _sut.MakeAbsolutePath(folderPath, filePath);

            Assert.Equal(expectedAbsolutePath, generatedPath);
        }

        [Theory]
        [InlineData("ValidFileName", "ValidFileName")]
        [InlineData("AC/DC", "AC DC")]
        [InlineData("Invalid\0", "Invalid ")]
        [InlineData("AC/DC\0KD/A", "AC DC KD A")]
        public void GetValidFilename_ReturnsValidFilename(string filename, string expectedFileName)
        {
            Assert.Equal(expectedFileName, _sut.GetValidFilename(filename));
        }

        [SkippableFact]
        public void GetFileInfo_DanglingSymlink_ExistsFalse()
        {
            Skip.If(OperatingSystem.IsWindows());

            string testFileDir = Path.Combine(Path.GetTempPath(), "jellyfin-test-data");
            string testFileName = Path.Combine(testFileDir, Path.GetRandomFileName() + "-danglingsym.link");

            Directory.CreateDirectory(testFileDir);
            Assert.Equal(0, symlink("thispathdoesntexist", testFileName));
            Assert.True(File.Exists(testFileName));

            var metadata = _sut.GetFileInfo(testFileName);
            Assert.False(metadata.Exists);
        }

        [SuppressMessage("Naming Rules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Have to")]
        [DllImport("libc", SetLastError = true, CharSet = CharSet.Ansi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.UserDirectories)]
        private static extern int symlink(string target, string linkpath);
    }
}
