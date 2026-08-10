using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using MediaBrowser.MediaEncoding.Subtitles;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.MediaEncoding.Subtitles.Tests
{
    public class SubtitleEncoderTests
    {
        private const int StreamCount = 8;
        private const int CueCount = 500;

        // A Greek line that requires a non-UTF-8 legacy encoding to reproduce the bug. The accented
        // characters (ά, έ, ή, ί, ό, ύ, ώ) share the same code points in windows-1253 and iso-8859-7,
        // so a Greek-vs-Greek charset misdetection still round-trips correctly.
        private const string GreekText = "Καλημέρα κόσμε, αυτό είναι ένας υπότιτλος.";

        static SubtitleEncoderTests()
        {
            // Mirrors Jellyfin.Server startup so legacy code pages (e.g. Greek windows-1253) are available.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        // Enough Greek text to give the charset detector a strong, unambiguous signal.
        private static string BuildGreekSrt()
        {
            var builder = new StringBuilder();
            for (var i = 1; i <= 8; i++)
            {
                builder.Append(i.ToString(CultureInfo.InvariantCulture)).Append('\n');
                builder.Append("00:00:0").Append(i.ToString(CultureInfo.InvariantCulture))
                    .Append(",000 --> 00:00:0").Append((i + 1).ToString(CultureInfo.InvariantCulture)).Append(",000\n");
                builder.Append(GreekText).Append('\n');
                builder.Append("Η γρήγορη καφέ αλεπού πηδάει πάνω από το τεμπέλικο σκυλί.\n\n");
            }

            return builder.ToString();
        }

        public static TheoryData<MediaSourceInfo, MediaStream, SubtitleEncoder.SubtitleInfo> GetReadableFile_Valid_TestData()
        {
            var data = new TheoryData<MediaSourceInfo, MediaStream, SubtitleEncoder.SubtitleInfo>
            {
                {
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
                    }
                },
                {
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
                    }
                },
                {
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
                    }
                },
                {
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
                    }
                }
            };

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

        public static TheoryData<Encoding> GetSubtitleStream_NonUtf8LocalFile_TestData()
        {
            return
            [
                // Greek legacy encodings – the exact scenario reported in issue #17267.
                Encoding.GetEncoding("windows-1253"),
                Encoding.GetEncoding("iso-8859-7"),
                // Wide encoding with a BOM.
                new UnicodeEncoding(bigEndian: false, byteOrderMark: true),
            ];
        }

        [Theory]
        [MemberData(nameof(GetSubtitleStream_NonUtf8LocalFile_TestData))]
        public async Task GetSubtitleStream_NonUtf8LocalFile_ConvertedToUtf8(Encoding sourceEncoding)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var srt = BuildGreekSrt();
            var path = Path.GetTempFileName();
            try
            {
                await File.WriteAllTextAsync(path, srt, sourceEncoding, cancellationToken);

                var fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
                var subtitleEncoder = fixture.Create<SubtitleEncoder>();

                var fileInfo = new SubtitleEncoder.SubtitleInfo
                {
                    Path = path,
                    Protocol = MediaProtocol.File,
                    Format = "srt",
                    IsExternal = true
                };

                using var stream = await subtitleEncoder.GetSubtitleStream(fileInfo, cancellationToken);
                using var reader = new StreamReader(stream, new UTF8Encoding(false));
                var text = await reader.ReadToEndAsync(cancellationToken);

                // The Greek text must survive round-trip and contain no replacement characters.
                Assert.Contains(GreekText, text, StringComparison.Ordinal);
                Assert.DoesNotContain('�', text);
                Assert.DoesNotContain('?', text);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void ConvertSubtitles_SequentialCalls_AreDeterministic()
        {
            using var encoder = CreateEncoder();
            var sources = GenerateSources();

            var first = ConvertAllSequential(encoder, sources);
            var second = ConvertAllSequential(encoder, sources);

            for (var i = 0; i < StreamCount; i++)
            {
                Assert.Contains($"S{i}C{CueCount - 1}", first[i], StringComparison.Ordinal);
                Assert.Equal(first[i], second[i]);
            }
        }

        [Fact]
        public async Task GetSubtitleStream_Utf8LocalFile_PreservesContent()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var srt = BuildGreekSrt();
            var path = Path.GetTempFileName();
            try
            {
                await File.WriteAllTextAsync(path, srt, new UTF8Encoding(false), cancellationToken);

                var fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
                var subtitleEncoder = fixture.Create<SubtitleEncoder>();

                var fileInfo = new SubtitleEncoder.SubtitleInfo
                {
                    Path = path,
                    Protocol = MediaProtocol.File,
                    Format = "srt",
                    IsExternal = true
                };

                using var stream = await subtitleEncoder.GetSubtitleStream(fileInfo, cancellationToken);

                // An already-UTF-8 file must be short-circuited and served directly from disk,
                // not read into memory and re-encoded (which would produce a MemoryStream).
                Assert.IsNotType<MemoryStream>(stream);

                using var reader = new StreamReader(stream, new UTF8Encoding(false));
                var text = await reader.ReadToEndAsync(cancellationToken);

                Assert.Contains(GreekText, text, StringComparison.Ordinal);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task ConvertSubtitles_ConcurrentCalls_MatchSequentialBaseline()
        {
            const int Iterations = 10;

            using var encoder = CreateEncoder();
            var sources = GenerateSources();
            var baseline = ConvertAllSequential(encoder, sources);

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                var results = await Task.WhenAll(Enumerable.Range(0, StreamCount)
                    .Select(i => Task.Run(() => Convert(encoder, sources[i], i)))
                    .ToArray());

                for (var i = 0; i < StreamCount; i++)
                {
                    Assert.True(
                        string.Equals(baseline[i], results[i], StringComparison.Ordinal),
                        $"Iteration {iteration}: stream {i} returned corrupted content ({results[i].Length} chars vs {baseline[i].Length} baseline)");
                }
            }
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

            // Fontsize is derived from the frame height rather than from the track's own
            // byte (41), the way VLC's FontSizeConvert does: 1080 * 0.0605 = 65. MarginV
            // (~2.31% = 25) and Outline (~6% of Fontsize = 4) follow. This track authors no background box
            // (ffmpeg emits it as &Hff...... = fully transparent), so the invisible
            // OutlineColour/BackColour are substituted with opaque black; BorderStyle
            // stays 1 (outline + shadow).
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

        public static TheoryData<string, int?, int?, bool> ShouldExtractMovTextAsAss_TestData()
        {
            return new TheoryData<string, int?, int?, bool>
            {
                { "mov_text", 1920, 1080, true },
                // Without real dimensions the decoder would fall back to 384x288 and the
                // style could not be normalized, so ASS would be worse than SubRip here.
                { "mov_text", null, null, false },
                { "mov_text", 0, 0, false },
                { "mov_text", 1920, null, false },
                { "subrip", 1920, 1080, false },
                { "dvbsub", 1920, 1080, false }
            };
        }

        [Theory]
        [MemberData(nameof(ShouldExtractMovTextAsAss_TestData))]
        public void ShouldExtractMovTextAsAss_OnlyForMovTextWithKnownVideoSize(string codec, int? width, int? height, bool expected)
        {
            var mediaSource = new MediaSourceInfo
            {
                MediaStreams = width is null && height is null
                    ? []
                    : [new MediaStream { Type = MediaStreamType.Video, Width = width, Height = height }]
            };

            var subtitleStream = new MediaStream { Type = MediaStreamType.Subtitle, Codec = codec };

            Assert.Equal(expected, SubtitleEncoder.ShouldExtractMovTextAsAss(subtitleStream, mediaSource));
        }

        [Fact]
        public void NormalizeMovTextAss_CrlfLineEndings_StyleRewrittenAndLineEndingsPreserved()
        {
            // ff_ass_subtitle_header_full builds the header with CRLF and the current ASS
            // muxer normalizes it back to LF on the way out, so today's extracted files are
            // LF. This pins down the other case anyway: the Style regex is anchored with
            // RegexOptions.Multiline, whose '$' matches before the LF, so a CR would land
            // inside the match and has to survive the field split and rejoin intact.
            const string Input =
                "[V4+ Styles]\r\n" +
                "Style: Default,Arial,41,&Hffffff,&Hffffff,&Hff000000,&Hff000000,0,0,0,0,100,100,0,0,1,1,0,1,10,10,10,1\r\n" +
                "\r\n" +
                "[Events]\r\n" +
                "Dialogue: 0,0:00:00.00,0:00:05.00,Default,,0,0,0,,Line one.\r\n";

            var result = SubtitleEncoder.NormalizeMovTextAss(Input, 1080);

            Assert.Contains(
                "Style: Default,Arial,65,&Hffffff,&Hffffff,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,4,0,2,10,10,25,1\r\n",
                result,
                StringComparison.Ordinal);
            Assert.DoesNotContain("1\r,", result, StringComparison.Ordinal);
        }

        [Theory]
        // Opaque black, which is how ffmpeg's own mov_text muxer authors the track
        // (back_alpha 255 -> (255 - 255) << 24 = 0). Verified against a real remux.
        [InlineData("&H0")]
        // A coloured background at 25% transparency.
        [InlineData("&H40403020")]
        public void NormalizeMovTextAss_TrackAuthorsABorderColour_ColourAndBorderStylePreserved(string colour)
        {
            var input = "Style: Default,Arial,41,&Hffffff,&Hffffff," + colour + "," + colour
                + ",0,0,0,0,100,100,0,0,1,1,0,2,10,10,10,1";

            var result = SubtitleEncoder.NormalizeMovTextAss(input, 1080);

            // Nothing was invisible here, so the authored colour survives untouched and
            // BorderStyle stays on ffmpeg's outline (1). Only the size-derived fields move.
            Assert.Contains(
                "Style: Default,Arial,65,&Hffffff,&Hffffff," + colour + "," + colour
                    + ",0,0,0,0,100,100,0,0,1,4,0,2,10,10,25,1",
                result,
                StringComparison.Ordinal);
        }

        [Fact]
        public void NormalizeMovTextAss_UnparseableOutlineColour_LeavesColoursAndBorderStyleAlone()
        {
            const string Input = "Style: Default,Arial,41,&Hffffff,&Hffffff,not-a-colour,&Hff000000,0,0,0,0,100,100,0,0,1,1,0,2,10,10,10,1";

            var result = SubtitleEncoder.NormalizeMovTextAss(Input, 1080);

            // Font size, outline width and margin are still normalized, but the colour
            // fields are not guessed at.
            Assert.Contains(
                "Style: Default,Arial,65,&Hffffff,&Hffffff,not-a-colour,&Hff000000,0,0,0,0,100,100,0,0,1,4,0,2,10,10,25,1",
                result,
                StringComparison.Ordinal);
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

        private static SubtitleEncoder CreateEncoder()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
            fixture.Inject<ISubtitleParser>(new SubtitleEditParser(NullLogger<SubtitleEditParser>.Instance));
            return fixture.Create<SubtitleEncoder>();
        }

        private static byte[][] GenerateSources()
        {
            return Enumerable.Range(0, StreamCount)
                .Select(i => Encoding.UTF8.GetBytes(GenerateSrt(i, CueCount)))
                .ToArray();
        }

        private static string Convert(SubtitleEncoder encoder, byte[] source, int streamIndex)
        {
            using var input = new MemoryStream(source);
            var info = new SubtitleEncoder.SubtitleInfo { Path = $"track{streamIndex}.srt", Format = "srt" };
            using var output = encoder.ConvertSubtitles(input, info, "vtt", 0, 0, false);
            return Encoding.UTF8.GetString(output.ToArray());
        }

        private static string[] ConvertAllSequential(SubtitleEncoder encoder, byte[][] sources)
        {
            return sources.Select((source, i) => Convert(encoder, source, i)).ToArray();
        }

        private static string GenerateSrt(int streamIndex, int cueCount)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < cueCount; i++)
            {
                var start = TimeSpan.FromSeconds(i * 4);
                var end = start + TimeSpan.FromSeconds(2);
                builder.Append(i + 1).AppendLine()
                    .Append(start.ToString(@"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture))
                    .Append(" --> ")
                    .AppendLine(end.ToString(@"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture))
                    .Append('S').Append(streamIndex).Append('C').Append(i).AppendLine()
                    .AppendLine();
            }

            return builder.ToString();
        }
    }
}
