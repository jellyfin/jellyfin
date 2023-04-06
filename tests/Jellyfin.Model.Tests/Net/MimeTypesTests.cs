using MediaBrowser.Model.Net;
using Xunit;

namespace Jellyfin.Model.Tests.Net
{
    public class MimeTypesTests
    {
        [Theory]
        [InlineData(".dll", "application/octet-stream")]
        [InlineData(".log", "text/plain")]
        [InlineData(".srt", "application/x-subrip")]
        [InlineData(".html", "text/html; charset=UTF-8")]
        [InlineData(".htm", "text/html; charset=UTF-8")]
        [InlineData(".7z", "application/x-7z-compressed")]
        [InlineData(".azw", "application/vnd.amazon.ebook")]
        [InlineData(".azw3", "application/vnd.amazon.ebook")]
        [InlineData(".eot", "application/vnd.ms-fontobject")]
        [InlineData(".epub", "application/epub+zip")]
        [InlineData(".json", "application/json")]
        [InlineData(".mobi", "application/x-mobipocket-ebook")]
        [InlineData(".opf", "application/oebps-package+xml")]
        [InlineData(".pdf", "application/pdf")]
        [InlineData(".rar", "application/vnd.rar")]
        [InlineData(".ttml", "application/ttml+xml")]
        [InlineData(".wasm", "application/wasm")]
        [InlineData(".xml", "application/xml")]
        [InlineData(".zip", "application/zip")]
        [InlineData(".bmp", "image/bmp")]
        [InlineData(".gif", "image/gif")]
        [InlineData(".ico", "image/vnd.microsoft.icon")]
        [InlineData(".jpg", "image/jpeg")]
        [InlineData(".jpeg", "image/jpeg")]
        [InlineData(".png", "image/png")]
        [InlineData(".svg", "image/svg+xml")]
        [InlineData(".svgz", "image/svg+xml")]
        [InlineData(".tbn", "image/jpeg")]
        [InlineData(".tif", "image/tiff")]
        [InlineData(".tiff", "image/tiff")]
        [InlineData(".webp", "image/webp")]
        [InlineData(".ttf", "font/ttf")]
        [InlineData(".woff", "font/woff")]
        [InlineData(".woff2", "font/woff2")]
        [InlineData(".ass", "text/x-ssa")]
        [InlineData(".ssa", "text/x-ssa")]
        [InlineData(".css", "text/css")]
        [InlineData(".csv", "text/csv")]
        [InlineData(".edl", "text/plain")]
        [InlineData(".txt", "text/plain")]
        [InlineData(".vtt", "text/vtt")]
        [InlineData(".3gp", "video/3gpp")]
        [InlineData(".3g2", "video/3gpp2")]
        [InlineData(".asf", "video/x-ms-asf")]
        [InlineData(".avi", "video/x-msvideo")]
        [InlineData(".flv", "video/x-flv")]
        [InlineData(".mp4", "video/mp4")]
        [InlineData(".m4v", "video/x-m4v")]
        [InlineData(".mpegts", "video/mp2t")]
        [InlineData(".mpg", "video/mpeg")]
        [InlineData(".mkv", "video/x-matroska")]
        [InlineData(".mov", "video/quicktime")]
        [InlineData(".ogv", "video/ogg")]
        [InlineData(".ts", "video/mp2t")]
        [InlineData(".webm", "video/webm")]
        [InlineData(".wmv", "video/x-ms-wmv")]
        [InlineData(".aac", "audio/aac")]
        [InlineData(".ac3", "audio/ac3")]
        [InlineData(".ape", "audio/x-ape")]
        [InlineData(".dsf", "audio/dsf")]
        [InlineData(".dsp", "audio/dsp")]
        [InlineData(".flac", "audio/flac")]
        [InlineData(".m4a", "audio/mp4")]
        [InlineData(".m4b", "audio/m4b")]
        [InlineData(".mid", "audio/midi")]
        [InlineData(".midi", "audio/midi")]
        [InlineData(".mp3", "audio/mpeg")]
        [InlineData(".oga", "audio/ogg")]
        [InlineData(".ogg", "audio/ogg")]
        [InlineData(".opus", "audio/ogg")]
        [InlineData(".vorbis", "audio/vorbis")]
        [InlineData(".wav", "audio/wav")]
        [InlineData(".webma", "audio/webm")]
        [InlineData(".wma", "audio/x-ms-wma")]
        [InlineData(".wv", "audio/x-wavpack")]
        [InlineData(".xsp", "audio/xsp")]
        public void GetMimeType_Valid_ReturnsCorrectResult(string input, string expectedResult)
        {
            Assert.Equal(expectedResult, MimeTypes.GetMimeType(input, null));
        }

        [Theory]
        [InlineData("application/epub+zip", ".epub")]
        [InlineData("application/json", ".json")]
        [InlineData("application/oebps-package+xml", ".opf")]
        [InlineData("application/pdf", ".pdf")]
        [InlineData("application/ttml+xml", ".ttml")]
        [InlineData("application/vnd.amazon.ebook", ".azw")]
        [InlineData("application/vnd.ms-fontobject", ".eot")]
        [InlineData("application/vnd.rar", ".rar")]
        [InlineData("application/wasm", ".wasm")]
        [InlineData("application/x-7z-compressed", ".7z")]
        [InlineData("application/x-cbz", ".cbz")]
        [InlineData("application/x-javascript", ".js")]
        [InlineData("application/x-mobipocket-ebook", ".mobi")]
        [InlineData("application/x-mpegURL", ".m3u8")]
        [InlineData("application/x-subrip", ".srt")]
        [InlineData("application/xml", ".xml")]
        [InlineData("application/zip", ".zip")]
        [InlineData("audio/aac", ".aac")]
        [InlineData("audio/ac3", ".ac3")]
        [InlineData("audio/dsf", ".dsf")]
        [InlineData("audio/dsp", ".dsp")]
        [InlineData("audio/flac", ".flac")]
        [InlineData("audio/m4b", ".m4b")]
        [InlineData("audio/mp4", ".m4a")]
        [InlineData("audio/vorbis", ".vorbis")]
        [InlineData("audio/wav", ".wav")]
        [InlineData("audio/x-aac", ".aac")]
        [InlineData("audio/x-ape", ".ape")]
        [InlineData("audio/x-ms-wma", ".wma")]
        [InlineData("audio/x-wavpack", ".wv")]
        [InlineData("audio/xsp", ".xsp")]
        [InlineData("font/ttf", ".ttf")]
        [InlineData("font/woff", ".woff")]
        [InlineData("font/woff2", ".woff2")]
        [InlineData("image/bmp", ".bmp")]
        [InlineData("image/gif", ".gif")]
        [InlineData("image/jpeg", ".jpg")]
        [InlineData("image/png", ".png")]
        [InlineData("image/svg+xml", ".svg")]
        [InlineData("image/tiff", ".tiff")]
        [InlineData("image/vnd.microsoft.icon", ".ico")]
        [InlineData("image/webp", ".webp")]
        [InlineData("image/x-icon", ".ico")]
        [InlineData("image/x-png", ".png")]
        [InlineData("text/css", ".css")]
        [InlineData("text/csv", ".csv")]
        [InlineData("text/plain", ".txt")]
        [InlineData("text/rtf", ".rtf")]
        [InlineData("text/vtt", ".vtt")]
        [InlineData("text/x-ssa", ".ssa")]
        [InlineData("video/3gpp", ".3gp")]
        [InlineData("video/3gpp2", ".3g2")]
        [InlineData("video/mp2t", ".ts")]
        [InlineData("video/mp4", ".mp4")]
        [InlineData("video/ogg", ".ogv")]
        [InlineData("video/quicktime", ".mov")]
        [InlineData("video/vnd.mpeg.dash.mpd", ".mpd")]
        [InlineData("video/webm", ".webm")]
        [InlineData("video/x-flv", ".flv")]
        [InlineData("video/x-m4v", ".m4v")]
        [InlineData("video/x-matroska", ".mkv")]
        [InlineData("video/x-ms-asf", ".asf")]
        [InlineData("video/x-ms-wmv", ".wmv")]
        [InlineData("video/x-msvideo", ".avi")]
        public void ToExtension_Valid_ReturnsCorrectResult(string input, string expectedResult)
        {
            Assert.Equal(expectedResult, MimeTypes.ToExtension(input));
        }
    }
}
