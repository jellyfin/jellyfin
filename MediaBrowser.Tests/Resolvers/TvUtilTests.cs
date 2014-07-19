using MediaBrowser.Controller.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MediaBrowser.Tests.Resolvers
{
    [TestClass]
    public class TvUtilTests
    {
        [TestMethod]
        public void TestGetEpisodeNumberFromFile()
        {
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"S02E03 blah.avi", true));

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
            Assert.AreEqual(9, TVUtils.GetEpisodeNumberFromFile(@"Season 25\The Simpsons.S25E09.Steal this episode.mp4", true));
            Assert.AreEqual(8, TVUtils.GetEpisodeNumberFromFile(@"The Simpsons\The Simpsons.S25E08.Steal this episode.mp4", false));
            Assert.AreEqual(136, TVUtils.GetEpisodeNumberFromFile(@"Season 2\[HorribleSubs] Hunter X Hunter - 136 [720p].mkv",true));

            //Four Digits seasons
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\2009x02 blah.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\S2009x02 blah.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\S2009E02 blah.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\S2009xE02 blah.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\seriesname 2009x02 blah.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\seriesname S2009x02 blah.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\seriesname S2009E02 blah.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\seriesname S2009xE02 blah.avi", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\Elementary - 2009x03 - 2009x04 - 2009x15 - Ep Name.ext", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\2009x03 - 2009x04 - 2009x15 - Ep Name.ext", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\2009x03-04-15 - Ep Name.ext", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\Elementary - 2009x03-04-15 - Ep Name.ext", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\2009x03-E15 - Ep Name.ext", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\Elementary - 2009x03-E15 - Ep Name.ext", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\2009x03 - x04 - x15 - Ep Name.ext", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\Elementary - 2009x03 - x04 - x15 - Ep Name.ext", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\2009x03x04x15 - Ep Name.ext", true));
            Assert.AreEqual(03, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\Elementary - 2009x03x04x15 - Ep Name.ext", true));
            Assert.AreEqual(23, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\Elementary - S2009E23-E24-E26 - The Woman.mp4", true));
            Assert.AreEqual(23, TVUtils.GetEpisodeNumberFromFile(@"Season 2009\S2009E23-E24-E26 - The Woman.mp4", true));

            //Without season number
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 1\02 - blah.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 2\02 - blah 14 blah.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 1\02 - blah-02 a.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"Season 2\02.avi", true));

            //Without seasons
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"The Simpsons\02.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"The Simpsons\02 - Ep Name.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"The Simpsons\02-Ep Name.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"The Simpsons\02.EpName.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"The Simpsons\The Simpsons - 02.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"The Simpsons\The Simpsons - 02 - Ep Name.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"The Simpsons\The Simpsons - 02 Ep Name.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"The Simpsons\The Simpsons 5 - 02 - Ep Name.avi", true));
            Assert.AreEqual(02, TVUtils.GetEpisodeNumberFromFile(@"The Simpsons\The Simpsons 5 - 02 Ep Name.avi", true));
        }

        [TestMethod]
        public void TestGetEndingEpisodeNumberFromFile()
        {
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\4x01 – 20 Hours in America (1).mkv"));
            
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\01x02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\S01x02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\S01E02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\S01xE02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\seriesname 01x02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\seriesname S01x02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\seriesname S01E02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\seriesname S01xE02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2\02x03 - 04 Ep Name.ext"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2\My show name 02x03 - 04 Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2\Elementary - 02x03 - 02x04 - 02x15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2\02x03 - 02x04 - 02x15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2\02x03-04-15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2\Elementary - 02x03-04-15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 02\02x03-E15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 02\Elementary - 02x03-E15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 02\02x03 - x04 - x15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 02\Elementary - 02x03 - x04 - x15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 02\02x03x04x15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 02\Elementary - 02x03x04x15 - Ep Name.ext"));
            Assert.AreEqual(26, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\Elementary - S01E23-E24-E26 - The Woman.mp4"));
            Assert.AreEqual(26, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\S01E23-E24-E26 - The Woman.mp4"));


            //Four Digits seasons
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\2009x02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\S2009x02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\S2009E02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\S2009xE02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\seriesname 2009x02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\seriesname S2009x02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\seriesname S2009E02 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\seriesname S2009xE02 blah.avi"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\Elementary - 2009x03 - 2009x04 - 2009x15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\2009x03 - 2009x04 - 2009x15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\2009x03-04-15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\Elementary - 2009x03-04-15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\2009x03-E15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\Elementary - 2009x03-E15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\2009x03 - x04 - x15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\Elementary - 2009x03 - x04 - x15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\2009x03x04x15 - Ep Name.ext"));
            Assert.AreEqual(15, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\Elementary - 2009x03x04x15 - Ep Name.ext"));
            Assert.AreEqual(26, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\Elementary - S2009E23-E24-E26 - The Woman.mp4"));
            Assert.AreEqual(26, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2009\S2009E23-E24-E26 - The Woman.mp4"));

            //Without season number
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\02 - blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2\02 - blah 14 blah.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\02 - blah-02 a.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2\02.avi"));

            Assert.AreEqual(3, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\02-03 - blah.avi"));
            Assert.AreEqual(4, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2\02-04 - blah 14 blah.avi"));
            Assert.AreEqual(5, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 1\02-05 - blah-02 a.avi"));
            Assert.AreEqual(4, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2\02-04.avi"));
            Assert.AreEqual(null, TVUtils.GetEndingEpisodeNumberFromFile(@"Season 2\[HorribleSubs] Hunter X Hunter - 136 [720p].mkv"));

        }

        [TestMethod]
        public void TestGetSeasonNumberFromPath() {

            Assert.AreEqual(02, TVUtils.GetSeasonNumberFromEpisodeFile(@"\Show\Season 02\S02E03 blah.avi"));
            
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 1"));
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 1"));
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 1"));
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 1"));
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 1"));
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 1"));
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 1"));
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 1"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 02"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 02"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 02"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 02"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 02"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 02"));
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 1"));
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 1"));

            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromPath(@"\Drive\Seinfeld\2"));
            
            //Four Digits seasons
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromPath(@"\Drive\Season 2009"));
        }

        [TestMethod]
        public void TestGetSeasonNumberFromEpisodeFile()
        {
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 1\01x02 blah.avi"));
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 1\S01x02 blah.avi"));
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 1\S01E02 blah.avi"));
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 1\S01xE02 blah.avi"));
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 1\seriesname 01x02 blah.avi"));
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 1\seriesname S01x02 blah.avi"));
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 1\seriesname S01E02 blah.avi"));
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 1\seriesname S01xE02 blah.avi"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2\Elementary - 02x03 - 02x04 - 02x15 - Ep Name.ext"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2\02x03 - 02x04 - 02x15 - Ep Name.ext"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2\02x03-04-15 - Ep Name.ext"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2\Elementary - 02x03-04-15 - Ep Name.ext"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 02\02x03-E15 - Ep Name.ext"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 02\Elementary - 02x03-E15 - Ep Name.ext"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 02\02x03 - x04 - x15 - Ep Name.ext"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 02\Elementary - 02x03 - x04 - x15 - Ep Name.ext"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 02\02x03x04x15 - Ep Name.ext"));
            Assert.AreEqual(2, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 02\Elementary - 02x03x04x15 - Ep Name.ext"));
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 1\Elementary - S01E23-E24-E26 - The Woman.mp4"));
            Assert.AreEqual(1, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 1\S01E23-E24-E26 - The Woman.mp4"));

            //Four Digits seasons
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\2009x02 blah.avi"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\S2009x02 blah.avi"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\S2009E02 blah.avi"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\S2009xE02 blah.avi"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\seriesname 2009x02 blah.avi"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\seriesname S2009x02 blah.avi"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\seriesname S2009E02 blah.avi"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\seriesname S2009xE02 blah.avi"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\Elementary - 2009x03 - 2009x04 - 2009x15 - Ep Name.ext"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\2009x03 - 2009x04 - 2009x15 - Ep Name.ext"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\2009x03-04-15 - Ep Name.ext"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\Elementary - 2009x03-04-15 - Ep Name.ext"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\2009x03-E15 - Ep Name.ext"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\Elementary - 2009x03-E15 - Ep Name.ext"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\2009x03 - x04 - x15 - Ep Name.ext"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\Elementary - 2009x03 - x04 - x15 - Ep Name.ext"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\2009x03x04x15 - Ep Name.ext"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\Elementary - 2009x03x04x15 - Ep Name.ext"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\Elementary - S2009E23-E24-E26 - The Woman.mp4"));
            Assert.AreEqual(2009, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 2009\S2009E23-E24-E26 - The Woman.mp4"));
            Assert.AreEqual(25, TVUtils.GetSeasonNumberFromEpisodeFile(@"Season 25\The Simpsons.S25E09.Steal this episode.mp4"));
            Assert.AreEqual(25, TVUtils.GetSeasonNumberFromEpisodeFile(@"The Simpsons\The Simpsons.S25E09.Steal this episode.mp4"));
        }
    }
}
