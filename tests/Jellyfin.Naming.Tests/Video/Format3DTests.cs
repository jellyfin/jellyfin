using Emby.Naming.Common;
using Emby.Naming.Video;
using Xunit;

namespace Jellyfin.Naming.Tests.Video
{
    public class Format3DTests : BaseVideoTest
    {
        [Fact]
        public void TestKodiFormat3D()
        {
            var options = new NamingOptions();

            Test("Super movie.3d.mp4", false, null, options);
            Test("Super movie.3d.hsbs.mp4", true, "hsbs", options);
            Test("Super movie.3d.sbs.mp4", true, "sbs", options);
            Test("Super movie.3d.htab.mp4", true, "htab", options);
            Test("Super movie.3d.tab.mp4", true, "tab", options);
            Test("Super movie 3d hsbs.mp4", true, "hsbs", options);
        }

        [Fact]
        public void Test3DName()
        {
            var result =
                GetParser().ResolveFile(@"C:/Users/media/Desktop/Video Test/Movies/Oblivion/Oblivion.3d.hsbs.mkv");

            Assert.Equal("hsbs", result.Format3D);
            Assert.Equal("Oblivion", result.Name);
        }

        [Fact]
        public void TestExpandedFormat3D()
        {
            // These were introduced for Media Browser 3
            // Kodi conventions are preferred but these still need to be supported
            var options = new NamingOptions();

            Test("Super movie.3d.mp4", false, null, options);
            Test("Super movie.3d.hsbs.mp4", true, "hsbs", options);
            Test("Super movie.3d.sbs.mp4", true, "sbs", options);
            Test("Super movie.3d.htab.mp4", true, "htab", options);
            Test("Super movie.3d.tab.mp4", true, "tab", options);

            Test("Super movie.hsbs.mp4", true, "hsbs", options);
            Test("Super movie.sbs.mp4", true, "sbs", options);
            Test("Super movie.htab.mp4", true, "htab", options);
            Test("Super movie.tab.mp4", true, "tab", options);
            Test("Super movie.sbs3d.mp4", true, "sbs3d", options);
            Test("Super movie.3d.mvc.mp4", true, "mvc", options);

            Test("Super movie [3d].mp4", false, null, options);
            Test("Super movie [hsbs].mp4", true, "hsbs", options);
            Test("Super movie [fsbs].mp4", true, "fsbs", options);
            Test("Super movie [ftab].mp4", true, "ftab", options);
            Test("Super movie [htab].mp4", true, "htab", options);
            Test("Super movie [sbs3d].mp4", true, "sbs3d", options);
        }

        private void Test(string input, bool is3D, string format3D, NamingOptions options)
        {
            var parser = new Format3DParser(options);

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
