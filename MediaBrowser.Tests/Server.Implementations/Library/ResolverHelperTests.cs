using MediaBrowser.Server.Implementations.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MediaBrowser.Tests.Server.Implementations.Library {
    [TestClass]
    public class ResolverHelperTests
    {
        [TestMethod]
        public void TestStripBrackets()
        {
            Assert.AreEqual("My Movie", ResolverHelper.StripBrackets("My Movie [boxset] [blah blah]"));
            Assert.AreEqual("file 01", ResolverHelper.StripBrackets("[tag1] file 01 [tvdbid=12345]"));
            Assert.AreEqual("file 01", ResolverHelper.StripBrackets("[tag1] file 01 [tmdbid=12345]"));
            Assert.AreEqual("file 01", ResolverHelper.StripBrackets("[tag1] file [boxset] [tvdbid=12345] 01 [tmdbid=12345]"));
        }
    }
}