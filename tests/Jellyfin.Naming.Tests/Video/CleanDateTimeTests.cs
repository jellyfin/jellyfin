using System.IO;
using Emby.Naming.Common;
using Emby.Naming.Video;
using Xunit;

namespace Jellyfin.Naming.Tests.Video
{
    public sealed class CleanDateTimeTests
    {
        private readonly NamingOptions _namingOptions = new NamingOptions();

        [Theory]
        [InlineData(@"The Wolf of Wall Street (2013).mkv", "The Wolf of Wall Street", 2013)]
        [InlineData(@"The Wolf of Wall Street 2 (2013).mkv", "The Wolf of Wall Street 2", 2013)]
        [InlineData(@"The Wolf of Wall Street - 2 (2013).mkv", "The Wolf of Wall Street - 2", 2013)]
        [InlineData(@"The Wolf of Wall Street 2001 (2013).mkv", "The Wolf of Wall Street 2001", 2013)]
        [InlineData(@"300 (2006).mkv", "300", 2006)]
        [InlineData(@"d:/movies/300 (2006).mkv", "300", 2006)]
        [InlineData(@"300 2 (2006).mkv", "300 2", 2006)]
        [InlineData(@"300 - 2 (2006).mkv", "300 - 2", 2006)]
        [InlineData(@"300 2001 (2006).mkv", "300 2001", 2006)]
        [InlineData(@"curse.of.chucky.2013.stv.unrated.multi.1080p.bluray.x264-rough", "curse.of.chucky", 2013)]
        [InlineData(@"curse.of.chucky.2013.stv.unrated.multi.2160p.bluray.x264-rough", "curse.of.chucky", 2013)]
        [InlineData(@"/server/Movies/300 (2007)/300 (2006).bluray.disc", "300", 2006)]
        [InlineData(@"Arrival.2016.2160p.Blu-Ray.HEVC.mkv", "Arrival", 2016)]
        [InlineData(@"The Wolf of Wall Street (2013)", "The Wolf of Wall Street", 2013)]
        [InlineData(@"The Wolf of Wall Street 2 (2013)", "The Wolf of Wall Street 2", 2013)]
        [InlineData(@"The Wolf of Wall Street - 2 (2013)", "The Wolf of Wall Street - 2", 2013)]
        [InlineData(@"The Wolf of Wall Street 2001 (2013)", "The Wolf of Wall Street 2001", 2013)]
        [InlineData(@"300 (2006)", "300", 2006)]
        [InlineData(@"d:/movies/300 (2006)", "300", 2006)]
        [InlineData(@"300 2 (2006)", "300 2", 2006)]
        [InlineData(@"300 - 2 (2006)", "300 - 2", 2006)]
        [InlineData(@"300 2001 (2006)", "300 2001", 2006)]
        [InlineData(@"/server/Movies/300 (2007)/300 (2006)", "300", 2006)]
        [InlineData(@"/server/Movies/300 (2007)/300 (2006).mkv", "300", 2006)]
        [InlineData(@"American.Psycho.mkv", "American.Psycho.mkv", null)]
        [InlineData(@"American Psycho.mkv", "American Psycho.mkv", null)]
        [InlineData(@"[rec].mkv", "[rec].mkv", null)]
        [InlineData(@"St. Vincent (2014)", "St. Vincent", 2014)]
        [InlineData("Super movie(2009).mp4", "Super movie", 2009)]
        [InlineData("Drug War 2013.mp4", "Drug War", 2013)]
        [InlineData("My Movie (1997) - GreatestReleaseGroup 2019.mp4", "My Movie", 1997)]
        [InlineData("First Man 2018 1080p.mkv", "First Man", 2018)]
        [InlineData("First Man (2018) 1080p.mkv", "First Man", 2018)]
        [InlineData("Maximum Ride - 2016 - WEBDL-1080p - x264 AC3.mkv", "Maximum Ride", 2016)]
        // FIXME: [InlineData("Robin Hood [Multi-Subs] [2018].mkv", "Robin Hood", 2018)]
        [InlineData(@"3.Days.to.Kill.2014.720p.BluRay.x264.YIFY.mkv", "3.Days.to.Kill", 2014)] // In this test case, running CleanDateTime first produces no date, so it will attempt to run CleanString first and then CleanDateTime again
        [InlineData("3 days to kill (2005).mkv", "3 days to kill", 2005)]
        public void CleanDateTimeTest(string input, string expectedName, int? expectedYear)
        {
            input = Path.GetFileName(input);

            var result = new VideoResolver(_namingOptions).CleanDateTime(input);

            Assert.Equal(expectedName, result.Name, true);
            Assert.Equal(expectedYear, result.Year);
        }
    }
}
