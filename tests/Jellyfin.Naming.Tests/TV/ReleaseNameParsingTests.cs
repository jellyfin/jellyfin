using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    /// <summary>
    /// Regression tests for series name extraction from release folder names.
    /// All titles, groups and person names below are fictional.
    /// </summary>
    public class ReleaseNameParsingTests
    {
        private static readonly NamingOptions _namingOptions = new();

        [Theory]
        // Optimistic episode expressions must not produce series names.
        [InlineData("Bunker.S03.1080p.PULSAR.WEB-DL.DDP5.1.Atmos.H.264-showWEB", "Bunker")]
        [InlineData("Outer.Colony.S01.1080p.NOVA.WEB-DL.DDP5.1.H.264.HUN.ENG-QUASAR", "Outer Colony")]
        public void SeriesResolver_ExtractsNameFromReleaseFolder(string folderName, string expectedName)
        {
            var result = SeriesResolver.Resolve(_namingOptions, "/media/" + folderName);

            Assert.Equal(expectedName, result.Name);
        }

        [Theory]
        // Resolution patterns (1280x720 etc.) must not parse as season/episode.
        [InlineData("/media/Jujutsu Kaisen (BD_1280x720)")]
        [InlineData("/media/Show.1920x1080.BluRay")]
        public void SeriesPathParser_ResolutionPatternIsNotASeries(string path)
        {
            var result = SeriesPathParser.Parse(_namingOptions, path);

            Assert.False(result.Success);
        }
    }
}
