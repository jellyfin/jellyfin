#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Model.Net
{
    /// <summary>
    /// Class MimeTypes.
    /// </summary>
    public static class MimeTypes
    {
        /// <summary>
        /// Any extension in this list is considered a video file.
        /// </summary>
        private static readonly HashSet<string> _videoFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".3gp",
            ".asf",
            ".avi",
            ".divx",
            ".dvr-ms",
            ".f4v",
            ".flv",
            ".img",
            ".iso",
            ".m2t",
            ".m2ts",
            ".m2v",
            ".m4v",
            ".mk3d",
            ".mkv",
            ".mov",
            ".mp4",
            ".mpg",
            ".mpeg",
            ".mts",
            ".ogg",
            ".ogm",
            ".ogv",
            ".rec",
            ".ts",
            ".rmvb",
            ".webm",
            ".wmv",
            ".wtv",
        };

        // http://en.wikipedia.org/wiki/Internet_media_type
        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/MIME_types/Common_types
        // http://www.iana.org/assignments/media-types/media-types.xhtml
        // Add more as needed
        private static readonly Dictionary<string, string> _mimeTypeLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Type application
            { ".7z", "application/x-7z-compressed" },
            { ".azw", "application/vnd.amazon.ebook" },
            { ".azw3", "application/vnd.amazon.ebook" },
            { ".cbz", "application/x-cbz" },
            { ".cbr", "application/epub+zip" },
            { ".eot", "application/vnd.ms-fontobject" },
            { ".epub", "application/epub+zip" },
            { ".js", "application/x-javascript" },
            { ".json", "application/json" },
            { ".m3u8", "application/x-mpegURL" },
            { ".map", "application/x-javascript" },
            { ".mobi", "application/x-mobipocket-ebook" },
            { ".opf", "application/oebps-package+xml" },
            { ".pdf", "application/pdf" },
            { ".rar", "application/vnd.rar" },
            { ".srt", "application/x-subrip" },
            { ".ttml", "application/ttml+xml" },
            { ".wasm", "application/wasm" },
            { ".xml", "application/xml" },
            { ".zip", "application/zip" },

            // Type image
            { ".bmp", "image/bmp" },
            { ".gif", "image/gif" },
            { ".ico", "image/vnd.microsoft.icon" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".svg", "image/svg+xml" },
            { ".svgz", "image/svg+xml" },
            { ".tbn", "image/jpeg" },
            { ".tif", "image/tiff" },
            { ".tiff", "image/tiff" },
            { ".webp", "image/webp" },

            // Type font
            { ".ttf" , "font/ttf" },
            { ".woff" , "font/woff" },
            { ".woff2" , "font/woff2" },

            // Type text
            { ".ass", "text/x-ssa" },
            { ".ssa", "text/x-ssa" },
            { ".css", "text/css" },
            { ".csv", "text/csv" },
            { ".edl", "text/plain" },
            { ".rtf", "text/rtf" },
            { ".txt", "text/plain" },
            { ".vtt", "text/vtt" },

            // Type video
            { ".3gp", "video/3gpp" },
            { ".3g2", "video/3gpp2" },
            { ".asf", "video/x-ms-asf" },
            { ".avi", "video/x-msvideo" },
            { ".flv", "video/x-flv" },
            { ".mp4", "video/mp4" },
            { ".m4s", "video/mp4" },
            { ".m4v", "video/x-m4v" },
            { ".mpegts", "video/mp2t" },
            { ".mpg", "video/mpeg" },
            { ".mkv", "video/x-matroska" },
            { ".mov", "video/quicktime" },
            { ".mpd", "video/vnd.mpeg.dash.mpd" },
            { ".ogv", "video/ogg" },
            { ".ts", "video/mp2t" },
            { ".webm", "video/webm" },
            { ".wmv", "video/x-ms-wmv" },

            // Type audio
            { ".aac", "audio/mp4" },
            { ".ac3", "audio/ac3" },
            { ".ape", "audio/x-ape" },
            { ".dsf", "audio/dsf" },
            { ".dsp", "audio/dsp" },
            { ".flac", "audio/flac" },
            { ".m4a", "audio/mp4" },
            { ".m4b", "audio/m4b" },
            { ".mid", "audio/midi" },
            { ".midi", "audio/midi" },
            { ".mp3", "audio/mpeg" },
            { ".oga", "audio/ogg" },
            { ".ogg", "audio/ogg" },
            { ".opus", "audio/ogg" },
            { ".vorbis", "audio/vorbis" },
            { ".wav", "audio/wav" },
            { ".webma", "audio/webm" },
            { ".wma", "audio/x-ms-wma" },
            { ".wv", "audio/x-wavpack" },
            { ".xsp", "audio/xsp" },
        };

        private static readonly Dictionary<string, string> _extensionLookup = CreateExtensionLookup();

        private static Dictionary<string, string> CreateExtensionLookup()
        {
            var dict = _mimeTypeLookup
                .GroupBy(i => i.Value)
                .ToDictionary(x => x.Key, x => x.First().Key, StringComparer.OrdinalIgnoreCase);

            dict["image/jpg"] = ".jpg";
            dict["image/x-png"] = ".png";

            dict["audio/x-aac"] = ".aac";

            return dict;
        }

        public static string? GetMimeType(string path) => GetMimeType(path, true);

        /// <summary>
        /// Gets the type of the MIME.
        /// </summary>
        public static string? GetMimeType(string path, bool enableStreamDefault)
        {
            if (path.Length == 0)
            {
                throw new ArgumentException("String can't be empty.", nameof(path));
            }

            var ext = Path.GetExtension(path);

            if (_mimeTypeLookup.TryGetValue(ext, out string result))
            {
                return result;
            }

            // Catch-all for all video types that don't require specific mime types
            if (_videoFileExtensions.Contains(ext))
            {
                return "video/" + ext.Substring(1);
            }

            // Type text
            if (string.Equals(ext, ".html", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".htm", StringComparison.OrdinalIgnoreCase))
            {
                return "text/html; charset=UTF-8";
            }

            if (string.Equals(ext, ".log", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".srt", StringComparison.OrdinalIgnoreCase))
            {
                return "text/plain";
            }

            // Misc
            if (string.Equals(ext, ".dll", StringComparison.OrdinalIgnoreCase))
            {
                return "application/octet-stream";
            }

            return enableStreamDefault ? "application/octet-stream" : null;
        }

        public static string? ToExtension(string mimeType)
        {
            if (mimeType.Length == 0)
            {
                throw new ArgumentException("String can't be empty.", nameof(mimeType));
            }

            // handle text/html; charset=UTF-8
            mimeType = mimeType.Split(';')[0];

            if (_extensionLookup.TryGetValue(mimeType, out string result))
            {
                return result;
            }

            return null;
        }
    }
}
