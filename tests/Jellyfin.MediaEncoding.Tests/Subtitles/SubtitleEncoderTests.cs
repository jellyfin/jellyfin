using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using MediaBrowser.MediaEncoding.Subtitles;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Xunit;

namespace Jellyfin.MediaEncoding.Subtitles.Tests
{
    public class SubtitleEncoderTests
    {
        public static TheoryData<MediaSourceInfo, MediaStream, SubtitleEncoder.SubtitleInfo> GetReadableFile_Valid_TestData()
        {
            var data = new TheoryData<MediaSourceInfo, MediaStream, SubtitleEncoder.SubtitleInfo>();

            data.Add(
                new MediaSourceInfo()
                {
                    Protocol = MediaProtocol.File
                },
                new MediaStream()
                {
                    Path = "/media/sub.ass",
                    IsExternal = true
                },
                new SubtitleEncoder.SubtitleInfo()
                {
                    Path = "/media/sub.ass",
                    Protocol = MediaProtocol.File,
                    Format = "ass",
                    IsExternal = true
                });

            data.Add(
                new MediaSourceInfo()
                {
                    Protocol = MediaProtocol.File
                },
                new MediaStream()
                {
                    Path = "/media/sub.ssa",
                    IsExternal = true
                },
                new SubtitleEncoder.SubtitleInfo()
                {
                    Path = "/media/sub.ssa",
                    Protocol = MediaProtocol.File,
                    Format = "ssa",
                    IsExternal = true
                });

            data.Add(
                new MediaSourceInfo()
                {
                    Protocol = MediaProtocol.File
                },
                new MediaStream()
                {
                    Path = "/media/sub.srt",
                    IsExternal = true
                },
                new SubtitleEncoder.SubtitleInfo()
                {
                    Path = "/media/sub.srt",
                    Protocol = MediaProtocol.File,
                    Format = "srt",
                    IsExternal = true
                });

            data.Add(
                new MediaSourceInfo()
                {
                    Protocol = MediaProtocol.Http
                },
                new MediaStream()
                {
                    Path = "/media/sub.ass",
                    IsExternal = true
                },
                new SubtitleEncoder.SubtitleInfo()
                {
                    Path = "/media/sub.ass",
                    Protocol = MediaProtocol.File,
                    Format = "ass",
                    IsExternal = true
                });

            return data;
        }

        [Theory]
        [MemberData(nameof(GetReadableFile_Valid_TestData))]
        public async Task GetReadableFile_Valid_Success(MediaSourceInfo mediaSource, MediaStream subtitleStream, SubtitleEncoder.SubtitleInfo subtitleInfo)
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
            var subtitleEncoder = fixture.Create<SubtitleEncoder>();
            var result = await subtitleEncoder.GetReadableFile(mediaSource, subtitleStream, CancellationToken.None);
            Assert.Equal(subtitleInfo.Path, result.Path);
            Assert.Equal(subtitleInfo.Protocol, result.Protocol);
            Assert.Equal(subtitleInfo.Format, result.Format);
            Assert.Equal(subtitleInfo.IsExternal, result.IsExternal);
        }

        [Fact]
        public void NormalizeMovTextAss_RealMovTextStyleLine_MatchesVlcFontSizeConvention()
        {
            // Actual header ffmpeg produces for a tx3g track authored with left justification
            // (h_align=0 per 3GPP TS 26.245), reproduced from a real anime mp4 sample
            // (1440x1080 video, tx3g Fontsize byte = 41).
            const string Input =
                "[Script Info]\n" +
                "ScriptType: v4.00+\n" +
                "PlayResX: 1440\n" +
                "PlayResY: 1080\n" +
                "\n" +
                "[V4+ Styles]\n" +
                "Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding\n" +
                "Style: Default,Arial,41,&Hffffff,&Hffffff,&Hff000000,&Hff000000,0,0,0,0,100,100,0,0,1,1,0,1,10,10,10,1\n" +
                "\n" +
                "[Events]\n" +
                "Dialogue: 0,0:01:46.65,0:01:49.78,Default,,0,0,0,,Cela commence à bien faire,\\Ninspecteur Megure.\n";

            var result = SubtitleEncoder.NormalizeMovTextAss(Input, 1080);

            // Fontsize ~= 0.0605 * PlayResY (halved from an initial 0.121 VLC-matched
            // calibration that rendered ~2x too large against the actual deployment font).
            // 1080 * 0.0605 = 65. MarginV (~2.31% = 25) and Outline (~6% of Fontsize = 4)
            // scale independently of that halving. OutlineColour/BackColour are forced to
            // fully opaque (&H00000000) since ffmpeg writes them with a transparent alpha
            // byte (&Hff......), making the outline/shadow invisible regardless of width.
            Assert.Contains(
                "Style: Default,Arial,65,&Hffffff,&Hffffff,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,4,0,2,10,10,25,1",
                result,
                StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("7")] // top-left
        [InlineData("4")] // middle-left
        [InlineData("1")] // bottom-left
        public void NormalizeMovTextAss_AnyLeftColumnStyleAlignment_MapsToCenterColumn(string leftAlignment)
        {
            var centerAlignment = leftAlignment switch
            {
                "7" => "8",
                "4" => "5",
                _ => "2"
            };

            var input = "Style: Default,Arial,41,&Hffffff,&Hffffff,&Hff000000,&Hff000000,0,0,0,0,100,100,0,0,1,1,0,"
                + leftAlignment + ",10,10,10,1";

            var result = SubtitleEncoder.NormalizeMovTextAss(input, 1080);

            Assert.EndsWith(centerAlignment + ",10,10,25,1", result, StringComparison.Ordinal);
        }

        [Fact]
        public void NormalizeMovTextAss_PerLineAlignmentOverrideTag_RemapsToCenter()
        {
            const string Input = "Dialogue: 0,0:00:00.00,0:00:05.00,Default,,0,0,0,,{\\an1}Left-aligned override.";

            var result = SubtitleEncoder.NormalizeMovTextAss(Input, 1080);

            Assert.Contains("{\\an2}", result, StringComparison.Ordinal);
        }

        [Fact]
        public void NormalizeMovTextAss_AlreadyCenteredOrRightAligned_AlignmentIsUnchanged()
        {
            const string Input = "Style: Default,Arial,41,&Hffffff,&Hffffff,&Hff000000,&Hff000000,0,0,0,0,100,100,0,0,1,1,0,3,10,10,10,1";

            var result = SubtitleEncoder.NormalizeMovTextAss(Input, 1080);

            // Alignment (right, "3") is untouched -- only the left column is remapped --
            // but font size, outline and margin are still normalized to the video's real height.
            Assert.EndsWith(",1,4,0,3,10,10,25,1", result, StringComparison.Ordinal);
        }

        [Fact]
        public void NormalizeMovTextAss_PerLineFontSizeOverride_RescaledProportionally()
        {
            // A segment emphasized at roughly double the default style's font size
            // (e.g. a STYL box override) should stay roughly twice the new base size too.
            const string Input =
                "Style: Default,Arial,41,&Hffffff,&Hffffff,&Hff000000,&Hff000000,0,0,0,0,100,100,0,0,1,1,0,2,10,10,10,1\n"
                + "Dialogue: 0,0:00:00.00,0:00:05.00,Default,,0,0,0,,{\\fs82}Emphasized text.";

            var result = SubtitleEncoder.NormalizeMovTextAss(Input, 1080);

            // original override (82) was 2x the original default (41); new default is 65,
            // so the rescaled override should be 2x that too (130).
            Assert.Contains("{\\fs130}", result, StringComparison.Ordinal);
        }
    }
}
