using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    /// <summary>
    /// Regression tests for scene-release folder/file names found in the "everything in one folder"
    /// layout (movies and TV shows mixed in a single directory).
    /// All titles, groups and person names below are fictional; only the naming
    /// structure (SxxEyy, resolutions, codecs, leading numbers) is exercised.
    /// </summary>
    public class ReleaseNameParsingTests
    {
        private static readonly NamingOptions _namingOptions = new();

        [Theory]
        // Show episode files must parse as episodes.
        [InlineData("/media/Starlight.S02/Starlight.S02E01.1080p.NOVA.WEB-DL.DDP5.1.Atmos.DV.HDR.H265.HUN.ENG-Z9R.mkv", 2, 1)]
        [InlineData("/media/Bunker.S03/Bunker.S03E01.Who.Is.There.1080p.PULSAR.WEB-DL.DDP5.1.Atmos.H.264-showWEB.mkv", 3, 1)]
        [InlineData("/media/Outer.Colony.S01/Outer.Colony.S01E08.1080p.NOVA.WEB-DL.DDP5.1.H.264.HUN.ENG-QUASAR.mkv", 1, 8)]
        [InlineData("/media/Defenders.2019.S03/Defenders.2019.S03E02.1080p.MARKET.WEB-DL.DD+5.1.H.264-showWEB.mkv", 3, 2)]
        public void EpisodeResolver_ParsesRealEpisodeFiles(string path, int season, int episode)
        {
            var resolver = new EpisodeResolver(_namingOptions);
            var result = resolver.Resolve(path, false, true, false);

            Assert.NotNull(result);
            Assert.Equal(season, result.SeasonNumber);
            Assert.Equal(episode, result.EpisodeNumber);
        }

        [Theory]
        // Movie files must NOT parse as episodes.
        [InlineData("/media/30.Years.After.2026/quasar-30.years.after.2026.1080p.webrip.ma.mkv")]
        [InlineData("/media/Canadian Maniac 1999/Canadian Maniac 1999 1080p CEE Blu-Ray ReMuX AVC DTS-HD 5.1-HQZONE.mkv")]
        [InlineData("/media/3000.Women.And.Me/3000.Women.And.Me.The.Crimson.Tide.Story.2027.HUN.WEB-DL.1080p.H.264-HORDE.mkv")]
        [InlineData("/media/Le.Grand.Cabaret/Le.Grand.Cabaret.presente.40.20.Jean.Dupont.1080p.TVP.WEB-DL.AAC2.0.H.264.HUN.mkv")]
        public void EpisodeResolver_DoesNotParseMovieFiles(string path)
        {
            var resolver = new EpisodeResolver(_namingOptions);
            var result = resolver.Resolve(path, false, true, false);

            Assert.Null(result);
        }

        [Theory]
        // Movie folders must NOT be detected as season folders.
        // These use the same flags as SeriesResolver.IsSeriesFolder -> IsSeasonFolder.
        [InlineData("3000.Women.And.Me.The.Crimson.Tide.Story.2027.HUN.WEB-DL.1080p.H.264-HORDE")]
        [InlineData("30.Years.After.2026.1080p.MA.WEBRip.DDP5.1.Atmos.x264.HUN-PULSAR")]
        [InlineData("Canadian Maniac 1999 1080p CEE Blu-Ray ReMuX AVC DTS-HD 5.1-HQZONE")]
        [InlineData("Le.Grand.Cabaret.presente.40.20.Jean.Dupont.1080p.TVP.WEB-DL.AAC2.0.H.264.HUN")]
        public void SeasonPathParser_MovieFolderIsNotASeason(string folderName)
        {
            var path = "/media/" + folderName;
            var result = SeasonPathParser.Parse(path, "/media", false, false);

            Assert.False(result.Success, $"Expected '{folderName}' not to be a season but got season {result.SeasonNumber}");
        }

        [Theory]
        // Real season folders must still be detected.
        [InlineData("/media/Show/Season 1", "/media/Show", 1)]
        [InlineData("/media/Show/S01", "/media/Show", 1)]
        [InlineData("/media/Show/Season 2", "/media/Show", 2)]
        public void SeasonPathParser_RealSeasonFolderIsASeason(string path, string parentPath, int season)
        {
            var result = SeasonPathParser.Parse(path, parentPath, false, false);

            Assert.True(result.Success);
            Assert.Equal(season, result.SeasonNumber);
            Assert.True(result.IsSeasonFolder);
        }

        [Theory]
        // Series name extraction from real show folders.
        [InlineData("Starlight.S02.1080p.NOVA.WEB-DL.DDP5.1.Atmos.DV.HDR.H265.HUN.ENG-Z9R", "Starlight")]
        [InlineData("Outer.Colony.S01.1080p.NOVA.WEB-DL.DDP5.1.H.264.HUN.ENG-QUASAR", "Outer Colony")]
        [InlineData("Bunker.S03.1080p.PULSAR.WEB-DL.DDP5.1.Atmos.H.264-showWEB", "Bunker")]
        public void SeriesResolver_ExtractsNameFromReleaseFolder(string folderName, string expectedName)
        {
            var result = SeriesResolver.Resolve(_namingOptions, "/media/" + folderName);

            Assert.Equal(expectedName, result.Name);
        }
    }
}
