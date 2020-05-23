using Emby.Naming.Common;
using Emby.Naming.Video;
using Xunit;

namespace Jellyfin.Naming.Tests.Video
{
    public class Format3DTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        [Fact]
        public void TestKodiFormat3D()
        {
            Test("Super movie.3d.mp4", false, null);
            Test("Super movie.3d.hsbs.mp4", true, "hsbs");
            Test("Super movie.3d.sbs.mp4", true, "sbs");
            Test("Super movie.3d.htab.mp4", true, "htab");
            Test("Super movie.3d.tab.mp4", true, "tab");
            Test("Super movie 3d hsbs.mp4", true, "hsbs");
        }

        [Fact]
        public void Test3DName()
        {
            var result =
                new VideoResolver(_namingOptions).ResolveFile(@"C:/Users/media/Desktop/Video Test/Movies/Oblivion/Oblivion.3d.hsbs.mkv");

            Assert.Equal("hsbs", result?.Format3D);
            Assert.Equal("Oblivion", result?.Name);
        }

        [Fact]
        public void TestExpandedFormat3D()
        {
            // These were introduced for Media Browser 3
            // Kodi conventions are preferred but these still need to be supported

            Test("Super movie.3d.mp4", false, null);
            Test("Super movie.3d.hsbs.mp4", true, "hsbs");
            Test("Super movie.3d.sbs.mp4", true, "sbs");
            Test("Super movie.3d.htab.mp4", true, "htab");
            Test("Super movie.3d.tab.mp4", true, "tab");

            Test("Super movie.hsbs.mp4", true, "hsbs");
            Test("Super movie.sbs.mp4", true, "sbs");
            Test("Super movie.htab.mp4", true, "htab");
            Test("Super movie.tab.mp4", true, "tab");
            Test("Super movie.sbs3d.mp4", true, "sbs3d");
            Test("Super movie.3d.mvc.mp4", true, "mvc");

            Test("Super movie [3d].mp4", false, null);
            Test("Super movie [hsbs].mp4", true, "hsbs");
            Test("Super movie [fsbs].mp4", true, "fsbs");
            Test("Super movie [ftab].mp4", true, "ftab");
            Test("Super movie [htab].mp4", true, "htab");
            Test("Super movie [sbs3d].mp4", true, "sbs3d");
        }

        private void Test(string input, bool is3D, string? format3D)
        {
            var parser = new Format3DParser(_namingOptions);

            var result = parser.Parse(input);

            Assert.Equal(is3D, result.Is3D);

            if (format3D == null)
            {
                Assert.Null(result.Format3D);
            }
            else
            {
                Assert.Equal(format3D, result.Format3D, true);
            }
        }
    }
}
