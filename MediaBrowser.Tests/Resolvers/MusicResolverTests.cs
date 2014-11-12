using MediaBrowser.Controller.Resolvers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MediaBrowser.Tests.Resolvers
{
    [TestClass]
    public class MusicResolverTests
    {
        [TestMethod]
        public void TestMultiDiscAlbums()
        {
            Assert.IsFalse(EntityResolutionHelper.IsMultiDiscAlbumFolder(@"blah blah"));
            Assert.IsFalse(EntityResolutionHelper.IsMultiDiscAlbumFolder(@"d:\\music\weezer\\03 Pinkerton"));
            Assert.IsFalse(EntityResolutionHelper.IsMultiDiscAlbumFolder(@"d:\\music\\michael jackson\\Bad (2012 Remaster)"));

            Assert.IsTrue(EntityResolutionHelper.IsMultiDiscAlbumFolder(@"cd1"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiDiscAlbumFolder(@"disc1"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiDiscAlbumFolder(@"disk1"));

            // Add a space
            Assert.IsTrue(EntityResolutionHelper.IsMultiDiscAlbumFolder(@"cd 1"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiDiscAlbumFolder(@"disc 1"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiDiscAlbumFolder(@"disk 1"));

            Assert.IsTrue(EntityResolutionHelper.IsMultiDiscAlbumFolder(@"cd  - 1"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiDiscAlbumFolder(@"disc- 1"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiDiscAlbumFolder(@"disk - 1"));

            Assert.IsTrue(EntityResolutionHelper.IsMultiDiscAlbumFolder(@"Disc 01 (Hugo Wolf · 24 Lieder)"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiDiscAlbumFolder(@"Disc 04 (Encores and Folk Songs)"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiDiscAlbumFolder(@"Disc04 (Encores and Folk Songs)"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiDiscAlbumFolder(@"Disc 04(Encores and Folk Songs)"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiDiscAlbumFolder(@"Disc04(Encores and Folk Songs)"));
        }
    }
}
