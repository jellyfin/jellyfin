using Emby.Naming.Audio;
using Emby.Naming.Common;
using Xunit;

namespace Jellyfin.Naming.Tests.Music
{
    public class MultiDiscAlbumTests
    {
        [Fact]
        public void TestMultiDiscAlbums()
        {
            Assert.False(IsMultiDiscAlbumFolder(@"blah blah"));
            Assert.False(IsMultiDiscAlbumFolder(@"d:/music\weezer/03 Pinkerton"));
            Assert.False(IsMultiDiscAlbumFolder(@"d:/music/michael jackson/Bad (2012 Remaster)"));

            Assert.True(IsMultiDiscAlbumFolder(@"cd1"));
            Assert.True(IsMultiDiscAlbumFolder(@"disc1"));
            Assert.True(IsMultiDiscAlbumFolder(@"disk1"));

            // Add a space
            Assert.True(IsMultiDiscAlbumFolder(@"cd 1"));
            Assert.True(IsMultiDiscAlbumFolder(@"disc 1"));
            Assert.True(IsMultiDiscAlbumFolder(@"disk 1"));

            Assert.True(IsMultiDiscAlbumFolder(@"cd  - 1"));
            Assert.True(IsMultiDiscAlbumFolder(@"disc- 1"));
            Assert.True(IsMultiDiscAlbumFolder(@"disk - 1"));

            Assert.True(IsMultiDiscAlbumFolder(@"Disc 01 (Hugo Wolf · 24 Lieder)"));
            Assert.True(IsMultiDiscAlbumFolder(@"Disc 04 (Encores and Folk Songs)"));
            Assert.True(IsMultiDiscAlbumFolder(@"Disc04 (Encores and Folk Songs)"));
            Assert.True(IsMultiDiscAlbumFolder(@"Disc 04(Encores and Folk Songs)"));
            Assert.True(IsMultiDiscAlbumFolder(@"Disc04(Encores and Folk Songs)"));

            Assert.True(IsMultiDiscAlbumFolder(@"D:/Video/MBTestLibrary/VideoTest/music/.38 special/anth/Disc 2"));
        }

        [Fact]
        public void TestMultiDiscAlbums1()
        {
            Assert.False(IsMultiDiscAlbumFolder(@"[1985] Oppurtunities (Let's make lots of money) (1985)"));
        }

        [Fact]
        public void TestMultiDiscAlbums2()
        {
            Assert.False(IsMultiDiscAlbumFolder(@"Blah 04(Encores and Folk Songs)"));
        }

        private bool IsMultiDiscAlbumFolder(string path)
        {
            var parser = new AlbumParser(new NamingOptions());

            return parser.ParseMultiPart(path).IsMultiPart;
        }
    }
}
