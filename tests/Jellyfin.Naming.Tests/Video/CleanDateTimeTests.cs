using System.IO;
using Emby.Naming.Common;
using Emby.Naming.Video;
using Xunit;

namespace Jellyfin.Naming.Tests.Video
{
    public sealed class CleanDateTimeTests
    {
        private readonly NamingOptions _namingOptions = new();

        [Theory]
        [InlineData("The Wolf of Wall Street (2013).mkv", "The Wolf of Wall Street", 2013)]
        [InlineData("The Wolf of Wall Street 2 (2013).mkv", "The Wolf of Wall Street 2", 2013)]
        [InlineData("The Wolf of Wall Street - 2 (2013).mkv", "The Wolf of Wall Street - 2", 2013)]
        [InlineData("The Wolf of Wall Street 2001 (2013).mkv", "The Wolf of Wall Street 2001", 2013)]
        [InlineData("300 (2006).mkv", "300", 2006)]
        [InlineData("d:/movies/300 (2006).mkv", "300", 2006)]
        [InlineData("300 2 (2006).mkv", "300 2", 2006)]
        [InlineData("300 - 2 (2006).mkv", "300 - 2", 2006)]
        [InlineData("300 2001 (2006).mkv", "300 2001", 2006)]
        [InlineData("curse.of.chucky.2013.stv.unrated.multi.1080p.bluray.x264-rough", "curse.of.chucky", 2013)]
        [InlineData("curse.of.chucky.2013.stv.unrated.multi.2160p.bluray.x264-rough", "curse.of.chucky", 2013)]
        [InlineData("/server/Movies/300 (2007)/300 (2006).bluray.disc", "300", 2006)]
        [InlineData("Arrival.2016.2160p.Blu-Ray.HEVC.mkv", "Arrival", 2016)]
        [InlineData("The Wolf of Wall Street (2013)", "The Wolf of Wall Street", 2013)]
        [InlineData("The Wolf of Wall Street 2 (2013)", "The Wolf of Wall Street 2", 2013)]
        [InlineData("The Wolf of Wall Street - 2 (2013)", "The Wolf of Wall Street - 2", 2013)]
        [InlineData("The Wolf of Wall Street 2001 (2013)", "The Wolf of Wall Street 2001", 2013)]
        [InlineData("300 (2006)", "300", 2006)]
        [InlineData("d:/movies/300 (2006)", "300", 2006)]
        [InlineData("300 2 (2006)", "300 2", 2006)]
        [InlineData("300 - 2 (2006)", "300 - 2", 2006)]
        [InlineData("300 2001 (2006)", "300 2001", 2006)]
        [InlineData("/server/Movies/300 (2007)/300 (2006)", "300", 2006)]
        [InlineData("/server/Movies/300 (2007)/300 (2006).mkv", "300", 2006)]
        [InlineData("American.Psycho.mkv", "American.Psycho.mkv", null)]
        [InlineData("American Psycho.mkv", "American Psycho.mkv", null)]
        [InlineData("[rec].mkv", "[rec].mkv", null)]
        [InlineData("St. Vincent (2014)", "St. Vincent", 2014)]
        [InlineData("Super movie(2009).mp4", "Super movie", 2009)]
        [InlineData("Drug War 2013.mp4", "Drug War", 2013)]
        [InlineData("My Movie (1997) - GreatestReleaseGroup 2019.mp4", "My Movie", 1997)]
        [InlineData("First Man 2018 1080p.mkv", "First Man", 2018)]
        [InlineData("First Man (2018) 1080p.mkv", "First Man", 2018)]
        [InlineData("Maximum Ride - 2016 - WEBDL-1080p - x264 AC3.mkv", "Maximum Ride", 2016)]
        [InlineData("Robin Hood [Multi-Subs] HighQuality [2018].mkv", "Robin Hood", 2018)]
        [InlineData("Robin Hood [Multi-Subs] [2018].mkv", "Robin Hood", 2018)]
        [InlineData("[2018] Robin Hood [Multi-Subs] BestUploadEver.mkv", "Robin Hood", 2018)]
        [InlineData("[2018] Robin Hood [Multi-Subs].mkv", "Robin Hood", 2018)]
        [InlineData("3.Days.to.Kill.2014.720p.BluRay.x264.YIFY.mkv", "3.Days.to.Kill", 2014)]
        [InlineData("3 days to kill (2005).mkv", "3 days to kill", 2005)]
        [InlineData("Rain Man 1988 REMASTERED 1080p BluRay x264 AAC - Ozlem.mp4", "Rain Man", 1988)]
        [InlineData("My Movie 2013.12.09", "My Movie 2013.12.09", null)]
        [InlineData("My Movie 2013-12-09", "My Movie 2013-12-09", null)]
        [InlineData("My Movie 20131209", "My Movie 20131209", null)]
        [InlineData("My Movie 2013-12-09 2013", "My Movie 2013-12-09", 2013)]
        [InlineData("Horrormovie (Uncut).mkv", "Horrormovie", null)]
        [InlineData("1917 - (2019)", "1917", 2019)]
        [InlineData("1883[2021]", "1883", 2021)]
        [InlineData("1883 2021", "1883", 2021)]
        [InlineData("2021 1883 1920x1110 BdRip.mkv", "1883", 2021)]
        [InlineData("Canigó.1883.2023.1080p.mkv", "Canigó.1883", 2023)]
        [InlineData("3000.Miles.to.Graceland-2001-2160p-h265", "3000.Miles.to.Graceland", 2001)]
        [InlineData("2001: A Space Odyssey (1968) 720p", "2001: A Space Odyssey", 1968)]
        [InlineData("IF.2024.1080p.WEBRip.1400MB.DD5.1.x264-GalaxyRG", "IF", 2024)]
        [InlineData("Deathly.Hallows.Part.2.2011.1080p.BluRay.x264", "Deathly.Hallows.Part.2", 2011)]
        [InlineData("[AnimeRG] Castle in the Sky (1986) Laputa Castle in the Sky [MULTI-AUDIO] [1080p][pseudo].mkv", "Castle in the Sky", 1986)]
        [InlineData("2014.The.Hobbit-.The.Battle.Of.The.Five.Armies.[Extended.Cut].1920x800.BDRip", "The.Hobbit-.The.Battle.Of.The.Five.Armies", 2014)]
        [InlineData("2015.Star.Wars.Episode.VII-.The.Force.Awakens.1920x800.BDRip", "Star.Wars.Episode.VII-.The.Force.Awakens", 2015)]
        [InlineData("2012.Brave.1920x802.BDRip.x264.TrueHD.mkv", "Brave", 2012)]
        [InlineData("2012.Brave.HD.1920x802.BDRip.x264.TrueHD.mkv", "Brave", 2012)]
        [InlineData("2012.Brave-UHD-BDRip.x264.TrueHD.mkv", "Brave", 2012)]
        [InlineData("2012.Brave-TrueHD.mkv", "Brave", 2012)]
        [InlineData("2012.Brave-DTS.mkv", "Brave", 2012)]
        [InlineData("2012.Brave-Master-Audio.mkv", "Brave", 2012)]
        [InlineData("10 Things I Hate About You - 1999", "10 Things I Hate About You", 1999)]
        [InlineData("Oldboy.REMASTERED.2003.1080p.BluRay.H264-VXT", "Oldboy", 2003)]
        [InlineData("Der Schuh des Manitu [Theatrical Cut] (2001) BDRip", "Der Schuh des Manitu", 2001)]
        [InlineData("The.Sorrow.And.The.Pity.1969.Part.2.REMASTERED.BDRip.x264-GHOULS", "The.Sorrow.And.The.Pity", 1969)]
        [InlineData("Sightseers 2012 LIMITED BDRip XviD-GECKOS", "Sightseers", 2012)]
        [InlineData("Sherlock.Holmes.2.A.Game.of.Shadows.MultiSubs", "Sherlock.Holmes.2.A.Game.of.Shadows", null)]
        [InlineData("10.000 BC AVI BDRip Dublado", "10.000 BC", null)]
        [InlineData("Piranha 3D.2010.XviD.UNRATED.BDRip.AbSurdity", "Piranha 3D", 2010)]
        [InlineData("Scar 3D [version 3D] [DVDSCREENER][Xvid][Spanish]", "Scar 3D", null)]
        [InlineData("STEP UP-Revolucion[3D-SBS]Spanish]", "STEP UP-Revolucion", null)]
        [InlineData("Spy Kids 3D.avi\"2003\"(3D-DVD-Rip)", "Spy Kids 3D", 2003)]
        [InlineData("2012-Wreck.it.Ralph.3D.MULTi.1080p.BluRay.x264", "Wreck.it.Ralph.3D", 2012)]
        [InlineData("Saw 3D *2010* [R5.XviD-miguel] [ENG]", "Saw 3D", 2010)]
        [InlineData("(2020) لى حر", "لى حر", 2020)]
        [InlineData("", "", null)]
        public void CleanDateTimeTest(string input, string? expectedName, int? expectedYear)
        {
            input = Path.GetFileName(input);

            var result = VideoResolver.CleanDateTime(input, _namingOptions);

            Assert.Equal(expectedName, result.Name, true);
            Assert.Equal(expectedYear, result.Year);
        }
    }
}
