using System.Diagnostics;
using Jellyfin.Api.Helpers;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    /// <summary>
    /// How the bytes an ffmpeg process read are found and shown.
    /// </summary>
    public class TranscodeTestProcessIoTests
    {
        [Fact]
        public void ParseBytesRead_UsesRcharNotReadBytes()
        {
            const string Text = "rchar: 1020304050\nwchar: 12\nsyscr: 300\nsyscw: 4\nread_bytes: 4096\nwrite_bytes: 0\ncancelled_write_bytes: 0\n";
            Assert.Equal(1_020_304_050, TranscodeTestProcessIo.ParseBytesRead(Text));
        }

        [Theory]
        [InlineData("")]
        [InlineData("wchar: 12\nread_bytes: 4096\n")]
        [InlineData("rchar: lots\n")]
        public void ParseBytesRead_WithoutAnRcharNumber_IsNull(string text)
            => Assert.Null(TranscodeTestProcessIo.ParseBytesRead(text));

        [Theory]
        [InlineData(0, "0 B")]
        [InlineData(999, "999 B")]
        [InlineData(1_500, "2 kB")]
        [InlineData(850_000_000, "850 MB")]
        [InlineData(999_499_999, "999 MB")]
        [InlineData(999_500_000, "1.00 GB")]
        [InlineData(1_020_000_000, "1.02 GB")]
        [InlineData(12_345_000_000, "12.35 GB")]
        public void FormatBytes_UsesDecimalUnits(long bytes, string expected)
            => Assert.Equal(expected, TranscodeTestProcessIo.FormatBytes(bytes));

        [Fact]
        public void BytesRead_OfAProcessThatWasNeverStarted_IsNull()
        {
            using var process = new Process();
            Assert.Null(TranscodeTestProcessIo.BytesRead(process));
        }
    }
}
