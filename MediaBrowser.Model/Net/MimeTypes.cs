#pragma warning disable CS1591
#pragma warning disable SA1600

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Model.Net
{
    /// <summary>
    /// Class MimeTypes
    /// </summary>
    public static class MimeTypes
    {
        /// <summary>
        /// Any extension in this list is considered a video file
        /// </summary>
        private static readonly HashSet<string> _videoFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mkv",
            ".m2t",
            ".m2ts",
            ".img",
            ".iso",
            ".mk3d",
            ".ts",
            ".rmvb",
            ".mov",
            ".avi",
            ".mpg",
            ".mpeg",
            ".wmv",
            ".mp4",
            ".divx",
            ".dvr-ms",
            ".wtv",
            ".ogm",
            ".ogv",
            ".asf",
            ".m4v",
            ".flv",
            ".f4v",
            ".3gp",
            ".webm",
            ".mts",
            ".m2v",
            ".rec"
        };

        // http://en.wikipedia.org/wiki/Internet_media_type
        // Add more as needed
        private static readonly Dictionary<string, string> _mimeTypeLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Type application
            { ".cbz", "application/x-cbz" },
            { ".cbr", "application/epub+zip" },
            { ".eot", "application/vnd.ms-fontobject" },
            { ".epub", "application/epub+zip" },
            { ".js", "application/x-javascript" },
            { ".json", "application/json" },
            { ".map", "application/x-javascript" },
            { ".pdf", "application/pdf" },
            { ".ttml", "application/ttml+xml" },
            { ".m3u8", "application/x-mpegURL" },
            { ".mobi", "application/x-mobipocket-ebook" },
            { ".xml", "application/xml" },
            { ".wasm", "application/wasm" },

            // Type image
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".tbn", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".tiff", "image/tiff" },
            { ".webp", "image/webp" },
            { ".ico", "image/vnd.microsoft.icon" },
            { ".svg", "image/svg+xml" },
            { ".svgz", "image/svg+xml" },

            // Type font
            { ".ttf" , "font/ttf" },
            { ".woff" , "font/woff" },

            // Type text
            { ".ass", "text/x-ssa" },
            { ".ssa", "text/x-ssa" },
            { ".css", "text/css" },
            { ".csv", "text/csv" },
            { ".txt", "text/plain" },
            { ".vtt", "text/vtt" },

            // Type video
            { ".mpg", "video/mpeg" },
            { ".ogv", "video/ogg" },
            { ".mov", "video/quicktime" },
            { ".webm", "video/webm" },
            { ".mkv", "video/x-matroska" },
            { ".wmv", "video/x-ms-wmv" },
            { ".flv", "video/x-flv" },
            { ".avi", "video/x-msvideo" },
            { ".asf", "video/x-ms-asf" },
            { ".m4v", "video/x-m4v" },
            { ".m4s", "video/mp4" },
            { ".3gp", "video/3gpp" },
            { ".3g2", "video/3gpp2" },
            { ".mpd", "video/vnd.mpeg.dash.mpd" },
            { ".ts", "video/mp2t" },

            // Type audio
            { ".mp3", "audio/mpeg" },
            { ".m4a", "audio/mp4" },
            { ".aac", "audio/mp4" },
            { ".webma", "audio/webm" },
            { ".wav", "audio/wav" },
            { ".wma", "audio/x-ms-wma" },
            { ".ogg", "audio/ogg" },
            { ".oga", "audio/ogg" },
            { ".opus", "audio/ogg" },
            { ".ac3", "audio/ac3" },
            { ".dsf", "audio/dsf" },
            { ".m4b", "audio/m4b" },
            { ".xsp", "audio/xsp" },
            { ".dsp", "audio/dsp" },
            { ".flac", "audio/flac" },
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

        public static string GetMimeType(string path) => GetMimeType(path, true);

        /// <summary>
        /// Gets the type of the MIME.
        /// </summary>
        public static string GetMimeType(string path, bool enableStreamDefault)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
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

        public static string ToExtension(string mimeType)
        {
            if (string.IsNullOrEmpty(mimeType))
            {
                throw new ArgumentNullException(nameof(mimeType));
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
