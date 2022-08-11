using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Server.Implementations.IO;
using Emby.Server.Implementations.Library;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library
{
    public class MediaSourceManagerTests
    {
        private readonly MediaSourceManager _mediaSourceManager;

        public MediaSourceManagerTests()
        {
            IFixture fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
            fixture.Inject<IFileSystem>(fixture.Create<ManagedFileSystem>());
            _mediaSourceManager = fixture.Create<MediaSourceManager>();
        }

        [Theory]
        [InlineData(@"C:\mydir\myfile.ext", MediaProtocol.File)]
        [InlineData("/mydir/myfile.ext", MediaProtocol.File)]
        [InlineData("file:///mydir/myfile.ext", MediaProtocol.File)]
        [InlineData("http://example.com/stream.m3u8", MediaProtocol.Http)]
        [InlineData("https://example.com/stream.m3u8", MediaProtocol.Http)]
        [InlineData("rtsp://media.example.com:554/twister/audiotrack", MediaProtocol.Rtsp)]
        public void GetPathProtocol_ValidArg_Correct(string path, MediaProtocol expected)
            => Assert.Equal(expected, _mediaSourceManager.GetPathProtocol(path));
    }
}
