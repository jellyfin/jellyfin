using Emby.Naming.Audio;
using Emby.Naming.Common;
using Xunit;

namespace Jellyfin.Naming.Tests.Music
{
    public class MultiDiscAlbumTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        [Theory]
        [InlineData("", false)]
        [InlineData("C:/", false)]
        [InlineData("/home/", false)]
        [InlineData(@"blah blah", false)]
        [InlineData(@"D:/music/weezer/03 Pinkerton", false)]
        [InlineData(@"D:/music/michael jackson/Bad (2012 Remaster)", false)]
        [InlineData(@"cd1", true)]
        [InlineData(@"disc18", true)]
        [InlineData(@"disk10", true)]
        [InlineData(@"vol7", true)]
        [InlineData(@"volume1", true)]
        [InlineData(@"cd 1", true)]
        [InlineData(@"disc 1", true)]
        [InlineData(@"disk 1", true)]
        [InlineData(@"disk", false)]
        [InlineData(@"disk ·", false)]
        [InlineData(@"disk a", false)]
        [InlineData(@"disk volume", false)]
        [InlineData(@"disc disc", false)]
        [InlineData(@"disk disc 6", false)]
        [InlineData(@"cd  - 1", true)]
        [InlineData(@"disc- 1", true)]
        [InlineData(@"disk - 1", true)]
        [InlineData(@"Disc 01 (Hugo Wolf · 24 Lieder)", true)]
        [InlineData(@"Disc 04 (Encores and Folk Songs)", true)]
        [InlineData(@"Disc04 (Encores and Folk Songs)", true)]
        [InlineData(@"Disc 04(Encores and Folk Songs)", true)]
        [InlineData(@"Disc04(Encores and Folk Songs)", true)]
        [InlineData(@"D:/Video/MBTestLibrary/VideoTest/music/.38 special/anth/Disc 2", true)]
        [InlineData(@"[1985] Opportunities (Let's make lots of money) (1985)", false)]
        [InlineData(@"Blah 04(Encores and Folk Songs)", false)]
        public void AlbumParser_MultidiscPath_Identifies(string path, bool result)
        {
            var parser = new AlbumParser(_namingOptions);

            Assert.Equal(result, parser.IsMultiPart(path));
        }
    }
}
