using Emby.Naming.Common;
using Emby.Naming.Video;
using Xunit;

namespace Jellyfin.Naming.Tests.Video
{
    public class StubTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        [Fact]
        public void TestStubs()
        {
            Test("video.mkv", false, null);
            Test("video.disc", true, null);
            Test("video.dvd.disc", true, "dvd");
            Test("video.hddvd.disc", true, "hddvd");
            Test("video.bluray.disc", true, "bluray");
            Test("video.brrip.disc", true, "bluray");
            Test("video.bd25.disc", true, "bluray");
            Test("video.bd50.disc", true, "bluray");
            Test("video.vhs.disc", true, "vhs");
            Test("video.hdtv.disc", true, "tv");
            Test("video.pdtv.disc", true, "tv");
            Test("video.dsr.disc", true, "tv");
            Test(string.Empty, false, "tv");
        }

        [Fact]
        public void TestStubName()
        {
            var result = VideoResolver.ResolveFile("C:/Users/media/Desktop/Video Test/Movies/Oblivion/Oblivion.dvd.disc", _namingOptions);

            Assert.Equal("Oblivion", result?.Name);
        }

        private void Test(string path, bool isStub, string? stubType)
        {
            var isStubResult = StubResolver.TryResolveFile(path, _namingOptions, out var stubTypeResult);

            Assert.Equal(isStub, isStubResult);

            if (isStub)
            {
                Assert.Equal(stubType, stubTypeResult);
            }
            else
            {
                Assert.Null(stubTypeResult);
            }
        }
    }
}
