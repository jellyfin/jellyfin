using System.IO;
using Emby.Naming.Common;
using Emby.Naming.TV;
using Xunit;

namespace Jellyfin.Naming.Tests.TV
{
    public class SimpleEpisodeTests
    {
        private readonly EpisodeResolver _resolver = new EpisodeResolver(new NamingOptions());

        [Theory]
        [InlineData("/server/anything_s01e02.mp4", "anything", 1, 2)]
        [InlineData("/server/anything_s1e2.mp4", "anything", 1, 2)]
        [InlineData("/server/anything_s01.e02.mp4", "anything", 1, 2)]
        [InlineData("/server/anything_102.mp4", "anything", 1, 2)]
        [InlineData("/server/anything_1x02.mp4", "anything", 1, 2)]
        [InlineData("/server/The Walking Dead 4x01.mp4", "The Walking Dead", 4, 1)]
        [InlineData("/server/the_simpsons-s02e01_18536.mp4", "the_simpsons", 2, 1)]
        [InlineData("/server/Temp/S01E02 foo.mp4", "", 1, 2)]
        [InlineData("Series/4x12 - The Woman.mp4", "", 4, 12)]
        [InlineData("Series/LA X, Pt. 1_s06e32.mp4", "LA X, Pt. 1", 6, 32)]
        [InlineData("[Baz-Bar]Foo - [1080p][Multiple Subtitle]/[Baz-Bar] Foo - 05 [1080p][Multiple Subtitle].mkv", "Foo", null, 5)]
        [InlineData("/Foo/The.Series.Name.S01E04.WEBRip.x264-Baz[Bar]/the.series.name.s01e04.webrip.x264-Baz[Bar].mkv", "The.Series.Name", 1, 4)]
        [InlineData("Love.Death.and.Robots.S01.1080p.NF.WEB-DL.DDP5.1.x264-NTG/Love.Death.and.Robots.S01E01.Sonnies.Edge.1080p.NF.WEB-DL.DDP5.1.x264-NTG.mkv", "Love.Death.and.Robots", 1, 1)]
        [InlineData("[YuiSubs] Tensura Nikki - Tensei Shitara Slime Datta Ken/[YuiSubs] Tensura Nikki - Tensei Shitara Slime Datta Ken - 12 (NVENC H.265 1080p).mkv", "Tensura Nikki - Tensei Shitara Slime Datta Ken", null, 12)]
        [InlineData("[Baz-Bar]Foo - 01 - 12[1080p][Multiple Subtitle]/[Baz-Bar] Foo - 05 [1080p][Multiple Subtitle].mkv", "Foo", null, 5)]
        [InlineData("Series/4-12 - The Woman.mp4", "", 4, 12, 12)]
        // TODO: [InlineData("E:\\Anime\\Yahari Ore no Seishun Love Comedy wa Machigatteiru\\Yahari Ore no Seishun Love Comedy wa Machigatteiru. Zoku\\Oregairu Zoku 11 - Hayama Hayato Always Renconds to Everyone's Expectations..mkv", "Yahari Ore no Seishun Love Comedy wa Machigatteiru", null, 11)]
        // TODO: [InlineData(@"/Library/Series/The Grand Tour (2016)/Season 1/S01E01 The Holy Trinity.mkv", "The Grand Tour", 1, 1)]
        public void TestSimple(string path, string seriesName, int? seasonNumber, int? episodeNumber, int? episodeEndNumber = null)
        {
            var result = _resolver.Resolve(path, false);

            Assert.NotNull(result);
            Assert.Equal(seasonNumber, result!.SeasonNumber);
            Assert.Equal(episodeNumber, result!.EpisodeNumber);
            Assert.Equal(seriesName, result!.SeriesName, true);
            Assert.Equal(path, result!.Path);
            Assert.Equal(Path.GetExtension(path).Substring(1), result?.Container);
            Assert.Null(result!.Format3D);
            Assert.False(result!.Is3D);
            Assert.False(result!.IsStub);
            Assert.Null(result!.StubType);
            Assert.Equal(episodeEndNumber, result!.EndingEpisodeNumber);
            Assert.False(result!.IsByDate);
        }
    }
}
