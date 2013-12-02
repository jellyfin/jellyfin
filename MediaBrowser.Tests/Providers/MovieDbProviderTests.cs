using MediaBrowser.Controller.Providers;
using MediaBrowser.Providers.Movies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MediaBrowser.Tests.Providers {
    [TestClass]
    public class MovieDbProviderTests {
        [TestMethod]
        public void TestNameMatches() {
            var name = string.Empty;
            int? year = null;
            NameParser.ParseName("My Movie (2013)", out name, out year);
            Assert.AreEqual("My Movie", name);
            Assert.AreEqual(2013, year);
            name = string.Empty;
            year = null;
            NameParser.ParseName("My Movie 2 (2013)", out name, out year);
            Assert.AreEqual("My Movie 2", name);
            Assert.AreEqual(2013, year);
            name = string.Empty;
            year = null;
            NameParser.ParseName("My Movie 2001 (2013)", out name, out year);
            Assert.AreEqual("My Movie 2001", name);
            Assert.AreEqual(2013, year);
            name = string.Empty;
            year = null;
            NameParser.ParseName("My Movie - 2 (2013)", out name, out year);
            Assert.AreEqual("My Movie - 2", name);
            Assert.AreEqual(2013, year);
            name = string.Empty;
            year = null;
            NameParser.ParseName("curse.of.chucky.2013.stv.unrated.multi.1080p.bluray.x264-rough", out name, out year);
            Assert.AreEqual("curse.of.chucky", name);
            Assert.AreEqual(2013, year);
        }
    }
}