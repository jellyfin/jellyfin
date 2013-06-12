using MediaBrowser.Controller.Resolvers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MediaBrowser.Tests.Resolvers
{
    [TestClass]
    public class MovieResolverTests
    {
        [TestMethod]
        public void TestMultiPartFiles()
        {
            Assert.IsFalse(EntityResolutionHelper.IsMultiPartFile(@"blah blah.mkv"));

            Assert.IsTrue(EntityResolutionHelper.IsMultiPartFile(@"blah blah - cd1.mkv"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiPartFile(@"blah blah - disc1.mkv"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiPartFile(@"blah blah - disk1.mkv"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiPartFile(@"blah blah - pt1.mkv"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiPartFile(@"blah blah - part1.mkv"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiPartFile(@"blah blah - dvd1.mkv"));

            // Add a space
            Assert.IsTrue(EntityResolutionHelper.IsMultiPartFile(@"blah blah - cd 1.mkv"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiPartFile(@"blah blah - disc 1.mkv"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiPartFile(@"blah blah - disk 1.mkv"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiPartFile(@"blah blah - pt 1.mkv"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiPartFile(@"blah blah - part 1.mkv"));
            Assert.IsTrue(EntityResolutionHelper.IsMultiPartFile(@"blah blah - dvd 1.mkv"));
        }
    }
}
