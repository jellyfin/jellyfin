using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Server.Implementations.IO;
using Xunit;

namespace Emby.Server.Implementations.Tests.IO
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

        [Theory]
        [InlineData("/Volumes/Library/Sample/Music/Playlists/", "../Beethoven/Misc/Moonlight Sonata.mp3", "/Volumes/Library/Sample/Music/Beethoven/Misc/Moonlight Sonata.mp3")]
        [InlineData("/Volumes/Library/Sample/Music/Playlists/", "../../Beethoven/Misc/Moonlight Sonata.mp3", "/Volumes/Library/Sample/Beethoven/Misc/Moonlight Sonata.mp3")]
        [InlineData("/Volumes/Library/Sample/Music/Playlists/", "Beethoven/Misc/Moonlight Sonata.mp3", "/Volumes/Library/Sample/Music/Playlists/Beethoven/Misc/Moonlight Sonata.mp3")]
        public void MakeAbsolutePathCorrectlyHandlesRelativeFilePaths(
            string folderPath,
            string filePath,
            string expectedAbsolutePath)
        {
            var generatedPath = _sut.MakeAbsolutePath(folderPath, filePath);
            Assert.Equal(expectedAbsolutePath, generatedPath);
        }
    }
}
