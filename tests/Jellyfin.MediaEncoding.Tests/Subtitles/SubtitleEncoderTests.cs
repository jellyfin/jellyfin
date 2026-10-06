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
