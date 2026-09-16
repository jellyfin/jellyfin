using System;
using System.IO;
using System.Net.Mime;
using System.Text;
using Emby.Naming.Common;
using Jellyfin.Api.Helpers;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    public class UploadHelperTests
    {
        // Building the MIME definitions is expensive, so share one instance across all cases.
        private static readonly UploadHelper _uploadHelper = new UploadHelper(new NamingOptions());

        [Theory]
        [MemberData(nameof(GetAttachmentMimeType_TestData))]
        public void GetAttachmentMimeType_DetectsFormatFromContent(byte[] content, string expected)
        {
            using var stream = new MemoryStream(content, writable: false);

            Assert.Equal(expected, _uploadHelper.GetAttachmentMimeType(stream));
        }

        [Fact]
        public void GetAttachmentMimeType_LeavesStreamPositionUntouched()
        {
            // The stream is served to the client afterwards, so inspecting it must not consume it.
            using var stream = new MemoryStream(TrueTypeFont(), writable: false);

            Assert.Equal(MediaTypeNames.Font.Ttf, _uploadHelper.GetAttachmentMimeType(stream));
            Assert.Equal(0, stream.Position);

            stream.Position = 4;
            _uploadHelper.GetAttachmentMimeType(stream);
            Assert.Equal(4, stream.Position);
        }

        [Fact]
        public void GetAttachmentMimeType_NonSeekableStream_ReturnsOctetStream()
        {
            using var stream = new NonSeekableStream(TrueTypeFont());

            Assert.Equal(MediaTypeNames.Application.Octet, _uploadHelper.GetAttachmentMimeType(stream));
        }

        [Fact]
        public void GetAttachmentMimeType_NullStream_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _uploadHelper.GetAttachmentMimeType(null!));
        }

        public static TheoryData<byte[], string> GetAttachmentMimeType_TestData()
        {
            return new TheoryData<byte[], string>
            {
                // Fonts and cover art are what media files legitimately carry as attachments.
                { TrueTypeFont(), MediaTypeNames.Font.Ttf },
                { OpenTypeFont(), MediaTypeNames.Font.Otf },
                { Header([0x77, 0x4F, 0x46, 0x46, 0x00, 0x01, 0x00, 0x00]), MediaTypeNames.Font.Woff },
                { Header([0x77, 0x4F, 0x46, 0x32, 0x00, 0x01, 0x00, 0x00]), MediaTypeNames.Font.Woff2 },
                { Header([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00]), MediaTypeNames.Image.Jpeg },
                { Header([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52]), MediaTypeNames.Image.Png },
                { Header(Encoding.ASCII.GetBytes("GIF89a")), MediaTypeNames.Image.Gif },
                { Header([.. Encoding.ASCII.GetBytes("RIFF"), 0x24, 0x00, 0x00, 0x00, .. Encoding.ASCII.GetBytes("WEBPVP8 ")]), MediaTypeNames.Image.Webp },
                { Header([0x42, 0x4D, 0x00, 0x10, 0x00, 0x00]), MediaTypeNames.Image.Bmp },

                // An attachment claiming to be HTML must never be served as anything a browser will execute,
                // otherwise its script runs on the Jellyfin origin and can read the stored access token.
                { Encoding.UTF8.GetBytes("<html><body><script>alert(document.cookie)</script></body></html>"), MediaTypeNames.Application.Octet },
                { Encoding.UTF8.GetBytes("<!DOCTYPE html>\n<script src=\"//example.invalid/x.js\"></script>\n"), MediaTypeNames.Application.Octet },
                { Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"), MediaTypeNames.Application.Octet },
                { Encoding.UTF8.GetBytes("plain text is not a font"), MediaTypeNames.Application.Octet },
                { Array.Empty<byte>(), MediaTypeNames.Application.Octet },

                // Script smuggled in behind a valid header is still served as the format the header says,
                // which is inert, so the payload cannot execute.
                { Header([.. Encoding.ASCII.GetBytes("GIF89a"), .. Encoding.UTF8.GetBytes("<script>alert(1)</script>")]), MediaTypeNames.Image.Gif }
            };
        }

        /// <summary>
        /// Builds the sfnt header and table tags a TrueType font is recognized by.
        /// </summary>
        private static byte[] TrueTypeFont()
        {
            var font = Header([0x00, 0x01, 0x00, 0x00, 0x00]);
            Encoding.ASCII.GetBytes("cmapglyfheadlocamaxpnamepost$hmtx6hhea").CopyTo(font, 128);
            return font;
        }

        /// <summary>
        /// Builds the sfnt header and table tags an OpenType font is recognized by.
        /// </summary>
        private static byte[] OpenTypeFont()
        {
            var font = Header([0x4F, 0x54, 0x54, 0x4F, 0x00]);
            Encoding.ASCII.GetBytes("cmapheadmaxpnamepost").CopyTo(font, 12);
            return font;
        }

        private static byte[] Header(byte[] header)
        {
            var content = new byte[1024];
            header.CopyTo(content, 0);
            return content;
        }

        private sealed class NonSeekableStream(byte[] content) : MemoryStream(content, writable: false)
        {
            public override bool CanSeek => false;
        }
    }
}
