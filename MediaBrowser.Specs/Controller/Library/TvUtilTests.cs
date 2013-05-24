using MediaBrowser.Controller.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MediaBrowser.Specs.Controller.Library
{
    [TestClass]
    public class TvUtilTests
    {
        [TestMethod]
        public void TestGetEpisodeNumberFromFile()
        {
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 1\01x02 blah.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 1\S01x02 blah.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 1\S01E02 blah.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 1\S01xE02 blah.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 1\seriesname 01x02 blah.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 1\seriesname S01x02 blah.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 1\seriesname S01E02 blah.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 1\seriesname S01xE02 blah.avi", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 2\Elementary - 02x03 - 02x04 - 02x15 - Ep Name.ext", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 2\02x03 - 02x04 - 02x15 - Ep Name.ext", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 2\02x03-04-15 - Ep Name.ext", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 2\Elementary - 02x03-04-15 - Ep Name.ext", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 02\02x03-E15 - Ep Name.ext", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 02\Elementary - 02x03-E15 - Ep Name.ext", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 02\02x03 - x04 - x15 - Ep Name.ext", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 02\Elementary - 02x03 - x04 - x15 - Ep Name.ext", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 02\02x03x04x15 - Ep Name.ext", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 02\Elementary - 02x03x04x15 - Ep Name.ext", true));
            Assert.AreEqual(23, TVUtils.GetEpisodeNumberFromFile(@"Season 1\Elementary - S01E23-E24-E26 - The Woman.mp4", true));
            Assert.AreEqual(23, TVUtils.GetEpisodeNumberFromFile(@"Season 1\S01E23-E24-E26 - The Woman.mp4", true));
        }

        [TestMethod]
        public void TestGetEndingEpisodeNumberFromFile()
        {
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\01x02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\S01x02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\S01E02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\S01xE02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\seriesname 01x02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\seriesname S01x02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\seriesname S01E02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\seriesname S01xE02 blah.avi"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2\Elementary - 02x03 - 02x04 - 02x15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2\02x03 - 02x04 - 02x15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 02\02x03-E15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 02\Elementary - 02x03-E15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2\02x03-04-15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2\Elementary - 02x03-04-15 - Ep Name.ext"));
            Assert.AreEqual(26, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\Elementary - S01E23-E24-E26 - The Woman.mp4"));
            Assert.AreEqual(26, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\S01E23-E24-E26 - The Woman.mp4"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 02\02x03x04x15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 02\02x03 - x04 - x15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 02\Elementary - 02x03 - x04 - x15 - Ep Name.ext"));
        }
    }
}
